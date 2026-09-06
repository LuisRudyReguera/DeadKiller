# Órdenes de trabajo

Carpeta de encargos para Codex. Claude escribe la orden aquí; el desarrollador se la
pasa a Codex con una línea; Codex la implementa; Claude la revisa.

## Por qué existe

Claude no puede invocar a Codex: no tiene ninguna herramienta para ello. Y el puente MCP
de `tools/docs-mcp/claude_bridge.py` va en la otra dirección — es Codex quien pide
revisiones a Claude. Este directorio es el canal que falta, y el relevo lo hace el
desarrollador.

## Cómo se usa

1. **Claude escribe la orden** en `docs/tasks/NNN-nombre.md`, con el formato de abajo.
2. **El desarrollador se la pasa a Codex**, literalmente:

       implementa docs/tasks/003-lo-que-sea.md

3. **Codex la implementa.** Ya trabaja bajo `AGENTS.md`, que son las mismas reglas que
   `CLAUDE.md`, así que no hace falta repetirle el contrato.
4. **Claude revisa**: lee el diff, compila, ejecuta las escenas sin ventana y dice qué
   está bien y qué no.

## Qué tiene que llevar una orden

Una orden sin criterios de aceptación no es una orden, es una sugerencia.

- **Objetivo** en una frase. Qué problema resuelve, no qué código escribir.
- **Archivos** que se pueden tocar, y explícitamente los que **no**.
- **Comportamiento**, con números concretos. Nada de «que se vea bien».
- **Criterios de aceptación**: lista comprobable. Si no se puede comprobar, sobra.
- **Cómo verificar**: los comandos exactos.
- **Fuera de alcance**: lo que NO hay que hacer, para que no se desmadre.

## Estado

| Orden | Estado |
|---|---|
| `001-animacion-procedural.md` | Sin empezar |
