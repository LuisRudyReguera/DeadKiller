using Godot;
using DeadKillers.Components;

namespace DeadKillers.Enemies;

/// <summary>
/// Base de los estados del licántropo. Centraliza el acceso a la entidad y los nombres
/// de los estados: escribirlos sueltos en cada transición es pedir una errata que
/// rompería la IA sin dar ningún error.
/// </summary>
public partial class WerewolfState : State
{
    protected static readonly StringName Idle = "Idle";
    protected static readonly StringName Alert = "Alert";
    protected static readonly StringName Chase = "Chase";
    protected static readonly StringName Prepare = "Prepare";
    protected static readonly StringName Attack = "Attack";
    protected static readonly StringName Recovery = "Recovery";

    protected Werewolf Agent { get; private set; }

    public override void Setup(Node agent)
    {
        Agent = agent as Werewolf;

        if (Agent == null)
        {
            GD.PushError($"{GetPath()}: la máquina de estados no cuelga de un Werewolf.");
        }
    }
}
