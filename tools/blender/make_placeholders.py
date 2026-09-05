# -*- coding: utf-8 -*-
"""
Genera las siluetas del cazador y de los monstruos y las exporta a `models/` en .glb.

    "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background ^
        --python tools/blender/make_placeholders.py

No son los modelos definitivos, pero YA NO SON VOLÚMENES GENÉRICOS: las proporciones,
la silueta y la paleta salen de las hojas de concepto del proyecto. Lo que fijan es lo
único que se lee desde la cámara cenital, así que el modelo definitivo debería
respetarlo aunque cambie todo lo demás.

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

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "models")


# ---------------------------------------------------------------- color

def srgb_to_linear(channel):
    """Blender trabaja en lineal; los códigos de las paletas están en sRGB."""
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def material(name, hex_color, emission=0.0):
    """Material plano a partir de un color hexadecimal de la paleta de referencia."""
    if name in bpy.data.materials:
        return bpy.data.materials[name]

    hex_color = hex_color.lstrip("#")
    rgb = tuple(srgb_to_linear(int(hex_color[i:i + 2], 16) / 255.0) for i in (0, 2, 4))

    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.85
    bsdf.inputs["Metallic"].default_value = 0.0

    if emission > 0.0:
        bsdf.inputs["Emission Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission

    return mat


# ---------------------------------------------------------------- primitivas

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

    for block in (bpy.data.meshes, bpy.data.materials):
        for item in list(block):
            if item.users == 0:
                block.remove(item)


def paint(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj


def box(size, loc, mat, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.active_object
    obj.scale = size
    return paint(obj, mat)


def blob(radius, loc, mat, scale=(1.0, 1.0, 1.0)):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=12, ring_count=8, location=loc)
    obj = bpy.context.active_object
    obj.scale = scale
    return paint(obj, mat)


def cone(radius, depth, loc, mat, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cone_add(radius1=radius, depth=depth, vertices=8,
                                    location=loc, rotation=rot)
    return paint(bpy.context.active_object, mat)


def disc(radius, depth, loc, mat, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=depth, vertices=16,
                                        location=loc, rotation=rot)
    return paint(bpy.context.active_object, mat)


def finish(parts, name):
    bpy.ops.object.select_all(action="DESELECT")

    for part in parts:
        part.select_set(True)

    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()

    obj = bpy.context.active_object
    obj.name = name

    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

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

    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True,
                              export_apply=True, export_yup=True)

    height = max((obj.matrix_world @ v.co).z for v in obj.data.vertices)
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print("[BLENDER] %-14s %4d tris   altura %.2f m" % (filename, tris, height))


# ---------------------------------------------------------------- cazador
# Paleta de la hoja "GOTHIC MONSTER HUNTER". Miran a -Y en Blender.

def build_hunter():
    """
    1,80 m exactos, como marca la hoja. Desde arriba lo que se ve es EL ALA DEL SOMBRERO
    —domina la silueta entera—, los hombros del gabán y el farol saliendo a un lado.
    Es literalmente lo que enseña su panel de legibilidad cenital.
    """
    duster = material("Duster", "#50392B")
    hat = material("Hat", "#382A23")
    skin = material("Skin", "#DCAA78")
    denim = material("Denim", "#243D54")
    iron = material("Iron", "#4B5158")
    scarf = material("Scarf", "#365B73")
    lantern = material("Lantern", "#F2CB83", emission=3.0)
    wood = material("Wood", "#70503A")

    parts = [
        # piernas y botas
        box((0.17, 0.17, 0.62), (-0.11, 0.0, 0.44), denim),
        box((0.17, 0.17, 0.62), (0.11, 0.0, 0.44), denim),
        box((0.20, 0.26, 0.16), (-0.11, -0.03, 0.08), iron),
        box((0.20, 0.26, 0.16), (0.11, -0.03, 0.08), iron),

        # gabán: se abre hacia abajo, es lo que ensancha la huella
        box((0.46, 0.30, 0.72), (0.0, 0.0, 1.10), duster),
        box((0.60, 0.40, 0.46), (0.0, 0.0, 0.80), duster),
        # esclavina sobre los hombros
        box((0.66, 0.38, 0.16), (0.0, 0.0, 1.42), duster),

        box((0.30, 0.24, 0.12), (0.0, -0.02, 1.50), scarf),
        blob(0.115, (0.0, 0.0, 1.64), skin, (1.0, 1.05, 1.05)),

        # el ala del sombrero: la pieza que de verdad identifica al personaje desde arriba
        disc(0.30, 0.05, (0.0, 0.0, 1.735), hat),
        disc(0.135, 0.16, (0.0, 0.0, 1.80), hat),

        # brazos
        box((0.15, 0.15, 0.56), (-0.27, 0.0, 1.14), duster),
        box((0.15, 0.15, 0.56), (0.27, 0.0, 1.14), duster),

        # ballesta cruzada a la espalda, en diagonal
        box((0.10, 0.10, 0.62), (0.0, 0.20, 1.24), wood, (0.0, math.radians(34), 0.0)),
        box((0.48, 0.08, 0.07), (0.0, 0.20, 1.34), iron, (0.0, math.radians(34), 0.0)),

        # farol al costado: el segundo identificador cenital, y ademas emite
        box((0.13, 0.13, 0.17), (0.30, -0.10, 0.86), lantern),
        box((0.04, 0.04, 0.12), (0.30, -0.10, 1.00), iron),

        # espada corta al otro costado
        box((0.06, 0.09, 0.44), (-0.26, 0.02, 0.84), iron, (math.radians(12), 0.0, 0.0)),
    ]
    return finish(parts, "Hunter")


# ---------------------------------------------------------------- licantropo
# Paleta y postura de la hoja "LUPINE STALKER".

def build_werewolf():
    """
    Encorvado y digitígrado, con la cruz más alta que la cabeza y una cola larga.
    La hoja lo dibuja mucho más ancho de hombros que de cadera: esa cuña invertida es
    lo que hay que conservar. Alto 1,75 m en postura encorvada.
    """
    fur = material("Fur", "#6E625B")
    fur_dark = material("FurDark", "#4A403B")
    ridge = material("Ridge", "#8A4A3C")
    claw = material("Claw", "#D8CBB4")
    eye = material("Eye", "#C4302B", emission=4.0)

    parts = [
        # tronco inclinado hacia delante
        box((0.62, 0.98, 0.52), (0.0, -0.10, 1.16), fur, (math.radians(-22), 0, 0)),
        # masa de hombros: lo mas ancho del bicho
        box((1.14, 0.44, 0.44), (0.0, -0.30, 1.36), fur),
        # cresta de pelo por el lomo, en rojizo
        box((0.26, 0.86, 0.16), (0.0, 0.06, 1.50), ridge, (math.radians(-16), 0, 0)),

        blob(0.19, (0.0, -0.56, 1.36), fur, (0.95, 1.1, 0.9)),
        cone(0.115, 0.36, (0.0, -0.80, 1.28), fur_dark, (math.radians(-90), 0, 0)),
        blob(0.035, (-0.09, -0.70, 1.40), eye),
        blob(0.035, (0.09, -0.70, 1.40), eye),
        cone(0.055, 0.19, (-0.13, -0.48, 1.56), fur_dark),
        cone(0.055, 0.19, (0.13, -0.48, 1.56), fur_dark),

        # brazos largos que casi tocan el suelo
        box((0.23, 0.23, 0.80), (-0.40, -0.26, 0.70), fur),
        box((0.23, 0.23, 0.80), (0.40, -0.26, 0.70), fur),
        cone(0.10, 0.26, (-0.40, -0.34, 0.24), claw, (math.radians(180), 0, 0)),
        cone(0.10, 0.26, (0.40, -0.34, 0.24), claw, (math.radians(180), 0, 0)),

        # patas traseras dobladas, tipico digitigrado
        box((0.26, 0.30, 0.50), (-0.20, 0.18, 0.60), fur_dark, (math.radians(28), 0, 0)),
        box((0.26, 0.30, 0.50), (0.20, 0.18, 0.60), fur_dark, (math.radians(28), 0, 0)),
        box((0.24, 0.38, 0.16), (-0.20, 0.06, 0.08), fur_dark),
        box((0.24, 0.38, 0.16), (0.20, 0.06, 0.08), fur_dark),

        # cola larga, que alarga la silueta hacia atras
        box((0.16, 0.70, 0.16), (0.0, 0.56, 1.14), ridge, (math.radians(34), 0, 0)),
        cone(0.09, 0.30, (0.0, 0.86, 0.86), ridge, (math.radians(140), 0, 0)),
    ]
    return finish(parts, "Werewolf")


# ---------------------------------------------------------------- vampiro
# Paleta de la hoja "COMMON VAMPIRE - FORMER LABORER".

def build_vampire():
    """
    Jornalero encorvado, no el noble que describe DESIGN.md. Mide 1,80 m de pie pero
    la joroba lo deja en 1,50 m de altura útil, y desde arriba lo que se ve es la
    espalda encorvada y la camisa clara: su propio panel de legibilidad cenital lo
    enseña así. Los brazos cuelgan hacia delante.
    """
    linen = material("Linen", "#D0CDB6")
    linen_light = material("LinenLight", "#E5DDD0")
    trousers = material("Trousers", "#3E3530")
    strap = material("Strap", "#625043")
    flesh = material("Flesh", "#A6A997")
    hair = material("Hair", "#242123")
    nail = material("Nail", "#4D5356")

    parts = [
        box((0.16, 0.16, 0.62), (-0.10, 0.0, 0.34), trousers),
        box((0.16, 0.16, 0.62), (0.10, 0.0, 0.34), trousers),
        box((0.19, 0.26, 0.14), (-0.10, -0.03, 0.07), strap),
        box((0.19, 0.26, 0.14), (0.10, -0.03, 0.07), strap),

        # torso volcado hacia delante: la joroba es la silueta
        box((0.44, 0.34, 0.62), (0.0, -0.06, 1.00), linen, (math.radians(-34), 0, 0)),
        box((0.50, 0.30, 0.22), (0.0, 0.06, 1.24), linen_light, (math.radians(-34), 0, 0)),
        box((0.09, 0.10, 0.44), (-0.13, -0.10, 1.06), strap, (math.radians(-34), 0, 0)),
        box((0.09, 0.10, 0.44), (0.13, -0.10, 1.06), strap, (math.radians(-34), 0, 0)),

        # cabeza baja y adelantada
        blob(0.135, (0.0, -0.30, 1.22), flesh, (0.95, 1.05, 1.0)),
        box((0.26, 0.22, 0.12), (0.0, -0.26, 1.33), hair),

        # brazos colgando hacia delante, casi al suelo
        box((0.14, 0.16, 0.66), (-0.28, -0.20, 0.80), flesh, (math.radians(16), 0, 0)),
        box((0.14, 0.16, 0.66), (0.28, -0.20, 0.80), flesh, (math.radians(16), 0, 0)),
        cone(0.06, 0.18, (-0.28, -0.30, 0.44), nail, (math.radians(170), 0, 0)),
        cone(0.06, 0.18, (0.28, -0.30, 0.44), nail, (math.radians(170), 0, 0)),
    ]
    return finish(parts, "Vampire")


# ---------------------------------------------------------------- demonio
# Sin hoja de referencia todavia: se mantiene lo descrito en ASSETS.md
# (piel agrietada con brasas entre las grietas, cuernos, hombreras).

def build_demon():
    """
    2,40 m y el doble de ancho que nadie. Ocupa tanto que se ve venir de lejos, que es
    justo lo que lo hace evitable con paciencia.
    """
    char = material("Char", "#2E211C")
    char_light = material("CharLight", "#463129")
    ember = material("Ember", "#D8541C", emission=3.5)
    horn = material("Horn", "#C7B79B")

    parts = [
        box((0.86, 0.58, 1.14), (0.0, 0.0, 1.34), char),
        # grietas encendidas: el rasgo que lo identifica de un vistazo
        box((0.20, 0.62, 0.90), (0.0, -0.02, 1.34), ember),
        box((1.54, 0.66, 0.44), (0.0, 0.0, 1.88), char_light),
        blob(0.22, (0.0, -0.12, 2.14), char, (1.0, 1.0, 0.9)),
        blob(0.05, (-0.09, -0.30, 2.16), ember),
        blob(0.05, (0.09, -0.30, 2.16), ember),
        cone(0.10, 0.46, (-0.22, -0.04, 2.36), horn, (math.radians(-26), 0, math.radians(-18))),
        cone(0.10, 0.46, (0.22, -0.04, 2.36), horn, (math.radians(-26), 0, math.radians(18))),
        box((0.32, 0.32, 0.98), (-0.64, 0.0, 1.28), char),
        box((0.32, 0.32, 0.98), (0.64, 0.0, 1.28), char),
        blob(0.26, (-0.66, -0.08, 0.78), char_light, (1.0, 1.1, 0.9)),
        blob(0.26, (0.66, -0.08, 0.78), char_light, (1.0, 1.1, 0.9)),
        box((0.34, 0.40, 0.84), (-0.26, 0.0, 0.42), char),
        box((0.34, 0.40, 0.84), (0.26, 0.0, 0.42), char),
    ]
    return finish(parts, "Demon")


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
