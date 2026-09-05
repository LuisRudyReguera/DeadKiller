using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Preparar ataque: se planta, encara al jugador y AVISA, de forma visible y audible.
/// Es el estado que hace justo al juego. docs/DESIGN.md: un enemigo que hace daño sin
/// aviso previo es un bug de diseño aunque el código funcione.
/// </summary>
[GlobalClass]
public partial class PrepareState : EnemyState
{
    private float _elapsed;

    public override void Enter()
    {
        _elapsed = 0.0f;

        Agent?.SetTelegraph(true);
        Agent?.Warn();
    }

    public override void Exit()
    {
        Agent?.SetTelegraph(false);
    }

    public override void PhysicsUpdate(double delta)
    {
        if (!IsReady)
        {
            return;
        }

        // Sigue encarando durante el aviso, pero sin avanzar: da tiempo a apartarse.
        Agent.StandStill(delta);
        Agent.FaceTarget();

        _elapsed += (float)delta;

        if (_elapsed >= Data.AttackWarning)
        {
            RequestTransition(Attack);
        }
    }
}
