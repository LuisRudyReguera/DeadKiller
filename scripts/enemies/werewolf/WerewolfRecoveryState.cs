using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Recuperación: queda expuesto un momento tras embestir. Es la ventana en la que el
/// jugador puede acercarse a golpear sin que le muerdan.
/// </summary>
[GlobalClass]
public partial class WerewolfRecoveryState : WerewolfState
{
    private float _elapsed;

    public override void Enter()
    {
        _elapsed = 0.0f;
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Agent == null)
        {
            return;
        }

        Agent.StandStill(delta);
        _elapsed += (float)delta;

        if (_elapsed >= Agent.RecoveryDuration)
        {
            RequestTransition(Chase);
        }
    }
}
