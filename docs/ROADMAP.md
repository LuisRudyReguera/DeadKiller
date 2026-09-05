# ROADMAP

> **Léelo al empezar cualquier tarea.** Aquí está el hito activo y el criterio que
> decide si está terminado.

**Regla:** no se empieza un hito sin que el anterior cumpla **todos** sus criterios de
cierre. Nada de "ya lo arreglo luego".

---

## Estado actual

**Hito activo:** 4 — Misión jugable
**Hecho:** hitos 0 a 3 cerrados. El hito 4 está **implementado y verificado con ventana**:
`scenes/levels/Mission01.tscn` es ahora la escena principal, con dos objetivos, HUD, salida
que solo se abre al cumplirlos y pantalla de resumen con bajas, puntería, oro y tiempo.
`TestRoom.tscn` se conserva como banco de pruebas rápido.
**Siguiente tarea:** jugarla entera. Falta comprobar si la misión se puede completar sin
morir, si 30×30 m con tres monstruos es el tamaño correcto, y si la munición de partida
llega.

**Bloqueos:** ninguno. Nota: al **cerrar** el juego salen dos líneas sobre instancias
sin liberar. Está investigado y es del servidor de audio de Godot, no nuestro (D-015).
Durante la partida la consola está limpia.

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
- [x] Un licántropo pasa por `Inactivo → Alerta → Persecución → Preparar → Atacar`
- [x] **Telegrafía el ataque** de forma visible y audible antes de ejecutarlo
- [x] Puede matar al jugador; el jugador puede matarlo con la espada
- [x] Ambas muertes reinician la escena limpiamente
- [x] La vida vive en un `HealthComponent` reutilizable, no en la clase del enemigo
- [x] Consola limpia

## Hito 2 — Armas

El sistema guiado por datos que sostiene todo lo demás.

Cerrado cuando:
- [x] Existen dos armas definidas **solo con archivos `.tres`**, sin clases nuevas
- [x] Se cambia entre ellas con rueda o teclas
- [x] El arco dispara proyectiles con munición finita y se recarga
- [x] La munición está separada por tipo (flechas / virotes / pólvora)
- [x] Se pueden recoger armas del suelo
- [x] **Prueba real:** añadir una tercera arma no requiere tocar ningún `.cs`

## Hito 3 — Bestiario

Cerrado cuando:
- [x] Los tres arquetipos coexisten en una escena
- [x] Cada uno se juega distinto: quedarse quieto mata contra el licántropo, apuntar
      mal mata contra el vampiro, el pánico mata contra el demonio
- [x] Cada uno tiene su aviso sonoro propio
- [x] **Prueba real:** añadir un cuarto tipo no requiere tocar clases existentes

## Hito 4 — Misión jugable

Cerrado cuando:
- [x] Una misión se completa de principio a fin
- [x] Los objetivos se rastrean y se muestran en el HUD
- [x] La salida se desbloquea al cumplirlos
- [x] Pantalla de resumen: bajas, precisión, oro, tiempo
- [x] Se puede perder y reintentar

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
