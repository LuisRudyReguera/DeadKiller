using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Persecución: va a por el jugador en línea recta. Si consigue alejarse lo bastante,
/// vuelve a quedarse inactivo.
/// </summary>
[GlobalClass]
public partial class WerewolfChaseState : WerewolfState
{
    public override void PhysicsUpdate(double delta)
    {
        if (Agent == null)
        {
            return;
        }

        Agent.MoveTowardsTarget(delta);

        if (Agent.DistanceToTarget() > Agent.LoseSightRange)
        {
            RequestTransition(Idle);
        }
    }
}
