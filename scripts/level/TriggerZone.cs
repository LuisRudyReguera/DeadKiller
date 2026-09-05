using Godot;
using Godot.Collections;
using DeadKillers.Components;

namespace DeadKillers.Level;

/// <summary>
/// Zona invisible que dispara sucesos cuando el jugador la pisa. Es la pieza con la que
/// se guionizan los niveles sin escribir código: se coloca, se le arrastran los
/// respondedores y ya.
/// </summary>
[GlobalClass]
public partial class TriggerZone : Area3D
{
    [Signal]
    public delegate void TriggeredEventHandler();

    // Qué se activa al pisarla. Se arrastran aquí desde el árbol de la escena.
    [Export] public Array<LevelResponder> Targets { get; set; } = new();

    [Export] public bool Once { get; set; } = true;

    // Retardo antes de disparar. Útil para que algo ocurra un instante después de entrar.
    [Export] public float Delay { get; set; }

    private bool _fired;
    private float _pending = -1.0f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        Targets = null;   // ver D-011
    }

    public override void _Process(double delta)
    {
        _pending -= (float)delta;

        if (_pending > 0.0f)
        {
            return;
        }

        _pending = -1.0f;
        SetProcess(false);
        Fire();
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup(Groups.Player) || (Once && _fired))
        {
            return;
        }

        _fired = true;

        if (Delay > 0.0f)
        {
            _pending = Delay;
            SetProcess(true);
            return;
        }

        Fire();
    }

    private void Fire()
    {
        foreach (LevelResponder target in Targets)
        {
            if (IsInstanceValid(target))
            {
                target.Activate();
            }
        }

        EmitSignal(SignalName.Triggered);
    }
}
