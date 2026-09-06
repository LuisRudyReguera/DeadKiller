# Revision de Claude: emboscadas

Archivos enviados con autorizacion: Mission.cs, PurgeObjective.cs, EnemySpawner.cs.
Observaciones externas pendientes de contraste; no constituyen pruebas.

## Recuento de emboscadas y desbloqueo de salida

### 1. Acoplamiento global: cualquier `EnemySpawner` del nivel bloquea la salida

`Mission.WatchSpawners(GetParent() ?? this)` recorre recursivamente **todo el árbol del nivel** buscando instancias de `EnemySpawner` con `EnemyScene != null`, y las añade a `_pendingSpawners` sin comprobar si ese spawner está relacionado con algún `Objective` concreto. `PurgeObjective.RefreshProgress` luego exige `Mission.PendingAmbushes == 0` para completarse.

**Evidencia**: `Mission.cs`, método `WatchSpawners`, y `PurgeObjective.RefreshProgress`.

**Consecuencia**: cualquier `EnemySpawner` presente en la escena —aunque sea decorativo, de una zona secreta, o pensado para dispararse después de que el jugador ya haya salido— cuenta como "emboscada pendiente" y puede impedir para siempre que la salida se abra, sin que el diseñador lo haya vinculado a ningún objetivo.

**Corrección sugerida**: que `PurgeObjective` (u otro mecanismo explícito) declare qué spawners debe vigilar, en vez de que `Mission` los recoja indiscriminadamente de todo el nivel.

### 2. Sin salvaguarda ante una emboscada que nunca se activa

`PendingAmbushes` solo desciende cuando el spawner emite `Activated`. No hay temporizador, límite de intentos ni forma de que el diseño (o el jugador) fuerce esa activación si el disparador de la emboscada (zona, interacción, etc.) es inalcanzable o falla.

**Evidencia**: `_pendingSpawners` solo se vacía en el handler `activated` dentro de `WatchSpawners`; no hay ningún otro camino de salida.

**Consecuencia**: combinado con el punto 1, un solo spawner mal configurado o un trigger roto deja la misión en un softlock permanente, sin aviso claro más allá del contador "emboscadas pendientes: N" en `PurgeObjective.ProgressText`.

**Corrección sugerida**: validar en editor/tiempo de diseño que todo `EnemySpawner` relevante tenga un disparador alcanzable, o añadir un mecanismo de fallback (p. ej. activación forzada al entrar en una zona amplia, o un aviso de depuración si tras cierto tiempo sigue pendiente).

### 3. Orden `Spawned`/`Activated` no verificable con el código disponible

El comentario en `WatchSpawners` asume como contrato que `LevelResponder`/`EnemySpawner` emite todos los `Spawned` de una oleada **antes** de emitir `Activated`. Si ese orden se invirtiera, `PendingAmbushes` bajaría a 0 antes de que los enemigos recién nacidos se contabilicen en `RemainingEnemies`, y si en ese instante no quedaban más enemigos vivos, la salida se desbloquearía con la emboscada aún sin contar.

**Evidencia**: el propio comentario en `Mission.cs` ("Spawned ya registró toda la oleada antes de Activated") reconoce la dependencia, pero no tengo a la vista `LevelResponder.cs` para confirmar que `OnActivate()` se ejecuta síncronamente antes de emitir `Activated`.

**Limitación**: no puedo confirmar ni descartar este problema sin `LevelResponder.cs`. Lo señalo como riesgo, no como bug confirmado.

### Limitaciones generales
No he visto `LevelResponder.cs`, `Objective.cs` (clase base), `HealthComponent.cs` ni `Groups.cs`. Los puntos 1 y 2 sí son demostrables directamente en el código proporcionado; el punto 3 depende de código no revisado.

## Contraste local de Codex

- Los puntos 1 y 2 describen riesgos de configuracion, no un fallo demostrado del mapa actual. Mission02 contiene una emboscada enlazada a AmbushTrigger. No se ha verificado en esta tarea su accesibilidad jugando.
- El punto 3 queda descartado para la implementacion actual: LevelResponder.Activate llama sincronicamente a OnActivate antes de emitir Activated. EnemySpawner emite Spawned dentro de OnActivate.
- No se aplican temporizadores ni activaciones forzadas sin un problema reproducido: cambiarian el comportamiento de las emboscadas.
- Conexion real verificada con los tres archivos autorizados. Esta revision consumio uso de Claude; no demuestra ahorro de tokens.
