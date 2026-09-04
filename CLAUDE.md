# CLAUDE.md

> Godot `4.7.2` (build .NET) · .NET `8` · Nombre del proyecto `DeadKillers` · Namespace raíz `DeadKillers`

## Qué es esto

Shooter de acción con **cámara cenital**, ambientación gótico-medieval. Un cazador
limpia aldeas, criptas y bosques infestados de demonios, vampiros y licántropos.
Pocos enemigos pero muy peligrosos, armas medievales, misiones con objetivos.
Godot 4 con **C#** (build .NET, obligatorio). Solo PC. Desarrollador en solitario.

**El desarrollador empieza desde cero con Godot** y viene de tutoriales en GDScript.

---

## Cuándo leer cada documento

**No leas nada de esto por defecto.** Ábrelo solo cuando la tarea lo pida.

| Documento | Ábrelo cuando… |
|---|---|
| `docs/ROADMAP.md` | Empiezas cualquier tarea. Dice cuál es el hito activo y cuándo se cierra. **Este es el único de lectura obligatoria.** |
| `docs/ARCHITECTURE.md` | Escribes o modificas código: convenciones, carpetas, capas de física, acciones de input, patrones de Godot en C#, errores de compilación |
| `docs/DESIGN.md` | Trabajas en algo que afecta a cómo se juega: control, cámara, IA, misiones, progresión |
| `docs/BALANCE.md` | Necesitas cualquier número: vida, daño, velocidad, cadencia, munición |
| `docs/ASSETS.md` | Importas modelos, sonidos o placeholders |
| `docs/DECISIONS.md` | Vas a decidir algo estructural, o algo ya decidido parece discutible |

---

## Reglas de trabajo

1. **Una tarea por vez.** Al terminar, para y espera a que se pruebe en Godot.
2. **Solo el hito activo.** La documentación describe el juego completo; no implementes
   nada fuera del hito en curso aunque esté documentado. Si el hito actual depende de
   una decisión de un hito futuro, pregunta.
3. **Explica el porqué** de cada decisión de arquitectura, en **2-3 frases máximo**.
4. **Instrucciones del editor paso a paso, con el menú exacto**, siempre que haya que
   tocar algo en Godot. El desarrollador no puede deducirlo.
5. **Al terminar, di qué probar y qué debería verse en pantalla.**
6. Antes de escribir código nuevo, comprueba si ya existe algo parecido en `/scripts`.
7. **Escenas `.tscn`:** son texto y se pueden generar, pero es frágil. Por defecto,
   explica cómo crearlas en el editor. Genéralas solo si se pide.
8. No añadas plugins ni dependencias sin preguntar.
9. No refactorices código que funciona salvo petición explícita.
10. Si una petición choca con la documentación, **dilo** en vez de implementarla en silencio.
11. Avisa cuando la forma correcta en C# difiera de lo que se ve en los tutoriales de
    GDScript. Pasa constantemente y es fuente de confusión.

## Al cerrar una tarea

- Si cambia el estado del hito → propón la actualización de `docs/ROADMAP.md`.
- Si se tomó una decisión estructural → propón una entrada en `docs/DECISIONS.md`.
- Si se ajustó un número → propón la actualización de `docs/BALANCE.md`.

No edites esos archivos sin avisar; propón el cambio y espera confirmación.

## Qué NO construir

Multijugador · generación procedural de niveles · guardado antes del hito 5 · diálogos ·
crafteo · logros · soporte para mando · localización · **cualquier optimización antes de
tener un problema medido**.

Si algo de esto parece necesario, dilo y espera respuesta.

## Lo mínimo del código (detalle en `docs/ARCHITECTURE.md`)

- `PascalCase` público, `_camelCase` privado. Un archivo, una clase.
- **Composición sobre herencia.** Componentes (`HealthComponent`, `StateMachine`),
  no cadenas de clases base.
- **Ninguna constante mágica en la lógica.** Los valores compartidos van a un
  `Resource`; los de una escena, a `[Export]`.
- Señales y eventos, nunca `GetNode("../../..")`. Nada de `GetNode` en `_Process`.
- Comentarios en español, código en inglés.
