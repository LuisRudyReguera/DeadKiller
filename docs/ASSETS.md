# ASSETS

> Ábrelo cuando importes modelos, sonidos o placeholders.

## Placeholders generados por código

Las siluetas provisionales del cazador y de los tres monstruos salen de
`tools/blender/make_placeholders.py`, que se ejecuta con Blender sin abrirlo:

```
"C:\Program Files\Blender Foundation\Blender 5.2lender.exe" --background ^
    --python tools/blender/make_placeholders.py
```

Aplica las convenciones de más abajo por su cuenta: escala en metros, mirando a −Z,
origen en los pies y todas las transformaciones aplicadas. Salen a `models/*.glb`.

**Sustituirlas por modelos de verdad no toca código.** El cuerpo de una entidad puede ser
una malla suelta o un `.glb` con varias dentro: `Meshes.CollectFrom` recorre lo que haya,
y el color del arquetipo y el destello se aplican a todas. Basta con reemplazar el `.glb`.

**Lo que estas siluetas ya fijan** y conviene respetar al sustituirlas: la anchura de
hombros y la huella en el suelo de cada arquetipo. Es lo único que se lee desde la cámara
cenital, y es lo que hace que se distingan de un vistazo.

## Regla principal

**Ninguna tarea de código se bloquea esperando arte.** Durante los hitos 0-3 todo son
placeholders: cápsulas y cubos grises, o personajes CC0 ya riggeados. Los modelos
definitivos llegan en el hito 6. El código es idéntico con un lobo genérico o con el
definitivo.

## Convenciones de importación

| | Regla |
|---|---|
| Escala | 1 unidad de Godot = 1 metro. Un humano mide ~1,8 |
| Orientación | El personaje mira hacia **−Z** |
| Origen | En los pies, centrado en X y Z |
| Formato | `.glb` |
| Densidad | 3.000-8.000 tris por monstruo |
| Antes de exportar | En Blender: `Object → Apply → All Transforms` |

**A vista cenital nunca se ve la cara.** No inviertas tiempo en detalle facial. Lo que se
lee desde arriba es la silueta, los hombros y el contraste de color. Diseña para eso.

## Licencias

Todo asset externo se anota en `docs/ATTRIBUTIONS.md` con su origen y licencia, en el
mismo momento en que se añade. Bibliotecas CC0 recomendadas para placeholders:
KayKit, Quaternius, Kenney, PolygonalMind. Usan rigs humanoides estándar pensados para
retargeting.

---

## Generación con IA

Sirve muy bien para **props y armas**: un modelo texturizado en menos de un minuto.
Funciona a medias para **personajes**: la topología sale caótica y hay que retopologizar
para producción. El cuello de botella real es la **animación**, no el modelado: los
auto-riggers resuelven bípedos y algún cuadrúpedo, pero las anatomías raras (alas, cuatro
brazos, tentáculos) siguen requiriendo trabajo manual.

Por eso los personajes de los hitos 1-3 salen de bibliotecas CC0 ya riggeadas, y la IA se
reserva para objetos.

### Estructura de un buen prompt

`sujeto + estilo + material + detalle clave + restricción técnica`, en inglés, frases
cortas separadas por comas.

**Coletilla de estilo fija.** Úsala idéntica en todos los assets; es lo que hace que el
juego parezca de una sola mano:

```
dark fantasy, stylized realism, low poly, clean topology, PBR textures,
single object, neutral background
```

### Armas

```
medieval short sword, worn steel blade with chipped edge, leather-wrapped grip,
brass crossguard, [coletilla]
```
```
medieval crossbow, dark stained oak stock, iron mechanism, taut hemp string,
weathered, [coletilla]
```
```
flintlock pistol, 17th century, ornate engraved barrel, walnut grip, brass fittings,
[coletilla]
```
```
short recurve bow, dark wood, bone tips, wrapped leather grip, hunter's weapon,
[coletilla]
```
```
wooden torch, burning, iron bracket, rough cloth wrapping, medieval, [coletilla]
```

### Monstruos

Repite siempre `full body, T-pose, symmetrical, game ready character`. Es lo que separa
un modelo riggeable de un adorno.

```
werewolf creature, full body, T-pose, symmetrical, game ready character,
dark matted fur, elongated snout, muscular hunched posture, torn peasant clothes,
[coletilla]
```
```
gothic vampire, full body, T-pose, symmetrical, game ready character,
pale gaunt skin, tattered black noble coat, long claws, sunken red eyes, [coletilla]
```
```
lesser demon, full body, T-pose, symmetrical, game ready character,
cracked charred skin with glowing embers between the cracks, curved horns,
heavy armored shoulders, hulking build, [coletilla]
```

### Entorno

```
gothic gravestone, weathered granite, moss patches, cracked, [coletilla]
```
```
wooden barrel, iron bands, worn planks, medieval, [coletilla]
```
```
stone crypt door, gothic arch, carved runes, heavy iron hinges, aged stone, [coletilla]
```

### Errores frecuentes

- **Pedir escenas.** "a werewolf in a dark forest" genera también el bosque. De ahí
  `single object, neutral background` en la coletilla.
- **Pedir poses dinámicas** en algo que vas a animar. `T-pose` siempre.
- **Adjetivos en vez de materiales.** "worn steel with chipped edge" funciona; "cool
  sword" no.
- **Mezclar estilos.** La coherencia entre monstruos importa más que la calidad de cada
  uno por separado.
