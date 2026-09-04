"""
Servidor MCP de la documentación de DeadKillers.

Sirve los documentos de `docs/` y `CLAUDE.md` por SECCIONES en lugar de por
archivos enteros: pedir la tabla del jugador de BALANCE.md son 12 líneas en vez
de las 85 del archivo. Ese es todo el ahorro de contexto.

Sin dependencias: solo biblioteca estándar (regla 8 de CLAUDE.md).
Protocolo: JSON-RPC 2.0 sobre stdio, un mensaje por línea.

IMPORTANTE: stdout es el canal del protocolo. Nada de prints sueltos; los
mensajes de diagnóstico van a stderr.
"""

import json
import re
import sys
from pathlib import Path

PROTOCOL_VERSION = "2024-11-05"
SERVER_NAME = "deadkillers-docs"
SERVER_VERSION = "1.0.0"

# server.py está en <raíz>/tools/docs-mcp/, así que la raíz son dos niveles arriba.
ROOT = Path(__file__).resolve().parents[2]

HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*$")

# Límite de coincidencias de búsqueda, para que un término común no devuelva el
# proyecto entero y anule el propósito de esto.
MAX_SEARCH_HITS = 25


def documents():
    """Mapa nombre -> ruta. El nombre es el que se usa en las herramientas."""
    found = {}

    claude_md = ROOT / "CLAUDE.md"
    if claude_md.is_file():
        found["CLAUDE"] = claude_md

    docs_dir = ROOT / "docs"
    if docs_dir.is_dir():
        for path in sorted(docs_dir.glob("*.md")):
            found[path.stem.upper()] = path

    return found


def resolve(name):
    """Acepta 'BALANCE', 'balance', 'BALANCE.md' o 'docs/BALANCE.md'."""
    docs = documents()
    key = Path(str(name)).stem.upper()
    return docs.get(key)


def parse_sections(text):
    """
    Trocea un markdown en secciones. Cada sección va desde su encabezado hasta el
    siguiente encabezado del mismo nivel o superior.

    Devuelve una lista de dicts: level, title, start, end, body.
    """
    lines = text.splitlines()

    heads = []
    for index, line in enumerate(lines):
        match = HEADING.match(line)
        if match:
            heads.append((index, len(match.group(1)), match.group(2)))

    sections = []
    for position, (start, level, title) in enumerate(heads):
        end = len(lines)
        for next_start, next_level, _ in heads[position + 1:]:
            if next_level <= level:
                end = next_start
                break

        sections.append({
            "level": level,
            "title": title,
            "start": start + 1,          # 1-indexado, como lo ve un humano
            "end": end,
            "body": "\n".join(lines[start:end]).rstrip(),
        })

    return sections


# --------------------------------------------------------------------------
# Herramientas
# --------------------------------------------------------------------------

def tool_list_docs(_arguments):
    """Índice barato: qué documentos hay y qué secciones tiene cada uno."""
    out = []
    for name, path in documents().items():
        text = path.read_text(encoding="utf-8")
        total = len(text.splitlines())
        out.append(f"## {name}  ({path.relative_to(ROOT).as_posix()}, {total} líneas)")

        for section in parse_sections(text):
            if section["level"] > 3:
                continue
            size = section["end"] - section["start"] + 1
            indent = "  " * (section["level"] - 1)
            out.append(f"{indent}- {section['title']}  [{size} líneas]")

        out.append("")

    if not out:
        return "No se ha encontrado ningún documento. ¿Está el servidor en la raíz correcta?"

    return "\n".join(out).rstrip()


def tool_read_section(arguments):
    name = arguments.get("document", "")
    wanted = str(arguments.get("heading", "")).strip().lower()

    path = resolve(name)
    if path is None:
        return f"No existe el documento '{name}'. Usa list_docs para ver los disponibles."

    sections = parse_sections(path.read_text(encoding="utf-8"))

    exact = [s for s in sections if s["title"].strip().lower() == wanted]
    partial = [s for s in sections if wanted in s["title"].strip().lower()]
    matches = exact or partial

    if not matches:
        titles = ", ".join(s["title"] for s in sections if s["level"] <= 3)
        return f"'{arguments.get('heading')}' no aparece en {name}. Secciones: {titles}"

    return "\n\n".join(
        f"[{name} · líneas {s['start']}-{s['end']}]\n{s['body']}" for s in matches
    )


def tool_search_docs(arguments):
    """
    Devuelve las LÍNEAS que coinciden, no las secciones que las contienen: una
    sección de nivel 1 es el documento entero, y devolverla anularía el ahorro.
    Para leer el bloque completo está read_section, con el título que sale aquí.
    """
    query = str(arguments.get("query", "")).strip().lower()
    if not query:
        return "Falta el término de búsqueda."

    hits = []
    for name, path in documents().items():
        lines = path.read_text(encoding="utf-8").splitlines()
        sections = parse_sections("\n".join(lines))

        for number, line in enumerate(lines, start=1):
            if query not in line.lower():
                continue

            # Sección más profunda que contiene la línea: es la más informativa.
            enclosing = [s for s in sections if s["start"] <= number <= s["end"]]
            title = max(enclosing, key=lambda s: s["level"])["title"] if enclosing else "—"

            hits.append((name, title, number, line.strip()))

    if not hits:
        return f"Sin resultados para '{arguments.get('query')}'."

    truncated = len(hits) > MAX_SEARCH_HITS
    shown = hits[:MAX_SEARCH_HITS]

    out = [f"{name} · {title} · L{number}: {line}" for name, title, number, line in shown]

    if truncated:
        out.append(f"({len(hits)} coincidencias, mostradas {MAX_SEARCH_HITS}; afina la búsqueda)")

    out.append("Usa read_section con el documento y el título para leer el bloque entero.")

    return "\n".join(out)


def tool_read_doc(arguments):
    name = arguments.get("document", "")
    path = resolve(name)
    if path is None:
        return f"No existe el documento '{name}'. Usa list_docs para ver los disponibles."

    return path.read_text(encoding="utf-8")


TOOLS = [
    {
        "name": "list_docs",
        "description": (
            "Índice de la documentación de DeadKillers: qué documentos hay y qué "
            "secciones tiene cada uno, con su tamaño. Empieza siempre por aquí para "
            "saber qué sección pedir en lugar de leer un archivo entero."
        ),
        "inputSchema": {"type": "object", "properties": {}},
        "handler": tool_list_docs,
    },
    {
        "name": "read_section",
        "description": (
            "Devuelve UNA sección de un documento. Es la forma barata de consultar la "
            "documentación. Ejemplo: document='BALANCE', heading='Jugador'."
        ),
        "inputSchema": {
            "type": "object",
            "properties": {
                "document": {
                    "type": "string",
                    "description": "BALANCE, DESIGN, ARCHITECTURE, ROADMAP, ASSETS, DECISIONS o CLAUDE.",
                },
                "heading": {
                    "type": "string",
                    "description": "Título de la sección; admite coincidencia parcial.",
                },
            },
            "required": ["document", "heading"],
        },
        "handler": tool_read_section,
    },
    {
        "name": "search_docs",
        "description": (
            "Busca un término en toda la documentación y devuelve las secciones que lo "
            "contienen. Úsalo cuando no sepas en qué documento está lo que buscas."
        ),
        "inputSchema": {
            "type": "object",
            "properties": {
                "query": {"type": "string", "description": "Término a buscar."},
            },
            "required": ["query"],
        },
        "handler": tool_search_docs,
    },
    {
        "name": "read_doc",
        "description": (
            "Devuelve un documento entero. Caro: usa read_section salvo que de verdad "
            "necesites el documento completo."
        ),
        "inputSchema": {
            "type": "object",
            "properties": {
                "document": {"type": "string", "description": "Nombre del documento."},
            },
            "required": ["document"],
        },
        "handler": tool_read_doc,
    },
]

BY_NAME = {tool["name"]: tool for tool in TOOLS}


# --------------------------------------------------------------------------
# Protocolo
# --------------------------------------------------------------------------

def handle(message):
    """Devuelve la respuesta a un mensaje, o None si era una notificación."""
    method = message.get("method")
    message_id = message.get("id")

    if method == "initialize":
        requested = (message.get("params") or {}).get("protocolVersion")
        return {
            "protocolVersion": requested or PROTOCOL_VERSION,
            "capabilities": {"tools": {}},
            "serverInfo": {"name": SERVER_NAME, "version": SERVER_VERSION},
        }

    if method == "tools/list":
        return {
            "tools": [
                {k: v for k, v in tool.items() if k != "handler"} for tool in TOOLS
            ]
        }

    if method == "tools/call":
        params = message.get("params") or {}
        tool = BY_NAME.get(params.get("name"))

        if tool is None:
            return {
                "content": [{"type": "text", "text": f"Herramienta desconocida: {params.get('name')}"}],
                "isError": True,
            }

        try:
            text = tool["handler"](params.get("arguments") or {})
            return {"content": [{"type": "text", "text": text}]}
        except Exception as error:  # el servidor nunca debe morir por una consulta
            return {
                "content": [{"type": "text", "text": f"Error en {tool['name']}: {error}"}],
                "isError": True,
            }

    if method in ("ping",):
        return {}

    # Las notificaciones no llevan id y no se responden.
    if message_id is None:
        return None

    raise LookupError(f"Método no soportado: {method}")


def main():
    sys.stdin.reconfigure(encoding="utf-8")
    sys.stdout.reconfigure(encoding="utf-8")

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue

        try:
            message = json.loads(line)
        except json.JSONDecodeError:
            continue

        message_id = message.get("id")

        try:
            result = handle(message)
        except LookupError as error:
            if message_id is None:
                continue
            response = {
                "jsonrpc": "2.0",
                "id": message_id,
                "error": {"code": -32601, "message": str(error)},
            }
            sys.stdout.write(json.dumps(response, ensure_ascii=False) + "\n")
            sys.stdout.flush()
            continue

        if result is None or message_id is None:
            continue

        response = {"jsonrpc": "2.0", "id": message_id, "result": result}
        sys.stdout.write(json.dumps(response, ensure_ascii=False) + "\n")
        sys.stdout.flush()


if __name__ == "__main__":
    main()
