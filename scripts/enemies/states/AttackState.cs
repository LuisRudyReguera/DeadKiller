using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Atacar: arranca en línea recta hacia la dirección que fijó al empezar. Si el jugador
/// se ha movido durante el aviso, pasa de largo.
/// </summary>
[GlobalClass]
public partial class AttackState : EnemyState
{
    private float _elapsed;
    private bool _hasLanded;

    public override void Enter()
    {
        _elapsed = 0.0f;
        _hasLanded = false;

        // La dirección se congela aquí: a partir de ahora no corrige.
        Agent?.AimLunge();
    }

    public override void PhysicsUpdate(double delta)
    {
        if (!IsReady)
        {
            return;
        }

        Agent.Lunge(delta);

        // Un solo golpe por ataque, aunque el roce dure varios fotogramas.
        if (!_hasLanded && Agent.Attack != null && Agent.Attack.Strike() > 0)
        {
            _hasLanded = true;
        }

        _elapsed += (float)delta;

        if (_elapsed >= Data.LungeDuration)
        {
            RequestTransition(Recovery);
        }
    }
}
