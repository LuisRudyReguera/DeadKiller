using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Inactivo: quieto en su sitio hasta que el jugador entra en su distancia de visión.
/// </summary>
[GlobalClass]
public partial class WerewolfIdleState : WerewolfState
{
    public override void PhysicsUpdate(double delta)
    {
        if (Agent == null)
        {
            return;
        }

        Agent.StandStill(delta);

        if (Agent.DistanceToTarget() <= Agent.SightRange)
        {
            RequestTransition(Alert);
        }
    }
}
