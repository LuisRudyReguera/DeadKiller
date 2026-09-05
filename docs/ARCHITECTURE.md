# ARCHITECTURE

> Ábrelo cuando escribas o modifiques código.

## Stack

- **Godot 4.x, build .NET/Mono.** El build estándar no ejecuta C#.
- **C# exclusivamente.** Nada de GDScript sin justificarlo y preguntarlo.
- .NET 8+ · VS Code con C# Dev Kit como editor externo · Solo Windows/PC · Git

## Estructura de carpetas

```
/scenes      escenas .tscn por dominio (player/, enemies/, weapons/, levels/, ui/)
/scripts     código C#, misma estructura que /scenes
/scripts/components  componentes reutilizables sin escena propia
/resources   recursos .tres (WeaponData, EnemyData…)
/resources/weapons   un .tres por arma
/resources/enemies   un .tres por arquetipo
/models      modelos .glb
/audio       sonidos y música
/materials   materiales y shaders
/docs        documentación
```

`/scripts` **replica** la estructura de `/scenes`. Si una escena está en
`scenes/enemies/werewolf/`, su script está en `scripts/enemies/werewolf/`.

## Convenciones de código

- Clases y métodos públicos en `PascalCase`; campos privados en `_camelCase`.
- Un archivo, una clase, mismo nombre.
- Comentarios en español, código en inglés.
- `GD.Print` solo para depuración temporal. Quitarlo antes de cerrar la tarea.

### Composición sobre herencia

Un enemigo es un `CharacterBody3D` con componentes hijos, **no** una cadena de clases base:

```
Werewolf (CharacterBody3D)
├── HealthComponent
├── HitboxComponent
├── StateMachine
│   ├── IdleState
│   ├── ChaseState
│   └── AttackState
├── AnimationPlayer
└── CollisionShape3D
```

Un componente no conoce a su padre concreto. `HealthComponent` funciona igual en el
jugador, en un enemigo y en un barril.

### Dónde vive cada número

- **Compartido por varias entidades del mismo tipo** → `Resource` guardado como `.tres`
  (`WeaponData`, `EnemyData`). Se edita sin recompilar.
- **Propio de una escena concreta** → `[Export]`.
- **Nunca** una constante mágica dentro de la lógica.

Los valores de partida están en `docs/BALANCE.md`.

### Señales en C#

```csharp
[Signal]
public delegate void HealthChangedEventHandler(int current, int max);

EmitSignal(SignalName.HealthChanged, _current, _max);
```

El sufijo `EventHandler` es **obligatorio** en Godot 4; sin él la señal no se registra.
Este es uno de los puntos donde C# difiere de los tutoriales en GDScript.

### Referencias entre nodos

- Comunicación **hacia arriba** por señales. Comunicación **hacia abajo** por
  referencias cacheadas.
- **Nunca** `GetNode("../../..")`. Si hace falta, la jerarquía está mal.
- Cachear en `_Ready`, nunca `GetNode` dentro de `_Process` o `_PhysicsProcess`.
- Para referencias entre escenas distintas: `[Export] NodePath` asignado en el editor.

## Capas de física 3D

Configurar en `Project Settings → Layers → 3D Physics`. Mantener esta lista actualizada.

| Nº | Nombre | Contiene |
|---|---|---|
| 1 | `World` | Suelo, paredes, obstáculos estáticos |
| 2 | `Player` | Cuerpo del jugador |
| 3 | `Enemy` | Cuerpos de enemigos |
| 4 | `PlayerHitbox` | Áreas que hacen daño al jugador |
| 5 | `EnemyHitbox` | Áreas que hacen daño a los enemigos |
| 6 | `Projectile` | Flechas, virotes, balas |
| 7 | `Pickup` | Armas, munición, oro del suelo |
| 8 | `Interactable` | Puertas, palancas, altares |

## Acciones de input

Nombres **exactos**, tal como se crean en `Project Settings → Input Map`. Usar estos
literales en el código; no inventar variantes. Este desajuste rompe el juego sin dar
ningún error visible.

| Acción | Tecla |
|---|---|
| `move_up` / `move_down` / `move_left` / `move_right` | W / S / A / D |
| `attack_primary` | Clic izquierdo |
| `attack_secondary` | Clic derecho |
| `reload` | R |
| `interact` | E |
| `weapon_next` / `weapon_prev` | Rueda arriba / abajo |
| `dodge` | Espacio |
| `pause` | Esc |

## Compilación y errores frecuentes

- Godot compila al ejecutar, pero conviene `dotnet build` desde la terminal integrada de
  VS Code para ver los errores antes.
- **Godot no encuentra las clases C#:** `Project → Tools → C# → Create C# solution` y
  reiniciar el editor. Es el fallo número uno al empezar.
- **Una clase nueva no aparece al asignar un script:** hay que compilar al menos una vez
  después de crearla.
- **Cambios en C# que no surten efecto:** Godot está ejecutando el ensamblado antiguo.
  Cerrar el juego, recompilar, reabrir.
