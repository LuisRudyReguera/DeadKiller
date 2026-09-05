using Godot;
using System.Collections.Generic;

namespace DeadKillers.Weapons;

/// <summary>
/// Reservas de munición del jugador, separadas por tipo. No hay munición universal:
/// quedarse sin flechas con la ballesta llena es una situación normal, no un fallo
/// (docs/DESIGN.md).
/// </summary>
[GlobalClass]
public partial class AmmoPouch : Node
{
    [Signal]
    public delegate void AmmoChangedEventHandler(int kind, int current, int max);

    // Valores de docs/BALANCE.md.
    [Export] public int Arrows { get; set; } = 30;
    [Export] public int MaxArrows { get; set; } = 60;

    [Export] public int Bolts { get; set; } = 10;
    [Export] public int MaxBolts { get; set; } = 20;

    [Export] public int Powder { get; set; } = 4;
    [Export] public int MaxPowder { get; set; } = 8;

    private readonly Dictionary<AmmoKind, int> _current = new();
    private readonly Dictionary<AmmoKind, int> _maximum = new();

    public override void _Ready()
    {
        _current[AmmoKind.Arrows] = Arrows;
        _current[AmmoKind.Bolts] = Bolts;
        _current[AmmoKind.Powder] = Powder;

        _maximum[AmmoKind.Arrows] = MaxArrows;
        _maximum[AmmoKind.Bolts] = MaxBolts;
        _maximum[AmmoKind.Powder] = MaxPowder;
    }

    /// <summary>El cuerpo a cuerpo no gasta nada, así que `None` es siempre infinito.</summary>
    public int Available(AmmoKind kind)
    {
        if (kind == AmmoKind.None)
        {
            return int.MaxValue;
        }

        return _current.TryGetValue(kind, out int value) ? value : 0;
    }

    public int Maximum(AmmoKind kind)
    {
        return _maximum.TryGetValue(kind, out int value) ? value : 0;
    }

    /// <summary>Gasta hasta <paramref name="amount"/> y devuelve cuánto pudo gastar de verdad.</summary>
    public int Consume(AmmoKind kind, int amount)
    {
        if (kind == AmmoKind.None)
        {
            return amount;
        }

        int taken = Mathf.Min(Available(kind), amount);
        if (taken <= 0)
        {
            return 0;
        }

        _current[kind] = Available(kind) - taken;
        EmitSignal(SignalName.AmmoChanged, (int)kind, _current[kind], Maximum(kind));

        return taken;
    }

    public void Add(AmmoKind kind, int amount)
    {
        if (kind == AmmoKind.None || amount <= 0)
        {
            return;
        }

        _current[kind] = Mathf.Min(Available(kind) + amount, Maximum(kind));
        EmitSignal(SignalName.AmmoChanged, (int)kind, _current[kind], Maximum(kind));
    }
}
