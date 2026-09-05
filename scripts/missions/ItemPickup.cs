using Godot;
using DeadKillers.Components;
using DeadKillers.Weapons;

namespace DeadKillers.Missions;

/// <summary>
/// Oro, llaves y munición tirados por el mapa. No sabe qué es un objetivo: se lo cuenta a
/// la misión y que ella decida si eso cumple algo.
/// </summary>
[GlobalClass]
public partial class ItemPickup : Area3D
{
    [Export] public ItemKind Kind { get; set; } = ItemKind.Gold;

    [Export] public int Amount { get; set; } = 10;

    [Export] public Node3D Spinner { get; set; }

    [Export] public float SpinSpeed { get; set; } = 2.0f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        Spinner?.RotateY(SpinSpeed * (float)delta);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup(Groups.Player))
        {
            return;
        }

        // La munición entra en la bolsa del jugador; el oro y las llaves no tienen dónde
        // guardarse aparte. En los dos casos la misión se entera, porque puede haber un
        // objetivo que cuente cualquiera de las dos cosas.
        TryFillPouch(body);

        if (GetTree().GetFirstNodeInGroup(Mission.MissionGroup) is Mission mission)
        {
            mission.Collect(Kind, Amount);
        }

        // QueueFree es seguro dentro de una señal de colisión: Godot lo aplaza.
        QueueFree();
    }

    private bool TryFillPouch(Node body)
    {
        AmmoKind ammo = Kind switch
        {
            ItemKind.Arrows => AmmoKind.Arrows,
            ItemKind.Bolts => AmmoKind.Bolts,
            ItemKind.Powder => AmmoKind.Powder,
            _ => AmmoKind.None,
        };

        if (ammo == AmmoKind.None)
        {
            return false;
        }

        foreach (Node child in body.GetChildren())
        {
            if (child is AmmoPouch pouch)
            {
                pouch.Add(ammo, Amount);
                return true;
            }
        }

        return false;
    }
}
