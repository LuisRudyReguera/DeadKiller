# -*- coding: utf-8 -*-
"""
Genera los personajes, los monstruos y las armas, y los exporta a `models/` en .glb.

    "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background ^
        --python tools/blender/make_placeholders.py

Siguen siendo modelos construidos con primitivas, no esculpidos: las proporciones, la
postura y la paleta salen de las hojas de `docs/references/`, pero el acabado tiene el
techo que tiene. El biselado y el sombreado por ángulo los levantan bastante.

CONVENCIONES (docs/ASSETS.md):
  - 1 unidad = 1 metro; un humano mide ~1,8
  - El personaje mira hacia -Z EN GODOT

  OJO CON LOS EJES: el exportador de glTF convierte el -Y de Blender en +Z de Godot,
  que es la ESPALDA. Por eso `finish()` gira el modelo 180° antes de exportar. Aquí se
  construye mirando a -Y, que es lo cómodo de leer, y la rotación lo deja bien.
"""

import math
import os
import sys

import bpy

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "models")


# ---------------------------------------------------------------- color

def srgb_to_linear(channel):
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def material(name, hex_color, roughness=0.85, metallic=0.0, emission=0.0):
    if name in bpy.data.materials:
        return bpy.data.materials[name]

    hex_color = hex_color.lstrip("#")
    rgb = tuple(srgb_to_linear(int(hex_color[i:i + 2], 16) / 255.0) for i in (0, 2, 4))

    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic

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


def blob(radius, loc, mat, scale=(1.0, 1.0, 1.0), rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=16, ring_count=10,
                                         location=loc, rotation=rot)
    obj = bpy.context.active_object
    obj.scale = scale
    return paint(obj, mat)


def taper(bottom, top, depth, loc, mat, rot=(0.0, 0.0, 0.0), sides=12):
    """Cilindro de radio distinto arriba y abajo. La forma más útil de todas."""
    bpy.ops.mesh.primitive_cone_add(radius1=bottom, radius2=top, depth=depth,
                                    vertices=sides, location=loc, rotation=rot)
    return paint(bpy.context.active_object, mat)


def cone(radius, depth, loc, mat, rot=(0.0, 0.0, 0.0), sides=10):
    bpy.ops.mesh.primitive_cone_add(radius1=radius, radius2=0.0, depth=depth,
                                    vertices=sides, location=loc, rotation=rot)
    return paint(bpy.context.active_object, mat)


def finish(parts, name, bevel=0.012, smooth_angle=42.0):
    bpy.ops.object.select_all(action="DESELECT")

    for part in parts:
        part.select_set(True)

    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()

    obj = bpy.context.active_object
    obj.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # 180°: construimos mirando a -Y porque es cómodo, pero glTF manda el -Y de Blender
    # al +Z de Godot, que es la espalda. Sin esto, todo el mundo da la espalda al jugador.
    obj.rotation_euler = (0.0, 0.0, math.radians(180.0))
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # Biselar: es lo que hace que una caja deje de parecer una caja. Los cantos recogen
    # la luz y el volumen se lee muchísimo mejor desde la cámara cenital.
    if bevel > 0.0:
        modifier = obj.modifiers.new("Bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        modifier.limit_method = "ANGLE"
        modifier.angle_limit = math.radians(50.0)
        modifier.harden_normals = False
        try:
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        except RuntimeError:
            obj.modifiers.remove(modifier)

    # Suave donde la curvatura es real, plano donde hay canto vivo.
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(smooth_angle))
    except AttributeError:
        bpy.ops.object.shade_flat()

    lowest = min((obj.matrix_world @ v.co).z for v in obj.data.vertices)
    bpy.context.scene.cursor.location = (0.0, 0.0, lowest)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    obj.location = (0.0, 0.0, 0.0)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

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
    print("[BLENDER] %-16s %5d tris   alto %.2f m" % (filename, tris, height))


# ================================================================== CAZADOR

def build_hunter():
    """
    1,80 m. Desde arriba manda el ala del sombrero; de frente, la silueta del gabán con
    la esclavina sobre los hombros. Paleta de la hoja `cazador_gotico.png`.
    """
    # Subidos dentro de la paleta de la hoja: el cuero original quedaba más oscuro
    # que el suelo de piedra y el personaje se leía como una mancha negra.
    leather = material("Leather", "#7A5940", roughness=0.75)
    leather_dark = material("LeatherDark", "#50392B", roughness=0.7)
    hat = material("Hat", "#3E2E24", roughness=0.8)
    skin = material("Skin", "#DCAA78", roughness=0.6)
    beard = material("Beard", "#3A2A20", roughness=0.9)
    denim = material("Denim", "#365B73", roughness=0.9)
    iron = material("Iron", "#737E86", roughness=0.35, metallic=0.9)
    brass = material("Brass", "#B78565", roughness=0.3, metallic=0.85)
    scarf = material("Scarf", "#365B73", roughness=0.9)
    glass = material("LanternGlass", "#F2CB83", emission=6.0)

    parts = []

    # --- piernas y botas ---
    for side in (-1, 1):
        parts += [
            taper(0.105, 0.085, 0.52, (0.11 * side, 0.0, 0.52), denim),
            taper(0.095, 0.11, 0.30, (0.11 * side, -0.01, 0.20), leather_dark),
            box((0.20, 0.30, 0.10), (0.11 * side, -0.04, 0.05), leather_dark),
            box((0.20, 0.10, 0.09), (0.11 * side, -0.16, 0.05), iron),
        ]

    # --- faldón del gabán: dos paneles que se abren, no un bloque ---
    parts += [
        taper(0.34, 0.22, 0.62, (0.0, 0.02, 0.86), leather),
        box((0.30, 0.16, 0.54), (-0.17, 0.04, 0.80), leather, (0.0, math.radians(-7), 0.0)),
        box((0.30, 0.16, 0.54), (0.17, 0.04, 0.80), leather, (0.0, math.radians(7), 0.0)),
        box((0.40, 0.26, 0.09), (0.0, 0.0, 1.14), leather_dark),
        box((0.09, 0.06, 0.07), (0.0, -0.15, 1.14), brass),
    ]

    # --- torso y esclavina ---
    parts += [
        taper(0.21, 0.25, 0.42, (0.0, 0.0, 1.36), leather),
        taper(0.34, 0.20, 0.20, (0.0, 0.0, 1.50), leather_dark),
        # correas cruzadas al pecho
        box((0.07, 0.05, 0.44), (0.0, -0.19, 1.36), leather_dark, (0.0, math.radians(24), 0.0)),
        box((0.07, 0.05, 0.44), (0.0, -0.19, 1.36), leather_dark, (0.0, math.radians(-24), 0.0)),
    ]

    # --- brazos ---
    for side in (-1, 1):
        parts += [
            blob(0.10, (0.27 * side, 0.0, 1.52), leather, (1.0, 1.0, 0.9)),
            taper(0.085, 0.07, 0.34, (0.29 * side, 0.0, 1.30), leather),
            taper(0.07, 0.065, 0.30, (0.30 * side, -0.03, 1.02), leather_dark),
            blob(0.075, (0.30 * side, -0.05, 0.86), leather_dark, (1.0, 1.15, 0.9)),
        ]

    # --- cabeza, barba y sombrero ---
    parts += [
        taper(0.075, 0.09, 0.10, (0.0, 0.0, 1.62), skin),
        box((0.24, 0.20, 0.08), (0.0, -0.01, 1.60), scarf),
        blob(0.105, (0.0, -0.01, 1.72), skin, (1.0, 1.05, 1.1)),
        blob(0.085, (0.0, -0.06, 1.69), beard, (1.0, 0.9, 0.75)),
        # ala ancha: la pieza que identifica al personaje desde la cámara cenital
        taper(0.31, 0.27, 0.035, (0.0, -0.01, 1.815), hat, sides=20),
        taper(0.135, 0.115, 0.16, (0.0, -0.01, 1.90), hat, sides=16),
        box((0.24, 0.20, 0.035), (0.0, -0.01, 1.835), leather_dark),
    ]

    # --- ballesta cruzada a la espalda ---
    parts += [
        box((0.075, 0.075, 0.56), (0.02, 0.20, 1.34), leather_dark, (0.0, math.radians(32), 0.0)),
        box((0.46, 0.06, 0.05), (0.02, 0.22, 1.44), iron, (0.0, math.radians(32), 0.0)),
        box((0.05, 0.05, 0.20), (0.02, 0.20, 1.14), iron, (0.0, math.radians(32), 0.0)),
    ]

    # --- farol al costado derecho: segundo identificador cenital, y da luz ---
    parts += [
        box((0.03, 0.03, 0.14), (0.31, -0.09, 1.06), iron),
        taper(0.075, 0.055, 0.05, (0.31, -0.09, 0.98), brass),
        box((0.10, 0.10, 0.13), (0.31, -0.09, 0.90), glass),
        taper(0.055, 0.075, 0.05, (0.31, -0.09, 0.82), brass),
    ]

    # --- vaina de la espada al costado izquierdo ---
    parts += [
        taper(0.035, 0.025, 0.44, (-0.28, 0.06, 0.86), leather_dark, (math.radians(14), 0.0, 0.0)),
        box((0.05, 0.05, 0.07), (-0.27, 0.01, 1.08), brass),
    ]

    return finish(parts, "Hunter")


# ================================================================== LICANTROPO

def build_werewolf():
    """
    1,75 m encorvado. Piernas digitígradas de verdad —muslo, caña y almohadilla—, cruz
    más alta que la cabeza y cola larga. Hoja `licantropo.jpg`.
    """
    fur = material("Fur", "#8A7A6E", roughness=0.95)
    fur_dark = material("FurDark", "#5C4E45", roughness=0.95)
    fur_belly = material("FurBelly", "#A89A8C", roughness=0.95)
    ridge = material("Ridge", "#A2503C", roughness=0.9)
    claw = material("Claw", "#D8CBB4", roughness=0.4)
    eye = material("Eye", "#E03A25", emission=7.0)
    fang = material("Fang", "#EDE6D6", roughness=0.35)

    parts = []

    # --- caja torácica y vientre, inclinados hacia delante ---
    parts += [
        blob(0.40, (0.0, -0.16, 1.14), fur, (1.25, 1.5, 1.0), (math.radians(-20), 0, 0)),
        blob(0.30, (0.0, 0.10, 0.98), fur_belly, (1.05, 1.25, 0.85), (math.radians(-14), 0, 0)),
        # cruz: el punto más alto del animal
        blob(0.30, (0.0, -0.26, 1.42), fur, (1.55, 0.95, 0.65)),
        box((0.22, 0.92, 0.13), (0.0, 0.02, 1.48), ridge, (math.radians(-18), 0, 0)),
    ]

    # --- cuello y cabeza ---
    parts += [
        taper(0.20, 0.16, 0.26, (0.0, -0.44, 1.34), fur, (math.radians(64), 0, 0)),
        blob(0.185, (0.0, -0.60, 1.28), fur, (1.0, 1.25, 0.95)),
        taper(0.13, 0.075, 0.32, (0.0, -0.84, 1.20), fur_dark, (math.radians(-96), 0, 0)),
        blob(0.05, (0.0, -0.99, 1.19), fur_dark),
        box((0.13, 0.16, 0.05), (0.0, -0.88, 1.10), fang),
        blob(0.032, (-0.085, -0.75, 1.34), eye),
        blob(0.032, (0.085, -0.75, 1.34), eye),
        cone(0.06, 0.20, (-0.14, -0.52, 1.46), fur_dark, (math.radians(-12), 0, math.radians(-16))),
        cone(0.06, 0.20, (0.14, -0.52, 1.46), fur_dark, (math.radians(-12), 0, math.radians(16))),
    ]

    # --- brazos, largos y con garras ---
    for side in (-1, 1):
        parts += [
            blob(0.16, (0.40 * side, -0.28, 1.34), fur, (1.0, 1.0, 0.95)),
            taper(0.14, 0.10, 0.50, (0.44 * side, -0.30, 1.02), fur, (math.radians(6), 0, math.radians(-6 * side))),
            taper(0.10, 0.085, 0.42, (0.46 * side, -0.34, 0.60), fur_dark),
            blob(0.10, (0.46 * side, -0.36, 0.36), fur_dark, (1.0, 1.2, 0.8)),
        ]
        for finger in (-1, 0, 1):
            parts.append(cone(0.028, 0.20,
                              (0.46 * side + 0.07 * finger, -0.46, 0.26), claw,
                              (math.radians(200), 0, 0)))

    # --- patas traseras digitígradas ---
    for side in (-1, 1):
        parts += [
            taper(0.19, 0.15, 0.42, (0.22 * side, 0.16, 0.86), fur, (math.radians(34), 0, 0)),
            taper(0.13, 0.09, 0.44, (0.22 * side, -0.02, 0.50), fur_dark, (math.radians(-30), 0, 0)),
            taper(0.085, 0.09, 0.26, (0.22 * side, 0.06, 0.20), fur_dark, (math.radians(50), 0, 0)),
            box((0.20, 0.30, 0.09), (0.22 * side, -0.06, 0.05), fur_dark),
        ]
        for toe in (-1, 0, 1):
            parts.append(cone(0.025, 0.13,
                              (0.22 * side + 0.06 * toe, -0.21, 0.04), claw,
                              (math.radians(-90), 0, 0)))

    # --- cola ---
    parts += [
        taper(0.13, 0.09, 0.44, (0.0, 0.44, 1.16), ridge, (math.radians(48), 0, 0)),
        taper(0.09, 0.05, 0.40, (0.0, 0.72, 0.90), ridge, (math.radians(62), 0, 0)),
        cone(0.05, 0.20, (0.0, 0.90, 0.72), ridge, (math.radians(118), 0, 0)),
    ]

    return finish(parts, "Werewolf")


# ================================================================== VAMPIRO

def build_vampire():
    """
    Jornalero encorvado de la hoja `vampiro_labrador.png`. 1,80 m de pie, pero la joroba
    lo deja en ~1,50 m útiles. Los brazos cuelgan hacia delante, casi al suelo.
    """
    linen = material("Linen", "#D0CDB6", roughness=0.95)
    linen_light = material("LinenLight", "#E5DDD0", roughness=0.95)
    trousers = material("Trousers", "#3E3530", roughness=0.95)
    patch = material("Patch", "#625043", roughness=0.95)
    strap = material("Strap", "#88705A", roughness=0.9)
    flesh = material("Flesh", "#A6A997", roughness=0.75)
    hair = material("Hair", "#242123", roughness=0.95)
    nail = material("Nail", "#4D5356", roughness=0.5)
    eye = material("VampEye", "#F5F1E5", emission=2.5)

    parts = []

    for side in (-1, 1):
        parts += [
            taper(0.10, 0.085, 0.50, (0.10 * side, 0.0, 0.40), trousers),
            box((0.14, 0.14, 0.13), (0.10 * side, 0.02, 0.44), patch),
            box((0.19, 0.27, 0.11), (0.10 * side, -0.03, 0.06), patch),
        ]

    # --- torso volcado: la joroba ES la silueta ---
    parts += [
        taper(0.23, 0.20, 0.54, (0.0, -0.06, 1.00), linen, (math.radians(-38), 0, 0)),
        blob(0.24, (0.0, 0.06, 1.22), linen_light, (1.15, 0.95, 0.7)),
        box((0.34, 0.20, 0.10), (0.0, -0.02, 0.74), trousers),
    ]

    for side in (-1, 1):
        parts += [
            box((0.055, 0.07, 0.42), (0.13 * side, -0.12, 1.02), strap, (math.radians(-38), 0, 0)),
            # brazos colgando hacia delante
            blob(0.10, (0.26 * side, -0.14, 1.14), linen, (1.0, 1.0, 0.9)),
            taper(0.085, 0.065, 0.40, (0.28 * side, -0.20, 0.92), linen_light, (math.radians(18), 0, 0)),
            taper(0.062, 0.055, 0.38, (0.29 * side, -0.31, 0.56), flesh, (math.radians(14), 0, 0)),
            blob(0.065, (0.29 * side, -0.36, 0.36), flesh, (1.0, 1.2, 0.8)),
        ]
        for finger in (-1, 0, 1):
            parts.append(cone(0.018, 0.15,
                              (0.29 * side + 0.045 * finger, -0.42, 0.28), nail,
                              (math.radians(196), 0, 0)))

    # --- cabeza baja y ladeada ---
    parts += [
        taper(0.075, 0.085, 0.16, (0.0, -0.20, 1.24), flesh, (math.radians(-52), 0, 0)),
        blob(0.125, (0.0, -0.32, 1.18), flesh, (1.0, 1.1, 1.05), (0.0, 0.0, math.radians(9))),
        blob(0.115, (0.0, -0.28, 1.24), hair, (1.05, 1.0, 0.85)),
        blob(0.026, (-0.055, -0.42, 1.19), eye),
        blob(0.026, (0.055, -0.42, 1.19), eye),
    ]

    return finish(parts, "Vampire")


# ================================================================== DEMONIO

def build_demon():
    """
    2,40 m y el doble de ancho que nadie. Sin hoja de referencia: se sigue lo descrito en
    ASSETS.md, piel agrietada con brasas, cuernos y hombreras.
    """
    char = material("Char", "#2E211C", roughness=0.9)
    char_light = material("CharLight", "#4A342B", roughness=0.85)
    ember = material("Ember", "#E85A18", emission=5.0)
    horn = material("Horn", "#C7B79B", roughness=0.45)
    hoof = material("Hoof", "#1C1512", roughness=0.6)

    parts = []

    parts += [
        blob(0.46, (0.0, 0.0, 1.42), char, (1.05, 0.78, 1.15)),
        taper(0.40, 0.30, 0.44, (0.0, 0.0, 1.00), char),
        # grietas encendidas por el pecho y el vientre
        box((0.11, 0.50, 0.62), (0.0, -0.06, 1.38), ember),
        box((0.42, 0.44, 0.09), (0.0, -0.10, 1.14), ember, (0.0, math.radians(12), 0.0)),
        # hombreras: lo que lo hace enorme desde arriba
        blob(0.34, (-0.62, 0.0, 1.78), char_light, (1.0, 0.9, 0.7)),
        blob(0.34, (0.62, 0.0, 1.78), char_light, (1.0, 0.9, 0.7)),
        box((1.44, 0.52, 0.20), (0.0, 0.0, 1.80), char_light),
    ]

    # --- cabeza hundida entre los hombros ---
    parts += [
        blob(0.21, (0.0, -0.12, 2.06), char, (1.0, 1.1, 0.95)),
        blob(0.046, (-0.09, -0.29, 2.08), ember),
        blob(0.046, (0.09, -0.29, 2.08), ember),
        box((0.18, 0.14, 0.05), (0.0, -0.26, 1.96), ember),
        taper(0.10, 0.045, 0.34, (-0.20, -0.02, 2.28), horn, (math.radians(-30), 0, math.radians(-20))),
        taper(0.10, 0.045, 0.34, (0.20, -0.02, 2.28), horn, (math.radians(-30), 0, math.radians(20))),
        cone(0.05, 0.22, (-0.30, -0.10, 2.50), horn, (math.radians(-40), 0, math.radians(-26))),
        cone(0.05, 0.22, (0.30, -0.10, 2.50), horn, (math.radians(-40), 0, math.radians(26))),
    ]

    # --- brazos macizos ---
    for side in (-1, 1):
        parts += [
            taper(0.20, 0.17, 0.54, (0.66 * side, 0.0, 1.44), char),
            taper(0.17, 0.15, 0.50, (0.68 * side, -0.04, 0.96), char_light),
            blob(0.24, (0.70 * side, -0.06, 0.66), char, (1.0, 1.15, 0.9)),
            box((0.05, 0.30, 0.20), (0.70 * side, -0.06, 0.72), ember),
        ]

    # --- piernas y pezuñas ---
    for side in (-1, 1):
        parts += [
            taper(0.24, 0.19, 0.52, (0.28 * side, 0.02, 0.70), char),
            taper(0.19, 0.17, 0.40, (0.28 * side, -0.02, 0.28), char_light),
            box((0.30, 0.40, 0.14), (0.28 * side, -0.06, 0.07), hoof),
        ]

    return finish(parts, "Demon")


# ================================================================== ARMAS
# Se sostienen en la mano derecha del cazador. Origen en el puño.

def build_short_sword():
    steel = material("Steel", "#9AA3AA", roughness=0.25, metallic=0.95)
    grip = material("Grip", "#3A2A20", roughness=0.9)
    brass = material("BrassW", "#B78565", roughness=0.3, metallic=0.85)

    parts = [
        taper(0.022, 0.020, 0.14, (0.0, 0.0, 0.0), grip),
        box((0.16, 0.045, 0.030), (0.0, 0.0, 0.08), brass),
        box((0.052, 0.014, 0.52), (0.0, 0.0, 0.36), steel),
        cone(0.026, 0.10, (0.0, 0.0, 0.66), steel, sides=4),
        blob(0.030, (0.0, 0.0, -0.08), brass),
    ]
    return finish(parts, "ShortSword", bevel=0.004)


def build_short_bow():
    wood = material("BowWood", "#70503A", roughness=0.8)
    cord = material("Cord", "#D8CBB4", roughness=0.95)
    horn = material("BowHorn", "#3A2A20", roughness=0.7)

    parts = [
        box((0.05, 0.06, 0.22), (0.0, 0.0, 0.0), horn),
        taper(0.028, 0.018, 0.42, (0.0, -0.06, 0.28), wood, (math.radians(-16), 0, 0)),
        taper(0.028, 0.018, 0.42, (0.0, -0.06, -0.28), wood, (math.radians(16), 0, 0)),
        taper(0.018, 0.012, 0.20, (0.0, -0.16, 0.56), wood, (math.radians(-40), 0, 0)),
        taper(0.018, 0.012, 0.20, (0.0, -0.16, -0.56), wood, (math.radians(40), 0, 0)),
        box((0.006, 0.006, 1.24), (0.0, -0.22, 0.0), cord),
    ]
    return finish(parts, "ShortBow", bevel=0.003)


def build_crossbow():
    wood = material("BowWood", "#70503A", roughness=0.8)
    iron = material("IronW", "#4B5158", roughness=0.35, metallic=0.9)
    cord = material("Cord", "#D8CBB4", roughness=0.95)

    parts = [
        box((0.06, 0.62, 0.05), (0.0, -0.16, 0.0), wood),
        box((0.05, 0.14, 0.12), (0.0, 0.16, -0.04), wood, (math.radians(20), 0, 0)),
        box((0.62, 0.05, 0.035), (0.0, -0.34, 0.02), iron),
        taper(0.020, 0.012, 0.18, (-0.34, -0.34, 0.02), iron, (0.0, math.radians(78), 0.0)),
        taper(0.020, 0.012, 0.18, (0.34, -0.34, 0.02), iron, (0.0, math.radians(-78), 0.0)),
        box((0.60, 0.005, 0.005), (0.0, -0.16, 0.03), cord),
        box((0.04, 0.10, 0.04), (0.0, -0.04, 0.05), iron),
    ]
    return finish(parts, "Crossbow", bevel=0.004)


def build_pistol():
    wood = material("BowWood", "#70503A", roughness=0.8)
    iron = material("IronW", "#4B5158", roughness=0.35, metallic=0.9)
    brass = material("BrassW", "#B78565", roughness=0.3, metallic=0.85)

    parts = [
        box((0.045, 0.07, 0.17), (0.0, 0.05, -0.04), wood, (math.radians(-18), 0, 0)),
        box((0.05, 0.30, 0.06), (0.0, -0.14, 0.06), wood),
        taper(0.024, 0.021, 0.34, (0.0, -0.26, 0.08), iron, (math.radians(90), 0, 0)),
        box((0.05, 0.07, 0.07), (0.0, -0.04, 0.10), brass),
        box((0.02, 0.05, 0.06), (0.0, 0.02, 0.12), iron, (math.radians(-30), 0, 0)),
    ]
    return finish(parts, "Pistol", bevel=0.003)


BUILDERS = {
    "hunter.glb": build_hunter,
    "werewolf.glb": build_werewolf,
    "vampire.glb": build_vampire,
    "demon.glb": build_demon,
    "weapon_short_sword.glb": build_short_sword,
    "weapon_short_bow.glb": build_short_bow,
    "weapon_crossbow.glb": build_crossbow,
    "weapon_pistol.glb": build_pistol,
}


def main():
    for filename, builder in BUILDERS.items():
        clear_scene()
        export(builder(), filename)

    print("[BLENDER] hecho: %d modelos en %s" % (len(BUILDERS), OUT_DIR))


if __name__ == "__main__":
    main()
    sys.exit(0)
