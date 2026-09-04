# ROADMAP

> **Léelo al empezar cualquier tarea.** Aquí está el hito activo y el criterio que
> decide si está terminado.

**Regla:** no se empieza un hito sin que el anterior cumpla **todos** sus criterios de
cierre. Nada de "ya lo arreglo luego".

---

## Estado actual

**Hito activo:** 1 — Primer combate
**Hecho:** hito 0 completo. Proyecto .NET compilando, Input Map con las 12 acciones y las
8 capas de física configuradas, y `scenes/levels/TestRoom.tscn` con sala cerrada, jugador
que se mueve relativo a la cámara y apunta al cursor, y cámara cenital con suavizado.
**Siguiente tarea:** `HealthComponent` y `StateMachine` reutilizables, y un licantropo que
pase de `Inactivo` a `Alerta` y `Persecución`. El ataque telegrafiado va después.

**Bloqueos:** ninguno.

---

## Hito 0 — Andamiaje

Proyecto en marcha y el jugador se mueve.

Cerrado cuando:
- [x] El proyecto abre en Godot y compila en Visual Studio sin errores
- [x] Git inicializado con `.gitignore` de Godot (`.godot/`, `bin/`, `obj/`, `.vs/`)
- [x] La cápsula se mueve con WASD **relativo a la cámara**, no a su propia rotación
- [x] Mira siempre al cursor del ratón, en cualquier punto de la pantalla
- [x] La cámara la sigue con suavizado, sin tirones
- [x] No atraviesa las paredes de la sala de prueba
- [x] Consola limpia: cero errores y cero avisos

## Hito 1 — Primer combate

El bucle mínimo del juego: matar o morir.

Cerrado cuando:
- [ ] Un licántropo pasa por `Inactivo → Alerta → Persecución → Preparar → Atacar`
- [ ] **Telegrafía el ataque** de forma visible y audible antes de ejecutarlo
- [ ] Puede matar al jugador; el jugador puede matarlo con la espada
- [ ] Ambas muertes reinician la escena limpiamente
- [ ] La vida vive en un `HealthComponent` reutilizable, no en la clase del enemigo
- [ ] Consola limpia

## Hito 2 — Armas

El sistema guiado por datos que sostiene todo lo demás.

Cerrado cuando:
- [ ] Existen dos armas definidas **solo con archivos `.tres`**, sin clases nuevas
- [ ] Se cambia entre ellas con rueda o teclas
- [ ] El arco dispara proyectiles con munición finita y se recarga
- [ ] La munición está separada por tipo (flechas / virotes / pólvora)
- [ ] Se pueden recoger armas del suelo
- [ ] **Prueba real:** añadir una tercera arma no requiere tocar ningún `.cs`

## Hito 3 — Bestiario

Cerrado cuando:
- [ ] Los tres arquetipos coexisten en una escena
- [ ] Cada uno se juega distinto: quedarse quieto mata contra el licántropo, apuntar
      mal mata contra el vampiro, el pánico mata contra el demonio
- [ ] Cada uno tiene su aviso sonoro propio
- [ ] **Prueba real:** añadir un cuarto tipo no requiere tocar clases existentes

## Hito 4 — Misión jugable

Cerrado cuando:
- [ ] Una misión se completa de principio a fin
- [ ] Los objetivos se rastrean y se muestran en el HUD
- [ ] La salida se desbloquea al cumplirlos
- [ ] Pantalla de resumen: bajas, precisión, oro, tiempo
- [ ] Se puede perder y reintentar

## Hito 5 — Metajuego

Cerrado cuando:
- [ ] Se encadenan dos misiones seguidas
- [ ] Entre ellas se gasta oro en la tienda y el efecto se nota al jugar
- [ ] El progreso sobrevive a cerrar y reabrir el juego
- [ ] Guardado **solo entre misiones**, nunca dentro del nivel

## Hito 6 — Arte y pulido

Modelos finales, audio, efectos, menús. Alcance abierto; se define al llegar.

---

## Después del hito 6

Aparcadero de ideas. **Nada de aquí se implementa antes de tiempo.** Cuando surja una
idea nueva fuera del hito activo, va a esta lista.

- (vacío)
