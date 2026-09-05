# DECISIONS

> Ábrelo cuando vayas a decidir algo estructural, o cuando algo ya decidido te parezca
> discutible.
>
> **Para qué sirve:** cada sesión de Claude Code empieza sin memoria de las anteriores.
> Sin este archivo, las mismas decisiones se vuelven a discutir cada semana y el código
> acaba con dos formas de hacer lo mismo.
>
> **Cómo se usa:** una entrada por decisión estructural. Escribe también **por qué se
> descartó la alternativa** — es la parte que evita revertir sin querer. Propón las
> entradas nuevas al desarrollador; no las añadas por tu cuenta.

---

### D-001 · 3D real con cámara cenital, no sprites 2D

**Decidido.** El original de referencia usaba sprites pre-renderizados desde modelos 3D.
Hoy eso obliga a renderizar cada monstruo en 8 o 16 direcciones por cada animación.
En 3D real se obtiene el mismo aspecto con luces dinámicas, sombras y partículas, y el
apuntado se resuelve con un rayo contra un plano.
**Descartado:** 2D puro con sprites — coste de arte prohibitivo para un desarrollador solo.

### D-002 · C# en lugar de GDScript

**Decidido.** Preferencia del desarrollador y uso de Visual Studio.
**Coste asumido:** la mayoría de tutoriales de Godot están en GDScript y hay que
traducirlos. Obliga a usar el build .NET de Godot.

### D-003 · Pocos enemigos, no hordas

**Decidido.** Cambia el género respecto a la referencia: de shooter de hordas a combate
táctico cenital. Permite enemigos con IA real y evita necesitar object pooling y
MultiMesh desde el primer día.
**Consecuencia:** las armas son medievales y de cadencia baja; la munición escasea.

### D-004 · Armas guiadas por datos (`Resource`)

**Decidido.** Cada arma es un `.tres`, no una clase. Añadir un arma no debe requerir
código nuevo. Es la prueba de cierre del hito 2.
**Descartado:** una clase por arma — se vuelve inmanejable a partir de la cuarta.

### D-005 · Guardado solo entre misiones

**Decidido.** Más simple de programar y más tenso de jugar que los puntos de guardado
dentro del nivel.
**Revisable** si las misiones acaban durando más de 15 minutos.

### D-006 · Placeholders CC0 hasta el hito 6

**Decidido.** La generación de modelos con IA funciona bien para props, pero animar
criaturas sigue siendo el cuello de botella. Los personajes de los hitos 1-3 salen de
bibliotecas CC0 ya riggeadas.
**Descartado:** modelar los tres monstruos antes de programar — dos semanas sin nada
jugable.


### D-007 · VS Code en lugar de Visual Studio 2022

**Decidido.** VS Code ya estaba instalado y con la extensión C# Dev Kit cubre lo que
necesita el proyecto: IntelliSense, `dotnet build` y depuración adjunta a Godot.
**Descartado:** Visual Studio 2022 Community — unos 10 GB de instalación para un
beneficio marginal en un proyecto de un solo desarrollador.
**Matiza D-002**, que justificaba C# en parte por «uso de Visual Studio». La elección de
C# se mantiene por preferencia del desarrollador.

### D-008 · La esquiva no entra en el hito 1

**Decidido.** Los criterios de cierre del hito 1 no la mencionan y el bucle mínimo
—matar o morir— se sostiene sin ella. Esquivar solo significa algo cuando hay varios
ataques telegrafiados que leer, y eso llega con el bestiario.
**Revisable** en el hito 3, cuando coexistan los tres arquetipos.

### D-009 · Los estados mueven a la entidad, no `_PhysicsProcess`

**Decidido.** El `Werewolf` no tiene `_PhysicsProcess` propio: son los estados los que
llaman a `MoveTowardsTarget`, `StandStill` o `Lunge`, y cada uno de ellos termina en
`MoveAndSlide`.
**Descartado:** que la entidad se moviera sola leyendo una variable que fijan los
estados — en Godot los padres se procesan antes que los hijos, así que cada orden
llegaría un fotograma tarde.

### D-010 · Audio de placeholder generado por código

**Decidido.** Los WAV del hito 1 los genera `tools/placeholder-audio/make_sounds.py` con
la biblioteca estándar de Python. Son ondas sintetizadas, no grabaciones.
**Por qué:** ASSETS.md exige que ninguna tarea de código se bloquee esperando arte, y
el telegrafiado audible es criterio de cierre del hito 1.
**Se sustituyen** por sonido real en el hito 6. El código no cambia: solo el `.wav`.

### D-011 · Soltar las referencias a `Resource` en `_ExitTree`

**Decidido.** Todo componente de C# que guarde un `Resource` en un campo lo pone a `null`
al salir del árbol.
**Por qué:** un campo de C# mantiene vivo el recurso más allá del cierre del motor, y
Godot lo denuncia con «resources still in use at exit». Ha aparecido tres veces en este
proyecto: al duplicar el material del licántropo, y al guardar el arma equipada y su
sonido en el `WeaponHolder`. La consola limpia es criterio de cierre de todos los hitos.

### D-012 · La entrada de acciones se lee por evento, nunca sondeando

**Decidido.** Atacar, recargar y cambiar de arma se recogen en `_UnhandledInput`, no con
`Input.IsActionPressed` dentro de `_PhysicsProcess`. El ataque además espera 0,15 s en un
búfer por si llega durante la cadencia.
**Por qué:** el sondeo pierde las pulsaciones más cortas que un fotograma de física
(16 ms), y la acción desaparece sin dar ningún error. Se detectó jugando: parecía que el
ataque tenía retardo, pero eran espadazos que no llegaban a ocurrir.
**La rueda del ratón obliga:** no tiene estado «pulsado» que sondear, solo eventos.
**Se mantiene el sondeo** solo para el movimiento, donde mantener la tecla es la norma.

### D-013 · Un solo `Enemy` guiado por `EnemyData`, no una clase por monstruo

**Decidido.** No existe una clase `Werewolf`. Hay un `Enemy` genérico, un `EnemyData` en
`.tres` por arquetipo, y una bolsa de estados reutilizables. Lo que distingue a un
licántropo de un vampiro son sus números y qué estados cuelgue su escena.
**Por qué:** es la prueba de cierre del hito 3, que exige que añadir un cuarto tipo no
toque ninguna clase existente. Un cuarto arquetipo es hoy un `.tres` y una escena; solo
necesitaría código si trajera un comportamiento que ningún estado cubre, y entonces sería
una clase **nueva**, no una modificación.
**Descartado:** herencia `Werewolf : Enemy`. Tres monstruos que solo se diferencian en
números no justifican tres clases, y contradice «composición sobre herencia».

### D-014 · Los cadáveres salen del grupo y dejan de colisionar

**Decidido.** Al morir, un enemigo se borra del grupo `enemy` y pone su capa de colisión
a 0, pero el nodo **no** se destruye.
**Por qué:** un cadáver no debe contar como enemigo vivo en una purga ni bloquear el paso,
pero el nodo tiene que seguir existiendo porque de él saldrá el oro en el hito 4.
**Se detectó** probando: el jugador de prueba se quedaba golpeando a un muerto porque
seguía siendo el objetivo más cercano.

### D-015 · La fuga de audio al cerrar es del motor, no nuestra

**Investigado y cerrado.** Al cerrar el juego aparecen en la consola:

```
WARNING: 2 ObjectDB instances were leaked at exit
ERROR: 1 resources still in use at exit  (AudioStreamPlaybackWAV)
```

**No es nuestro código.** Se comprobó: `_ExitTree` sí se ejecuta en todos los nodos, los
`AudioStreamPlayer3D` están parados (`Playing == false`) y sueltan su `Stream` antes de
desaparecer. El objeto que queda vivo es un `AudioStreamPlaybackWAV` interno del servidor
de audio de Godot, que no se libera antes de que el motor haga su comprobación de fugas.

**Se descartó por medición**, no por suposición: no es el recolector de basura de C#
(forzarlo no cambia nada), no escala con los reinicios de escena, y desaparece si ningún
sonido llega a reproducirse.

**Qué significa para «consola limpia»:** ese criterio se refiere a errores **durante la
partida**. Este mensaje sale solo al cerrar y no afecta a nada. Si algún día molesta, la
vía es reportarlo aguas arriba, no contorsionar nuestro código.

**No volver a investigarlo** sin una razón nueva. Costó bastante llegar aquí.

### D-016 · La misión es el punto de encuentro, y los objetivos son nodos

**Decidido.** `Mission` es un nodo que lleva los objetivos como hijos, las cifras del
resumen y el estado de la salida. Una moneda del suelo no sabe qué es un objetivo: se lo
cuenta a la misión y ella decide si eso cumple algo.
**Por qué:** deja añadir tipos de objetivo (`PurgeObjective`, `CollectObjective`, y los de
escolta o supervivencia que vengan) sin tocar nada de lo existente, igual que las armas y
los enemigos.
**Descartado:** que cada objetivo buscara por su cuenta lo que necesita vigilar. Acabaría
con cada objetivo conociendo media escena.

### D-017 · Registrarse en un grupo va en `_EnterTree`, no en `_Ready`

**Decidido.** Todo lo que se busca por grupo —jugador, enemigos, misión— se apunta al
grupo en `_EnterTree`.
**Por qué:** `_Ready` corre en orden de árbol, así que un nodo colocado antes en la escena
no encuentra al que se registra después. Pasó de verdad: la salida del nivel no encontraba
la misión solo por estar más arriba en la escena. Todos los `_EnterTree` ocurren antes que
cualquier `_Ready`, así que el orden dentro de la escena deja de importar.

### D-018 · El guardado es JSON plano, no un `Resource`

**Decidido.** El perfil (`GameProfile`) es un objeto de C# corriente que se serializa a
`user://profile.json`. No es un `Node` ni un `Resource`.
**Por qué:** el formato del guardado no debe depender de cómo esté hecha una escena hoy.
Un `.tres` guardado arrastra rutas y tipos; si mañana se renombra una clase, las partidas
guardadas dejan de cargar. El JSON solo tiene números con nombre.
**Además:** un guardado corrupto no impide jugar — se avisa y se empieza de cero.
**Se escribe en dos sitios y solo dos:** al comprar en la tienda y al cerrar una misión
ganada. Nunca dentro del nivel (D-005).

### D-019 · Las mejoras se aplican en un solo sitio, `Loadout`

**Decidido.** Un nodo `Loadout` en cada misión lee el perfil y lo vuelca sobre la vida, la
bolsa de munición y el soporte de armas del jugador.
**Por qué:** es la única frontera entre lo guardado y el juego. El `PlayerController`, las
armas y los enemigos siguen sin saber que existe un perfil, y una misión de prueba sin
`Loadout` se juega con los valores base.

### D-020 · El cuerpo de una entidad es un nodo, no una malla

**Decidido.** Todo lo que pinta o destella un personaje pasa por `Meshes.CollectFrom`,
que recoge todas las mallas que cuelguen del nodo del cuerpo.
**Por qué:** un placeholder es una cápsula suelta, pero un modelo importado de un `.glb`
es una escena con varias mallas dentro y una raíz que **no** es un `MeshInstance3D`.
Atarse a una malla concreta obligaría a tocar código cada vez que llegue un modelo nuevo.
**Consecuencia:** sustituir un monstruo de placeholder por el definitivo es cambiar un
archivo. Ni una línea de C#.

---

## Pendientes de decidir

- ¿Al morir se pierde la misión entera o hay reintentos limitados?
- Nombre definitivo del juego.
