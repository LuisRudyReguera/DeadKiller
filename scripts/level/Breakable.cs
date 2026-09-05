using Godot;
using Godot.Collections;
using DeadKillers.Components;

namespace DeadKillers.Level;

/// <summary>
/// Barril, ataúd o altar: algo con vida que se rompe a golpes y puede soltar botín o
/// disparar un suceso. Es la pieza de los objetivos de destrucción.
///
/// Reutiliza el mismo HealthComponent que el jugador y los monstruos: un barril recibe
/// daño exactamente igual que un licántropo.
/// </summary>
[GlobalClass]
public partial class Breakable : StaticBody3D
{
    public const string BreakableGroup = "breakable";

    [Signal]
    public delegate void BrokenEventHandler();

    [Export] public HealthComponent Health { get; set; }

    [Export] public Node3D Body { get; set; }

    [Export] public CollisionShape3D Blocker { get; set; }

    // Qué deja al romperse. Puede ser oro, munición o nada.
    [Export] public PackedScene Drop { get; set; }

    [Export] public int DropAmount { get; set; } = 10;

    // Qué se activa al romperse. Un altar puede abrir una reja.
    [Export] public Array<LevelResponder> Targets { get; set; } = new();

    [Export] public AudioStreamPlayer3D Sound { get; set; }

    public bool IsBroken { get; private set; }

    public override void _EnterTree()
    {
        AddToGroup(BreakableGroup);
    }

    public override void _Ready()
    {
        if (Health == null)
        {
            GD.PushWarning($"{Name}: sin HealthComponent; no se podrá romper.");
            return;
        }

        Health.Died += OnBroken;
    }

    public override void _ExitTree()
    {
        Drop = null;      // ver D-011
        Targets = null;
    }

    private void OnBroken()
    {
        if (IsBroken)
        {
            return;
        }

        IsBroken = true;
        RemoveFromGroup(BreakableGroup);

        Sound?.Play();

        if (Body != null)
        {
            Body.Visible = false;
        }

        // Diferido: la muerte llega desde un golpe en pleno ciclo de física.
        Blocker?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        SetDeferred(PropertyName.CollisionLayer, 0u);
        CallDeferred(MethodName.SpawnDrop);

        foreach (LevelResponder target in Targets)
        {
            if (IsInstanceValid(target))
            {
                target.Activate();
            }
        }

        EmitSignal(SignalName.Broken);
    }

    private void SpawnDrop()
    {
        if (Drop == null || !IsInsideTree() || Drop.Instantiate() is not Node3D loot)
        {
            return;
        }

        if (loot is Missions.ItemPickup pickup)
        {
            pickup.Amount = DropAmount;
        }

        GetParent().AddChild(loot);
        loot.GlobalPosition = GlobalPosition;
    }
}
