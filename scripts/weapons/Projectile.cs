using Godot;
using System.Collections.Generic;
using DeadKillers.Components;

namespace DeadKillers.Weapons;

/// <summary>
/// Flecha, virote o bala. Avanza en línea recta hacia su frente y reparte daño al
/// primer <see cref="HealthComponent"/> que encuentra. Sus valores no están aquí:
/// se los pone el <see cref="WeaponHolder"/> desde el `.tres` del arma.
/// </summary>
[GlobalClass]
public partial class Projectile : Area3D
{
    [Export] public float Speed { get; set; } = 25.0f;

    [Export] public int Damage { get; set; } = 30;

    // A cuántos enemigos alcanza antes de desaparecer. La ballesta atraviesa uno.
    [Export] public int MaxTargets { get; set; } = 1;

    [Export] public bool IgnoresArmor { get; set; }

    // Se autodestruye al agotarlo, por si sale por un hueco y no choca con nada.
    [Export] public float MaxDistance { get; set; } = 40.0f;

    private float _travelled;
    private int _targetsHit;

    // Un proyectil grande puede seguir solapando al mismo cuerpo varios fotogramas.
    private readonly HashSet<ulong> _alreadyHit = new();

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        float step = Speed * (float)delta;

        // -Z es el frente en Godot: el proyectil avanza hacia donde mira.
        GlobalPosition += -GlobalBasis.Z * step;

        _travelled += step;
        if (_travelled >= MaxDistance)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        HealthComponent health = HealthComponent.FindIn(body);

        if (health == null)
        {
            // Pared, suelo o cualquier cosa sin vida: el proyectil muere ahí.
            QueueFree();
            return;
        }

        if (!_alreadyHit.Add(body.GetInstanceId()) || health.IsDead)
        {
            return;
        }

        health.TakeDamage(Damage, IgnoresArmor);
        _targetsHit++;

        if (_targetsHit >= MaxTargets)
        {
            QueueFree();
        }
    }
}
