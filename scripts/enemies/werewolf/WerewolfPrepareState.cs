using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Preparar ataque: se planta, encara al jugador y AVISA. Es el estado que hace que
/// el juego sea justo; si el jugador muere aquí, tiene que sentir que fue culpa suya.
/// </summary>
[GlobalClass]
public partial class WerewolfPrepareState : WerewolfState
{
    private float _elapsed;

    public override void Enter()
    {
        _elapsed = 0.0f;

        // Las dos mitades del aviso: se pone ámbar y gruñe.
        Agent?.SetTelegraph(true);
        Agent?.Growl();
    }

    public override void Exit()
    {
        Agent?.SetTelegraph(false);
    }

    public override void PhysicsUpdate(double delta)
    {
        if (Agent == null)
        {
            return;
        }

        // Sigue encarando durante el aviso, pero sin avanzar: da tiempo a apartarse.
        Agent.StandStill(delta);
        Agent.FaceTarget();

        _elapsed += (float)delta;

        if (_elapsed >= Agent.AttackWarning)
        {
            RequestTransition(Attack);
        }
    }
}
