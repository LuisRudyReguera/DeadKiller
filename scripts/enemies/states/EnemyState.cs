using Godot;
using DeadKillers.Components;

namespace DeadKillers.Enemies;

/// <summary>
/// Base de los estados de un enemigo. Centraliza el acceso a la entidad y los nombres de
/// los estados: escribirlos sueltos en cada transición es pedir una errata que rompería
/// la IA sin dar ningún error.
/// </summary>
public partial class EnemyState : State
{
    protected static readonly StringName Idle = "Idle";
    protected static readonly StringName Alert = "Alert";
    protected static readonly StringName Chase = "Chase";
    protected static readonly StringName Prepare = "Prepare";
    protected static readonly StringName Attack = "Attack";
    protected static readonly StringName Recovery = "Recovery";
    protected static readonly StringName Blink = "Blink";

    protected Enemy Agent { get; private set; }

    protected EnemyData Data => Agent?.Data;

    /// <summary>Nada que hacer si falta la entidad o sus datos.</summary>
    protected bool IsReady => Agent != null && Agent.Data != null;

    public override void Setup(Node agent)
    {
        Agent = agent as Enemy;

        if (Agent == null)
        {
            GD.PushError($"{GetPath()}: la máquina de estados no cuelga de un Enemy.");
        }
    }
}
