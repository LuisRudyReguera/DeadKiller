using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Persecución. Con `StrafeStrength` a 0 va en línea recta, que es lo que hace al
/// licántropo fácil de acertar; con valor alto zigzaguea, que es lo que hace al vampiro
/// castigar el apuntar mal.
/// </summary>
[GlobalClass]
public partial class ChaseState : EnemyState
{
    // Cada cuánto cambia de lado el zigzag. Propio de la escena y no del arquetipo:
    // dos vampiros podrían llevar ritmos distintos.
    [Export] public float StrafeFlipInterval { get; set; } = 0.9f;

    private float _strafeTimer;
    private float _strafeSign = 1.0f;
    private float _sinceBlink;

    public override void Enter()
    {
        _strafeTimer = 0.0f;

        // _sinceBlink NO se reinicia aquí a propósito: mide el tiempo desde el último
        // parpadeo, no desde que empezó esta persecución. Reiniciarlo hacía que no
        // parpadeara nunca, porque entre ataque y ataque vuelve a Chase constantemente.
    }

    public override void PhysicsUpdate(double delta)
    {
        if (!IsReady)
        {
            return;
        }

        float step = (float)delta;
        float distance = Agent.DistanceToTarget();

        // Se comprueba antes de moverse: si ya está en distancia, no da un paso de más.
        if (distance <= Data.PrepareRange)
        {
            Agent.StandStill(delta);
            RequestTransition(Prepare);
            return;
        }

        if (WantsToBlink(step, distance))
        {
            Agent.StandStill(delta);
            RequestTransition(Blink);
            return;
        }

        Agent.MoveTowardsTarget(delta, CurrentStrafe(step));

        if (distance > Data.LoseSightRange)
        {
            RequestTransition(Idle);
        }
    }

    private float CurrentStrafe(float step)
    {
        if (Mathf.IsZeroApprox(Data.StrafeStrength))
        {
            return 0.0f;
        }

        _strafeTimer += step;

        if (_strafeTimer >= StrafeFlipInterval)
        {
            _strafeTimer = 0.0f;
            _strafeSign = -_strafeSign;
        }

        return Data.StrafeStrength * _strafeSign;
    }

    /// <summary>
    /// Solo parpadea quien tenga intervalo configurado. Los arquetipos que no lo usan
    /// nunca piden el estado, así que no importa que su escena no lo tenga.
    /// </summary>
    private bool WantsToBlink(float step, float distance)
    {
        if (Data.BlinkInterval <= 0.0f)
        {
            return false;
        }

        _sinceBlink += step;

        if (_sinceBlink < Data.BlinkInterval)
        {
            return false;
        }

        _sinceBlink = 0.0f;

        // Parpadear cuando ya está encima no aporta nada.
        return distance > Data.PrepareRange + Data.BlinkDistance * 0.5f;
    }
}
