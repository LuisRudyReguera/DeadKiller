# -*- coding: utf-8 -*-
"""
Genera las siluetas de placeholder del cazador y de los tres monstruos, y las exporta
a `models/` en .glb siguiendo las convenciones de docs/ASSETS.md.

    "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background ^
        --python tools/blender/make_placeholders.py

No son modelos definitivos: son volúmenes proporcionados. Lo que se lee desde la cámara
cenital es la silueta, la anchura de hombros y la huella en el suelo, y eso sí queda
fijado aquí. El detalle llega en el hito 6 sustituyendo el .glb, sin tocar código.

CONVENCIONES (docs/ASSETS.md):
  - 1 unidad = 1 metro; un humano mide ~1,8
  - El personaje mira hacia -Z en Godot, que es -Y en Blender antes de exportar
  - Origen en los pies, centrado en X y Z
  - Se aplican todas las transformaciones antes de exportar
"""

import math
import os
import sys

import bpy

# La raíz del proyecto son dos niveles por encima de tools/blender/.
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "models")


# ---------------------------------------------------------------- utilidades

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def box(size, loc, rot=(0.0, 0.0, 0.0)):
    """Caja de dimensiones `size` centrada en `loc`. Todo en metros."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.active_object
    obj.scale = (size[0], size[1], size[2])
    return obj


def blob(radius, loc, scale=(1.0, 1.0, 1.0)):
    """Esfera de pocos segmentos: da volumen sin disparar el número de triángulos."""
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=12, ring_count=8, location=loc)
    obj = bpy.context.active_object
    obj.scale = scale
    return obj


def cone(radius, depth, loc, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cone_add(radius1=radius, depth=depth, vertices=8,
                                    location=loc, rotation=rot)
    return bpy.context.active_object


def finish(parts, name):
    """Une las piezas, aplica transformaciones y deja el origen en los pies."""
    bpy.ops.object.select_all(action="DESELECT")

    for part in parts:
        part.select_set(True)

    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()

    obj = bpy.context.active_object
    obj.name = name

    # Sin esto el .glb llega a Godot con escalas y rotaciones pendientes.
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # Origen a los pies y centrado: es lo que espera la escena de Godot.
    lowest = min((obj.matrix_world @ v.co).z for v in obj.data.vertices)
    bpy.context.scene.cursor.location = (0.0, 0.0, lowest)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    obj.location = (0.0, 0.0, 0.0)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    bpy.ops.object.shade_flat()

    return obj


def export(obj, filename):
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, filename)

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,          # Blender es Z-arriba; Godot es Y-arriba
    )

    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print("[BLENDER] %s  ->  %d triangulos" % (filename, tris))


# ---------------------------------------------------------------- personajes
# Todos miran hacia -Y en Blender, que al exportar se convierte en -Z en Godot.

def build_hunter():
    """Cazador: erguido, 1,8 m, hombros normales, capa que ensancha la silueta."""
    parts = [
        box((0.42, 0.26, 0.85), (0.0, 0.0, 1.02)),                 # torso
        box((0.76, 0.20, 0.58), (0.0, 0.11, 1.22)),                # capa por la espalda
        blob(0.14, (0.0, 0.0, 1.60), (1.0, 1.05, 1.1)),            # cabeza
        box((0.19, 0.19, 0.62), (-0.29, 0.0, 1.05)),               # brazo izquierdo
        box((0.19, 0.19, 0.62), (0.29, 0.0, 1.05)),                # brazo derecho
        box((0.17, 0.17, 0.60), (-0.11, 0.0, 0.30)),               # pierna izquierda
        box((0.17, 0.17, 0.60), (0.11, 0.0, 0.30)),                # pierna derecha
        box((0.09, 0.46, 0.09), (0.26, -0.22, 1.12)),              # arma en la mano
    ]
    return finish(parts, "Hunter")


def build_werewolf():
    """
    Licántropo: encorvado, hombros muy anchos y hocico largo hacia delante. Más bajo
    que el cazador pero con el doble de huella: desde arriba se lee como una masa ancha.
    """
    parts = [
        box((0.52, 0.86, 0.46), (0.0, -0.06, 1.06), (math.radians(-18), 0, 0)),  # tronco inclinado
        box((1.06, 0.36, 0.38), (0.0, -0.16, 1.30)),                             # hombros
        blob(0.17, (0.0, -0.44, 1.34), (0.9, 1.15, 0.85)),                       # cabeza
        cone(0.11, 0.34, (0.0, -0.66, 1.28), (math.radians(-90), 0, 0)),         # hocico
        cone(0.05, 0.16, (-0.11, -0.40, 1.50)),                                  # oreja izquierda
        cone(0.05, 0.16, (0.11, -0.40, 1.50)),                                   # oreja derecha
        box((0.20, 0.20, 0.72), (-0.34, -0.14, 0.72)),                           # brazo izquierdo
        box((0.20, 0.20, 0.72), (0.34, -0.14, 0.72)),                            # brazo derecho
        box((0.22, 0.34, 0.46), (-0.17, 0.14, 0.34)),                            # pata izquierda
        box((0.22, 0.34, 0.46), (0.17, 0.14, 0.34)),                             # pata derecha
        box((0.12, 0.52, 0.12), (0.0, 0.40, 1.00), (math.radians(24), 0, 0)),    # cola
    ]
    return finish(parts, "Werewolf")


def build_vampire():
    """
    Vampiro: alto y estrecho, 1,9 m, con abrigo que se abre hacia abajo. La silueta
    es un triángulo invertido: hombros marcados y poca huella. Difícil de acertar.
    """
    parts = [
        box((0.30, 0.20, 0.92), (0.0, 0.0, 1.10)),                 # torso estrecho
        box((0.72, 0.30, 0.24), (0.0, 0.0, 1.48)),                 # hombros marcados
        box((0.84, 0.40, 0.74), (0.0, 0.04, 0.72)),                # faldón del abrigo
        blob(0.13, (0.0, 0.0, 1.72), (0.9, 1.0, 1.2)),             # cabeza alargada
        box((0.13, 0.13, 0.72), (-0.24, 0.0, 1.12)),               # brazo izquierdo
        box((0.13, 0.13, 0.72), (0.24, 0.0, 1.12)),                # brazo derecho
        cone(0.05, 0.20, (-0.30, -0.06, 0.72), (math.radians(180), 0, 0)),  # garra izquierda
        cone(0.05, 0.20, (0.30, -0.06, 0.72), (math.radians(180), 0, 0)),   # garra derecha
        box((0.14, 0.14, 0.52), (-0.09, 0.0, 0.26)),               # pierna izquierda
        box((0.14, 0.14, 0.52), (0.09, 0.0, 0.26)),                # pierna derecha
    ]
    return finish(parts, "Vampire")


def build_demon():
    """
    Demonio menor: 2,4 m, hombreras enormes y cuernos. Ocupa el doble que cualquier
    otro: desde arriba se ve venir de lejos, que es justo lo que lo hace evitable.
    """
    parts = [
        box((0.78, 0.52, 1.10), (0.0, 0.0, 1.36)),                 # torso macizo
        box((1.42, 0.60, 0.40), (0.0, 0.0, 1.86)),                 # hombreras
        blob(0.21, (0.0, -0.10, 2.14), (1.0, 1.0, 0.9)),           # cabeza hundida
        cone(0.09, 0.42, (-0.20, -0.06, 2.34), (math.radians(-24), 0, math.radians(-16))),  # cuerno izq.
        cone(0.09, 0.42, (0.20, -0.06, 2.34), (math.radians(-24), 0, math.radians(16))),    # cuerno der.
        box((0.30, 0.30, 0.94), (-0.60, 0.0, 1.30)),               # brazo izquierdo
        box((0.30, 0.30, 0.94), (0.60, 0.0, 1.30)),                # brazo derecho
        blob(0.24, (-0.62, -0.06, 0.82), (1.0, 1.1, 0.9)),         # puño izquierdo
        blob(0.24, (0.62, -0.06, 0.82), (1.0, 1.1, 0.9)),          # puño derecho
        box((0.32, 0.36, 0.82), (-0.24, 0.0, 0.41)),               # pierna izquierda
        box((0.32, 0.36, 0.82), (0.24, 0.0, 0.41)),                # pierna derecha
    ]
    return finish(parts, "Demon")


# ---------------------------------------------------------------- entrada

BUILDERS = {
    "hunter.glb": build_hunter,
    "werewolf.glb": build_werewolf,
    "vampire.glb": build_vampire,
    "demon.glb": build_demon,
}


def main():
    for filename, builder in BUILDERS.items():
        clear_scene()
        export(builder(), filename)

    print("[BLENDER] hecho: %d modelos en %s" % (len(BUILDERS), OUT_DIR))


if __name__ == "__main__":
    main()
    sys.exit(0)
