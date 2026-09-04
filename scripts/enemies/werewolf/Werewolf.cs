using Godot;
using DeadKillers.Components;

namespace DeadKillers.Enemies;

/// <summary>
/// Licántropo: rápido, embiste en línea recta, poca vida. Esta clase solo guarda los
/// datos y sabe moverse; quién decide qué hacer en cada momento es su StateMachine.
/// </summary>
[GlobalClass]
public partial class Werewolf : CharacterBody3D
{
    // Velocidad de persecución en m/s (docs/BALANCE.md). La embestida es más rápida
    // y llega con el estado de ataque.
    [Export] public float MoveSpeed { get; set; } = 7.0f;

    // Distancia a la que detecta al jugador (docs/BALANCE.md).
    [Export] public float SightRange { get; set; } = 12.0f;

    // Distancia a la que lo pierde. Mayor que SightRange a propósito: si fueran iguales,
    // el enemigo entraría y saldría de persecución en el borde exacto.
    [Export] public float LoseSightRange { get; set; } = 16.0f;

    // Segundos que tarda en reaccionar tras detectar al jugador, antes de lanzarse.
    [Export] public float AlertDuration { get; set; } = 0.6f;

    [Export] public HealthComponent Health { get; set; }

    [Export] public StateMachine States { get; set; }

    // Se deja vacío: se resuelve solo buscando al jugador por su grupo.
    [Export] public Node3D Target { get; set; }

    public const string PlayerGroup = "player";

    // Margen mínimo para no llamar a LookAt sobre la propia posición.
    private const float MinLookDistance = 0.01f;

    private float _gravity;

    public bool HasTarget => Target != null && IsInstanceValid(Target);

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        Target ??= GetTree().GetFirstNodeInGroup(PlayerGroup) as Node3D;
        if (Target == null)
        {
            GD.PushWarning($"{Name}: no hay ningún nodo en el grupo '{PlayerGroup}'; se quedará quieto.");
        }

        if (Health != null)
        {
            Health.Died += OnDied;
        }
    }

    /// <summary>
    /// Distancia al jugador en el plano horizontal. Devuelve infinito si no hay objetivo,
    /// de modo que cualquier comparación de "¿está cerca?" da que no sin casos especiales.
    /// </summary>
    public float DistanceToTarget()
    {
        if (!HasTarget)
        {
            return float.PositiveInfinity;
        }

        Vector3 delta = Target.GlobalPosition - GlobalPosition;
        delta.Y = 0.0f;
        return delta.Length();
    }

    /// <summary>Avanza hacia el jugador a MoveSpeed. Lo llaman los estados, no _PhysicsProcess.</summary>
    public void MoveTowardsTarget(double delta)
    {
        if (!HasTarget)
        {
            StandStill(delta);
            return;
        }

        Vector3 direction = Target.GlobalPosition - GlobalPosition;
        direction.Y = 0.0f;
        direction = direction.Normalized();

        Velocity = new Vector3(direction.X * MoveSpeed, Velocity.Y, direction.Z * MoveSpeed);
        FaceTarget();
        ApplyGravityAndSlide(delta);
    }

    /// <summary>Se queda parado sin dejar de estar sujeto a la gravedad.</summary>
    public void StandStill(double delta)
    {
        Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
        ApplyGravityAndSlide(delta);
    }

    public void FaceTarget()
    {
        if (!HasTarget)
        {
            return;
        }

        var look = new Vector3(Target.GlobalPosition.X, GlobalPosition.Y, Target.GlobalPosition.Z);
        if (GlobalPosition.DistanceSquaredTo(look) < MinLookDistance * MinLookDistance)
        {
            return;
        }

        LookAt(look, Vector3.Up);
    }

    private void ApplyGravityAndSlide(double delta)
    {
        if (!IsOnFloor())
        {
            Velocity += Vector3.Down * _gravity * (float)delta;
        }

        MoveAndSlide();
    }

    private void OnDied()
    {
        // El estado de Muerte y el reinicio de la escena llegan en la siguiente tarea.
        // De momento se apaga la máquina de estados para que no siga persiguiendo ya muerto.
        States?.SetPhysicsProcess(false);
        States?.SetProcess(false);
        Velocity = Vector3.Zero;
    }
}
