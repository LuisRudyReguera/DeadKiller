using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Recuperación: queda expuesto tras atacar. Es la ventana en la que el jugador puede
/// acercarse a golpear sin que le devuelvan el golpe.
/// </summary>
[GlobalClass]
public partial class RecoveryState : EnemyState
{
    private float _elapsed;

    public override void Enter()
    {
        _elapsed = 0.0f;
    }

    public override void PhysicsUpdate(double delta)
    {
        if (!IsReady)
        {
            return;
        }

        Agent.StandStill(delta);
        _elapsed += (float)delta;

        if (_elapsed >= Data.RecoveryDuration)
        {
            RequestTransition(Chase);
        }
    }
}
