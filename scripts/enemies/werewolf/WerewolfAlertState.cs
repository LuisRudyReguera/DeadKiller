using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Alerta: te ha visto y se gira hacia ti, pero todavía no se lanza. Es el margen que
/// tiene el jugador para reaccionar. Aquí irán después el gruñido y el cambio de postura.
/// </summary>
[GlobalClass]
public partial class WerewolfAlertState : WerewolfState
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
        Agent.FaceTarget();

        _elapsed += (float)delta;

        if (_elapsed >= Agent.AlertDuration)
        {
            RequestTransition(Chase);
        }
    }
}
