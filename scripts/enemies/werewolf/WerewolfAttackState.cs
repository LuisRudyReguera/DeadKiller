using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Atacar: embiste en línea recta a la dirección que fijó al empezar. Si el jugador
/// se ha movido durante el aviso, la embestida pasa de largo.
/// </summary>
[GlobalClass]
public partial class WerewolfAttackState : WerewolfState
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
        if (Agent == null)
        {
            return;
        }

        Agent.Lunge(delta);

        // Un solo mordisco por embestida, aunque el roce dure varios fotogramas.
        if (!_hasLanded && Agent.Bite != null && Agent.Bite.Strike() > 0)
        {
            _hasLanded = true;
        }

        _elapsed += (float)delta;

        if (_elapsed >= Agent.LungeDuration)
        {
            RequestTransition(Recovery);
        }
    }
}
