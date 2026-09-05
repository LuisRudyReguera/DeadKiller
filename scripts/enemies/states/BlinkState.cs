using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Desaparecer y reaparecer más cerca. Es el recurso del vampiro: castiga al jugador que
/// se confía por la distancia. Solo lo llevan las escenas que lo necesiten.
/// </summary>
[GlobalClass]
public partial class BlinkState : EnemyState
{
    // Cuánto dura el parpadeo entero: la mitad invisible, la mitad reapareciendo.
    [Export] public float Duration { get; set; } = 0.3f;

    private float _elapsed;
    private bool _hasMoved;

    public override void Enter()
    {
        _elapsed = 0.0f;
        _hasMoved = false;

        Agent?.Warn();

        if (Agent?.Body != null)
        {
            Agent.Body.Visible = false;
        }
    }

    public override void Exit()
    {
        if (Agent?.Body != null)
        {
            Agent.Body.Visible = true;
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        if (!IsReady)
        {
            return;
        }

        Agent.StandStill(delta);
        _elapsed += (float)delta;

        // Se reubica a mitad del parpadeo, con el cuerpo oculto: así no se ve deslizarse.
        if (!_hasMoved && _elapsed >= Duration * 0.5f)
        {
            _hasMoved = true;
            Agent.BlinkTowardsTarget();
        }

        if (_elapsed >= Duration)
        {
            RequestTransition(Chase);
        }
    }
}
