using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Alerta: te ha visto y se encara, pero todavía no se lanza. Hace legible el momento en
/// que el enemigo pasa a ser una amenaza.
/// </summary>
[GlobalClass]
public partial class AlertState : EnemyState
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
        Agent.FaceTarget();

        _elapsed += (float)delta;

        if (_elapsed >= Data.AlertDuration)
        {
            RequestTransition(Chase);
        }
    }
}
