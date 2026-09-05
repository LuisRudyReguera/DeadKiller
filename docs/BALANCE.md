# BALANCE

> Ábrelo cuando necesites cualquier número.
>
> **Todos estos valores son provisionales.** Existen para que los sistemas se construyan
> en una escala común, no porque estén probados. Se ajustan jugando.
>
> Si necesitas un número que no está aquí: **propónlo explícitamente, di de dónde sale, y
> añádelo a este documento.** No lo entierres en el código.

## Jugador

| | Valor |
|---|---|
| Vida | 100 |
| Velocidad | 5,5 m/s |
| Cápsula | 0,4 m de radio, 1,8 m de alto |
| Altura del plano de apuntado | 1,2 m (pecho) |
| Esquiva | 0,25 s de duración, 4 m de desplazamiento, 3 s de recarga |
| Invulnerabilidad tras recibir daño | 0,4 s |

## Enemigos

| | Vida | Velocidad | Daño | Aviso previo | Alcance | Oro |
|---|---|---|---|---|---|---|
| Licántropo | 40 | 5 m/s (11 al embestir) | 15 | 0,4 s | 1,5 m | 5-10 |
| Vampiro | 60 | 5 m/s | 12 | 0,5 s | 1,8 m | 10-20 |
| Demonio menor | 150 (+5 armadura plana) | 2,5 m/s | 40 | 0,9 s | 2,2 m | 25-40 |

**Armadura plana:** se resta del daño recibido, con un mínimo de 1. La maza la ignora.

### Tiempos de la máquina de estados

Provisionales, salidos de montar los hitos 1 y 3. Se ajustan jugando. Todos viven en el
`.tres` de cada arquetipo, en `resources/enemies/`.

| | Licántropo | Vampiro | Demonio | Por qué se diferencian |
|---|---|---|---|---|
| Duración de `Alerta` | 0,6 s | 0,5 s | 0,9 s | El demonio tarda en arrancar; es lo que lo hace evitable |
| Distancia a la que prepara | 2,5 m | 2,8 m | 3,2 m | Escala con el alcance de cada uno |
| Velocidad del arranque | 11 m/s | 8 m/s | 6 m/s | El licántropo embiste; el demonio solo se echa encima |
| Duración del arranque | 0,25 s | 0,2 s | 0,35 s | |
| Duración de `Recuperación` | 0,6 s | 0,5 s | 1,1 s | La del demonio es larga a propósito: es tu turno |
| Zigzag lateral | 0 | 0,8 | 0 | Lo que hace que fallarle al vampiro sea fácil |
| Intervalo de parpadeo | — | 1,2 s | — | Reaparece 4 m más cerca; castiga confiarse por la distancia |

Distancia a la que pierden al jugador: 16 m para los tres. Son los 12 m de visión más
4 de histéresis, para que no entren y salgan de persecución justo en el borde.

**Aviso previo:** segundos entre que el enemigo empieza a telegrafiar y el golpe conecta.
Nunca puede ser 0. Es el margen que tiene el jugador para reaccionar y define si el juego
es justo.

## Armas

| | Daño | Cadencia / recarga | Munición | Notas |
|---|---|---|---|---|
| Espada corta | 25 | 0,5 s | — | Arco de 60°, alcance 2 m |
| Maza | 45 | 1,1 s | — | Aturde 0,8 s; ignora armadura |
| Arco corto | 30 | 0,8 s | Flechas | Proyectil a 25 m/s; recuperables |
| Ballesta | 70 | 2,5 s de recarga | Virotes | Atraviesa un enemigo |
| Pistola de chispa | 120 | 4,0 s de recarga | Pólvora | Un disparo por carga |
| Antorcha | 5/s | — | — | Ilumina, prende fuego, ahuyenta |

**Cómo se traducen estos números al `.tres`:** en las armas a distancia la cadencia
**es** el tiempo de recarga, porque todas tienen cargador de 1. El arco no tiene una
cadencia de 0,8 s aparte: tiene un cargador de una flecha y 0,8 s de recarga, que da
exactamente lo mismo y además hace visible la recarga.

Valores que hubo que proponer porque no estaban aquí: alcance de los proyectiles 30 m
(la sala mide 20×20), velocidad de la ballesta 35 m/s y de la pistola 60 m/s, y
dispersión de 1,5° en el arco y 3° en la pistola. La ballesta no dispersa.

**Comprobación rápida:** con la espada, un licántropo cae en 2 golpes, un vampiro en 3
y un demonio en 8. Al demonio conviene dispararle. Esa es la intención.

## Munición

| Tipo | Máximo | Usan |
|---|---|---|
| Flechas | 60 | Arco corto |
| Virotes | 20 | Ballesta |
| Pólvora | 8 | Pistola de chispa |

Las flechas se pueden recuperar del suelo tras dispararlas (~50% de recuperación).

## Densidad y ritmo

| | Valor |
|---|---|
| Enemigos activos en pantalla | 3-8 |
| Enemigos por misión | ~20 máximo |
| Duración objetivo de una misión | 8-12 minutos |
| Distancia de detección del jugador | 12 m (visión), 20 m (sonido de disparo) |

## Cámara

| | Valor |
|---|---|
| Inclinación | 60° |
| Distancia al jugador | 14 m |
| Suavizado de seguimiento | 8 (interpolación por segundo) |
| Proyección | Perspectiva, FOV 50 |

## Registro de ajustes

> Cuando un valor cambie tras jugarlo, anótalo aquí con el motivo. Evita revertir sin
> querer un ajuste que costó encontrar.

| Fecha | Valor | Antes → Después | Motivo |
|---|---|---|---|
| 2026-09-05 | Velocidad del licántropo | 7 → 5 m/s | A 7 m/s era más rápido que el jugador (5,5) y no se podía retroceder nunca. `DESIGN.md` dice que se combate «retrocediendo y disparando», así que perseguir más rápido que el jugador contradecía su propio contraataque. La embestida sigue a 11 m/s: la amenaza está ahí, no en la persecución. |
