# 001 · Animación procedural de personajes

**Para:** Codex
**Estado:** sin empezar

## Objetivo

Que los personajes dejen de deslizarse. Ahora mismo el cazador y los tres monstruos se
mueven por el mapa como fichas sobre un tablero, sin un solo cambio de postura, y eso es
lo que más barato se nota y peor sienta.

## El problema de fondo

Los modelos de `models/*.glb` son **una sola malla unida, sin esqueleto**. No hay huesos
que rotar, así que no se puede animar por keyframes ni con `AnimationPlayer` sobre un
rig. La animación tiene que salir de transformar el nodo del modelo entero: balanceo,
inclinación, achatamiento y giro.

No es un apaño temporal: cuando lleguen modelos riggeados de verdad, este componente se
desactiva y entra un `AnimationPlayer`. Mientras tanto, un cuerpo que se inclina al
correr y se encoge antes de saltar comunica muchísimo más que uno rígido.

## Archivos

**Crear:**
- `scripts/components/ProceduralAnimator.cs`

**Modificar:**
- `scripts/enemies/Enemy.cs` — solo para exponer al animador el estado de movimiento
- `scripts/player/PlayerController.cs` — lo mismo
- `scenes/enemies/Werewolf.tscn`, `Vampire.tscn`, `Demon.tscn`
- `scenes/levels/Mission01.tscn`, `Mission02.tscn` — colgar el componente del jugador

**No tocar:** `tools/blender/`, los `.glb`, `BodyEffects.cs`, `HealthComponent.cs`, ni
nada de `scripts/missions/` o `scripts/level/`.

## Comportamiento

El componente recibe el nodo del cuerpo (`Node3D`, puede tener varias mallas dentro) y
lo transforma **en local**, sin tocar la posición del `CharacterBody3D` padre. Todo
relativo a la postura de reposo que guarda en `_Ready`.

### Al caminar o correr

La intensidad escala con la velocidad horizontal actual dividida por una velocidad de
referencia exportada. Parado, todo vuelve a reposo.

| Efecto | Valor |
|---|---|
| Balanceo vertical | seno de 2 ciclos por paso, amplitud 0,055 m a velocidad plena |
| Inclinación hacia delante | hasta 7° a velocidad plena |
| Balanceo lateral | ±4°, medio ciclo por paso, desfasado 90° del vertical |
| Frecuencia de paso | 2,1 pasos/segundo a velocidad plena, proporcional |

### Al preparar un ataque

Anticipación: el cuerpo **se echa atrás y se encoge** durante el aviso. Es lo que hace
legible que va a pasar algo, y refuerza el telegrafiado que ya existe.

- Inclinación hacia atrás progresiva hasta −12°
- Achatamiento hasta 0,92 en vertical y 1,05 en horizontal

### Al atacar

Estirón hacia delante en los primeros 0,08 s y vuelta suave:

- Inclinación hacia delante hasta 22°
- Estirado hasta 1,08 en vertical

### Al recibir daño

Sacudida corta de 0,15 s: retroceso de 6° y vuelta. No debe cortar lo anterior, se suma.

### Al morir

Cae de lado en 0,5 s: rotación de 82° sobre el eje de avance, y desciende 0,35 m.
Se queda así. **Es el efecto que más se va a notar**, porque ahora los cadáveres se
quedan de pie como estatuas.

## Cómo se entera del estado

No inventes un sistema de eventos nuevo. Lo mínimo que funcione:

- **Velocidad:** el componente lee `Velocity` del `CharacterBody3D` padre.
- **Estados de ataque:** `Enemy` ya llama a `SetTelegraph(bool)` desde `PrepareState`, y
  los estados llaman a `Lunge()`. Añade ahí las llamadas al animador.
- **Daño y muerte:** suscríbete a `HealthComponent.HealthChanged` y `Died`, igual que
  hace `BodyEffects`.
- **Jugador:** `PlayerController` ya sabe cuándo golpea, en `UpdateAttack`.

## Criterios de aceptación

- [ ] `dotnet build` sin errores **ni avisos**
- [ ] Las tres escenas de nivel arrancan sin errores en consola
- [ ] Un enemigo parado está exactamente en su postura de reposo, sin deriva
- [ ] Al perseguir, el cuerpo se balancea e inclina hacia delante
- [ ] Durante el aviso de ataque se echa atrás visiblemente **antes** de embestir
- [ ] Al morir cae de lado y se queda tumbado
- [ ] Nada de esto mueve la posición del `CharacterBody3D`: las colisiones y el alcance
      de los golpes no cambian
- [ ] Los valores están en `[Export]`, no como constantes en la lógica
- [ ] `_ExitTree` suelta las referencias a recursos, como manda D-011

## Cómo verificar

```
dotnet build

"C:\Godot\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --editor --quit
"C:\Godot\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --quit-after 700 res://scenes/levels/Mission01.tscn
"C:\Godot\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --quit-after 700 res://scenes/levels/Mission02.tscn
```

La consola tiene que quedar limpia. Al cerrar salen dos líneas sobre instancias sin
liberar: **están investigadas y son del servidor de audio de Godot**, no del proyecto
(ver D-015). Ignóralas.

Lo visual no se puede comprobar sin ventana. Deja dicho qué habría que mirar.

## Fuera de alcance

- Tocar los modelos o el script de Blender
- Animación por huesos o `AnimationPlayer`
- Animar las armas, los proyectiles o los objetos del kit
- Cambiar cualquier número de `docs/BALANCE.md`
- Cualquier cosa del hito 6

## Propuestas de documentación

Si sale bien, propón —sin aplicarlas— una entrada en `docs/DECISIONS.md` explicando por
qué la animación es procedural sobre el cuerpo entero y no por huesos, y qué la
sustituirá. Y una fila en `docs/ARCHITECTURE.md` si hace falta.
