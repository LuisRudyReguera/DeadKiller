using Godot;
using System.Collections.Generic;

namespace DeadKillers.Components;

/// <summary>
/// Utilidades para tratar el cuerpo visible de una entidad sin saber cómo está hecho.
///
/// Un placeholder es una cápsula suelta; un modelo importado de un `.glb` es una escena
/// con varias mallas dentro. Todo lo que colorea o destella pasa por aquí para que
/// sustituir un modelo por otro no obligue a tocar código.
/// </summary>
public static class Meshes
{
    /// <summary>
    /// Todas las mallas que cuelgan de un nodo, incluido él mismo si lo es.
    /// </summary>
    public static List<MeshInstance3D> CollectFrom(Node root)
    {
        var found = new List<MeshInstance3D>();
        Collect(root, found);
        return found;
    }

    /// <summary>Pinta todas las mallas con el mismo material, o lo quita si es null.</summary>
    public static void SetOverride(IEnumerable<MeshInstance3D> meshes, Material material)
    {
        foreach (MeshInstance3D mesh in meshes)
        {
            if (GodotObject.IsInstanceValid(mesh))
            {
                mesh.MaterialOverride = material;
            }
        }
    }

    /// <summary>
    /// Superpone un material sin tocar el de base. Es lo que usa el destello al recibir
    /// daño, para no pelearse con quien esté coloreando el cuerpo.
    /// </summary>
    public static void SetOverlay(IEnumerable<MeshInstance3D> meshes, Material material)
    {
        foreach (MeshInstance3D mesh in meshes)
        {
            if (GodotObject.IsInstanceValid(mesh))
            {
                mesh.MaterialOverlay = material;
            }
        }
    }

    private static void Collect(Node node, List<MeshInstance3D> found)
    {
        if (node == null)
        {
            return;
        }

        if (node is MeshInstance3D mesh)
        {
            found.Add(mesh);
        }

        foreach (Node child in node.GetChildren())
        {
            Collect(child, found);
        }
    }
}
