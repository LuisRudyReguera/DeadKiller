using Godot;
using DeadKillers.Components;

namespace DeadKillers.Enemies;

/// <summary>
/// Licántropo: rápido, embiste en línea recta, poca vida. Esta clase guarda los datos
/// y sabe moverse, golpear y avisar de que va a golpear; quién decide qué hacer en
/// cada momento es su StateMachine.
/// </summary>
[GlobalClass]
public partial class Werewolf : CharacterBody3D
{
    // --- Números de docs/BALANCE.md ---

    [Export] public float MoveSpeed { get; set; } = 7.0f;

    // Velocidad de la embestida, más alta que la de persecución.
    [Export] public float LungeSpeed { get; set; } = 11.0f;

    [Export] public float SightRange { get; set; } = 12.0f;

    // Alcance del mordisco.
    [Export] public float AttackRange { get; set; } = 1.5f;

    // Segundos de aviso antes de que el golpe conecte. Nunca puede ser 0: es el margen
    // que tiene el jugador para reaccionar y lo que hace que el juego sea justo.
    [Export] public float AttackWarning { get; set; } = 0.4f;

    // --- Números propuestos, pendientes de anotar en BALANCE.md ---

    // Distancia a la que deja de perseguir y empieza a prepararse. Mayor que el alcance
    // para que la embestida tenga recorrido y se pueda esquivar retrocediendo.
    [Export] public float PrepareRange { get; set; } = 2.5f;

    // Distancia a la que pierde al jugador. Mayor que SightRange a propósito: si fueran
    // iguales, entraría y saldría de persecución en el borde exacto.
    [Export] public float LoseSightRange { get; set; } = 16.0f;

    [Export] public float AlertDuration { get; set; } = 0.6f;

    [Export] public float LungeDuration { get; set; } = 0.25f;

    // Pausa tras embestir. Es la ventana en la que el jugador puede contraatacar.
    [Export] public float RecoveryDuration { get; set; } = 0.6f;

    // --- Referencias, asignadas en la escena ---

    [Export] public HealthComponent Health { get; set; }

    [Export] public StateMachine States { get; set; }

    [Export] public HitboxComponent Bite { get; set; }

    [Export] public MeshInstance3D Body { get; set; }

    [Export] public AudioStreamPlayer3D Voice { get; set; }

    // Se deja vacío: se resuelve solo buscando al jugador por su grupo.
    [Export] public Node3D Target { get; set; }

    // Color al que vira mientras telegrafía. Ámbar encendido, para que se lea de un
    // vistazo desde la cámara cenital.
    private static readonly Color TelegraphColor = new(1.0f, 0.72f, 0.15f);

    private const float MinLookDistance = 0.01f;
    private const float TelegraphGlow = 1.6f;

    private float _gravity;
    private Vector3 _lungeDirection = Vector3.Forward;
    private StandardMaterial3D _material;
    private Color _restColor;

    public bool HasTarget => Target != null && IsInstanceValid(Target);

    public bool IsDead => Health != null && Health.IsDead;

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        Target ??= GetTree().GetFirstNodeInGroup(Groups.Player) as Node3D;
        if (Target == null)
        {
            GD.PushWarning($"{Name}: no hay ningún nodo en el grupo '{Groups.Player}'; se quedará quieto.");
        }

        if (Health != null)
        {
            Health.Died += OnDied;
        }

        // Cada licántropo telegrafía por su cuenta porque el material está marcado como
        // local a la escena: Godot le da su propia copia a cada instancia. Duplicarlo
        // aquí funcionaba igual, pero dejaba el recurso vivo al cerrar el juego.
        if (Body?.MaterialOverride is StandardMaterial3D material)
        {
            _material = material;
            _restColor = _material.AlbedoColor;
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

    /// <summary>Avanza hacia el jugador. Lo llaman los estados, no _PhysicsProcess.</summary>
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

    /// <summary>
    /// Congela la dirección de la embestida. Se llama una vez al empezar el ataque:
    /// embiste en línea recta, así que apuntar mal es su forma de fallar.
    /// </summary>
    public void AimLunge()
    {
        if (!HasTarget)
        {
            _lungeDirection = -GlobalBasis.Z;
            _lungeDirection.Y = 0.0f;
            _lungeDirection = _lungeDirection.Normalized();
            return;
        }

        Vector3 direction = Target.GlobalPosition - GlobalPosition;
        direction.Y = 0.0f;
        _lungeDirection = direction.Normalized();

        FaceTarget();
    }

    public void Lunge(double delta)
    {
        Velocity = new Vector3(
            _lungeDirection.X * LungeSpeed,
            Velocity.Y,
            _lungeDirection.Z * LungeSpeed);

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

    /// <summary>
    /// Aviso visible de que va a atacar. docs/DESIGN.md: un enemigo que hace daño sin
    /// aviso previo es un bug de diseño aunque el código funcione.
    /// </summary>
    public void SetTelegraph(bool active)
    {
        if (_material == null)
        {
            return;
        }

        _material.AlbedoColor = active ? TelegraphColor : _restColor;
        _material.EmissionEnabled = active;
        _material.Emission = TelegraphColor;
        _material.EmissionEnergyMultiplier = active ? TelegraphGlow : 0.0f;
    }

    /// <summary>Aviso audible, la otra mitad del telegrafiado.</summary>
    public void Growl()
    {
        Voice?.Play();
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
        SetTelegraph(false);

        States?.SetPhysicsProcess(false);
        States?.SetProcess(false);

        // Deja de estorbar y de morder, pero no se borra: quien lleve la cuenta de
        // enemigos vivos necesita que el nodo siga existiendo.
        // Diferido a propósito: la muerte llega desde un Strike() en pleno ciclo de
        // física, y Godot bloquea tocar 'monitoring' dentro de una llamada de colisión.
        Bite?.SetDeferred(Area3D.PropertyName.Monitoring, false);

        Velocity = Vector3.Zero;
        SetPhysicsProcess(false);
    }
}
