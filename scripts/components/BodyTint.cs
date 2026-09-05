using Godot;

namespace DeadKillers.Components;

/// <summary>
/// Pinta el cuerpo de una entidad con un material, sea una cápsula suelta o un modelo
/// importado con varias mallas dentro.
///
/// Existe porque `material_override` es una propiedad de `MeshInstance3D`, y la raíz de
/// un `.glb` es un `Node3D`: no se le puede asignar desde la escena. Los enemigos lo
/// hacen por su cuenta desde su `EnemyData`; esto es para el resto.
/// </summary>
[GlobalClass]
public partial class BodyTint : Node
{
    [Export] public Node3D Body { get; set; }

    [Export] public Material Material { get; set; }

    public override void _Ready()
    {
        if (Body == null || Material == null)
        {
            GD.PushWarning($"{GetPath()}: faltan referencias; el cuerpo se queda con su material original.");
            return;
        }

        Meshes.SetOverride(Meshes.CollectFrom(Body), Material);
    }

    public override void _ExitTree()
    {
        Material = null;   // ver D-011
    }
}
