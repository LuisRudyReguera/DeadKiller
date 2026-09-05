using Godot;

namespace DeadKillers.Enemies;

/// <summary>Inactivo: quieto hasta que el jugador entra en su distancia de visión.</summary>
[GlobalClass]
public partial class IdleState : EnemyState
{
    public override void PhysicsUpdate(double delta)
    {
        if (!IsReady)
        {
            return;
        }

        Agent.StandStill(delta);

        if (Agent.DistanceToTarget() <= Data.SightRange)
        {
            RequestTransition(Alert);
        }
    }
}
