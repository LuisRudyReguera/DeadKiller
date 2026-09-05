using Godot;
using DeadKillers.Components;

namespace DeadKillers.Weapons;

/// <summary>
/// Arma tirada en el suelo. Al pisarla, se la queda quien traiga un
/// <see cref="WeaponHolder"/>. Solo desaparece si de verdad se la ha llevado: si ya la
/// tenía, sigue ahí.
/// </summary>
[GlobalClass]
public partial class WeaponPickup : Area3D
{
    [Signal]
    public delegate void PickedUpEventHandler(Node3D taker);

    [Export] public WeaponData Weapon { get; set; }

    // Nodo que gira para que se distinga del escenario. Opcional.
    [Export] public Node3D Spinner { get; set; }

    [Export] public float SpinSpeed { get; set; } = 1.5f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        if (Weapon == null)
        {
            GD.PushWarning($"{GetPath()}: recogida sin arma asignada; no hará nada.");
        }
    }

    public override void _Process(double delta)
    {
        Spinner?.RotateY(SpinSpeed * (float)delta);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (Weapon == null)
        {
            return;
        }

        WeaponHolder holder = FindHolder(body);
        if (holder == null || !holder.AddWeapon(Weapon))
        {
            return;
        }

        EmitSignal(SignalName.PickedUp, body);

        // QueueFree es seguro dentro de una señal de colisión: Godot lo aplaza al final
        // del fotograma. Borrarlo en el acto sí daría error.
        QueueFree();
    }

    private static WeaponHolder FindHolder(Node body)
    {
        foreach (Node child in body.GetChildren())
        {
            if (child is WeaponHolder holder)
            {
                return holder;
            }
        }

        return null;
    }
}
