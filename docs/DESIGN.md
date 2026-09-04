# DESIGN

> Ábrelo cuando trabajes en algo que afecte a cómo se juega: control, cámara, IA,
> armas, misiones, progresión.

## Concepto

Shooter de acción con **cámara cenital**, ambientación gótico-medieval. El jugador es
un cazador que limpia aldeas, criptas y bosques infestados de demonios, vampiros y
licántropos.

Referencia estructural: Alien Shooter — vista cenital, misiones con objetivos, armas
recogidas del suelo, tienda entre misiones. **Diferencia clave:** no hay hordas ni armas
pesadas. Hay pocos enemigos, muy peligrosos, y armas medievales. El combate premia el
posicionamiento, la gestión de munición y saber cuándo cambiar de arma.

No se reutiliza nombre, arte, sonido ni assets de Alien Shooter. Solo la estructura de
género, que es libre.

## Sensación objetivo

Al terminar una partida de diez minutos el jugador debe pensar **"casi no lo cuento"**,
no "he arrasado con todo".

Tres reglas que se derivan de eso y que condicionan el código:

1. **La munición a distancia es escasa** y las recargas son lentas. Quedarse sin flechas
   en mitad de una sala es una situación normal, no un fallo de diseño.
2. **El cuerpo a cuerpo siempre está disponible**, como recurso de reserva. Nunca te
   quedas indefenso, pero pelear de cerca es arriesgado.
3. **Todo ataque enemigo se telegrafía** antes de ejecutarse: cambio de postura, sonido
   propio, brillo. Si el jugador muere, tiene que sentir que fue culpa suya. Un enemigo
   que hace daño sin aviso previo es un bug de diseño, aunque el código funcione.

## Cámara y control

- Mundo **3D real** (`Node3D`) con cámara cenital fija inclinada **60°**, a unos 14 m
  del jugador, con seguimiento suavizado. Se lee como un juego 2D pero permite luces
  dinámicas, sombras y partículas sin pre-renderizar sprites en 16 direcciones.
- Movimiento WASD **relativo a la cámara**, no a la rotación del personaje. Pulsar W
  siempre mueve hacia arriba de la pantalla.
- **Apuntado:** rayo desde la cámara contra un plano horizontal a la altura del pecho
  del jugador. El personaje mira a ese punto. Cuerpo y mirada son independientes: se
  puede retroceder disparando hacia delante, y eso es central en el combate.
- Esquiva con breve invulnerabilidad. *(Confirmar si entra en el hito 1 o más tarde.)*

## Bucles

**Segundos (combate):** detectar amenaza → posicionarse → disparar o esquivar →
recargar retrocediendo → rematar de cerca si se acerca demasiado.

**Minutos (misión):** entrar al mapa → cumplir objetivos → recoger munición, oro y armas
→ llegar a la salida.

**Horas (campaña):** completar misión → gastar oro en la tienda → siguiente misión, más difícil.

## Armas

Sistema **guiado por datos**: cada arma es un `WeaponData : Resource` en un `.tres`.
Añadir un arma no debe requerir escribir una clase nueva.

Campos: nombre, tipo (melee/distancia), daño, cadencia, cargador, tiempo de recarga,
dispersión, alcance, tipo de munición, velocidad del proyectil, escena del modelo, sonidos.

| Arma | Tipo | Rasgo de diseño |
|---|---|---|
| Espada corta | Melee | Rápida, arco estrecho, siempre disponible |
| Maza | Melee | Lenta, aturde, ignora armadura |
| Arco corto | Distancia | Munición abundante, daño medio, flechas recuperables |
| Ballesta | Distancia | Daño alto, recarga muy lenta, atraviesa un enemigo |
| Pistola de chispa | Distancia | Un disparo, daño brutal, recarga larguísima, ruidosa |
| Antorcha | Utilidad | Ilumina, prende fuego, ahuyenta a algunos monstruos |

**Munición separada por tipo** (flechas, virotes, pólvora). No hay munición universal:
es justamente lo que obliga a alternar armas.

## Bestiario

Cada arquetipo **obliga a jugar distinto**. Si dos enemigos se combaten igual, uno sobra.

1. **Licántropo** — rápido, embiste en línea recta, poca vida.
   *Castiga quedarse quieto.* Se combate retrocediendo y disparando.
2. **Vampiro** — velocidad media, esquiva lateralmente, puede desaparecer y reaparecer
   más cerca. *Castiga apuntar mal.* Se combate con cadencia alta.
3. **Demonio menor** — lento, mucha vida y armadura, ataque devastador de cerca.
   *Castiga el pánico.* Se combate con paciencia, espacio y munición pesada.

*Más adelante: gárgola voladora, nigromante invocador, jefe de misión.*

**Máquina de estados común:**
`Inactivo → Alerta → Persecución → Preparar ataque → Atacar → Recuperación → Muerte`
con `Aturdido` y `Huyendo` opcionales según el tipo.

## Misiones

Mapa cerrado, 2-4 objetivos, una salida. Tipos de objetivo que cubren casi todo el juego:

- **Purga** — eliminar a todos los monstruos de una zona marcada
- **Llave** — encontrar un objeto que abre un paso
- **Destrucción** — romper nidos, ataúdes o altares, con oleada de defensa mientras
- **Escolta** — llevar a un aldeano vivo hasta la salida
- **Supervivencia** — aguantar X segundos hasta que se abra la puerta

Al terminar, **pantalla de resumen**: bajas, precisión, oro obtenido, tiempo.

## Economía y progresión

Oro y objetos se recogen del suelo durante la misión. Entre misiones se abre la
**tienda**: armas nuevas, munición y mejoras del cazador (vida, velocidad de recarga,
capacidad de munición).

Las armas encontradas en el mapa se conservan; la tienda sirve para conseguir antes lo
que tardarías en encontrar.

**Guardado solo entre misiones**, nunca dentro del nivel: más simple de programar y más
tenso de jugar.
