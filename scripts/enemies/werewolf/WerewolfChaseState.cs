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

        float distance = Agent.DistanceToTarget();

        // Se comprueba antes de moverse: si ya está en distancia, no da un paso de más.
        if (distance <= Agent.PrepareRange)
        {
            Agent.StandStill(delta);
            RequestTransition(Prepare);
            return;
        }

        Agent.MoveTowardsTarget(delta);

        if (distance > Agent.LoseSightRange)
        {
            RequestTransition(Idle);
        }
    }
}
