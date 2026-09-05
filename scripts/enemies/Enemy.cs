using Godot;
using DeadKillers.Components;
using DeadKillers.Missions;

namespace DeadKillers.Enemies;

/// <summary>
/// Cualquier enemigo. No sabe si es un licántropo, un vampiro o un demonio: todo lo que
/// los distingue está en su <see cref="EnemyData"/> y en qué estados cuelgue su escena.
///
/// Guarda los datos y sabe moverse, golpear y avisar. Quién decide qué hacer en cada
/// momento es su StateMachine, y son los estados quienes lo mueven (D-009).
/// </summary>
[GlobalClass]
public partial class Enemy : CharacterBody3D
{
    [Export] public EnemyData Data { get; set; }

    // --- Referencias de la escena ---

    [Export] public HealthComponent Health { get; set; }

    [Export] public StateMachine States { get; set; }

    [Export] public HitboxComponent Attack { get; set; }

    [Export] public CollisionShape3D AttackShape { get; set; }

    [Export] public MeshInstance3D Body { get; set; }

    [Export] public AudioStreamPlayer3D Voice { get; set; }

    // Se deja vacío: se resuelve solo buscando al jugador por su grupo.
    [Export] public Node3D Target { get; set; }

    private const float MinLookDistance = 0.01f;
    private const float TelegraphGlow = 1.6f;

    private float _gravity;
    private Vector3 _lungeDirection = Vector3.Forward;

    // Copias de lo que hace falta al morir: para entonces Data ya puede estar suelto.
    private PackedScene _drop;
    private int _minGold;
    private int _maxGold;
    private StandardMaterial3D _material;
    private Color _restColor;

    public bool HasTarget => Target != null && IsInstanceValid(Target);

    public bool IsDead => Health != null && Health.IsDead;

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        if (Data == null)
        {
            GD.PushError($"{Name}: falta el EnemyData; el enemigo no sabe qué es.");
            return;
        }

        Target ??= GetTree().GetFirstNodeInGroup(Groups.Player) as Node3D;
        if (Target == null)
        {
            GD.PushWarning($"{Name}: no hay ningún nodo en el grupo '{Groups.Player}'; se quedará quieto.");
        }

        ApplyData();

        if (Health != null)
        {
            Health.Died += OnDied;
        }
    }

    /// <summary>
    /// Vuelca el `.tres` sobre los componentes de la escena. Es lo que permite que tres
    /// arquetipos compartan escena y código y aun así se jueguen distinto.
    /// </summary>
    private void ApplyData()
    {
        // Reset y no asignación directa: el componente ya fijó su vida en su propio
        // _Ready, que corre antes que el del padre.
        Health?.Reset(Data.MaxHealth, Data.FlatArmor);

        if (Attack != null)
        {
            Attack.Damage = Data.Damage;
            Attack.ArcDegrees = Data.AttackArcDegrees;
            Attack.IgnoresArmor = Data.IgnoresArmor;
        }

        if (AttackShape?.Shape is SphereShape3D sphere)
        {
            sphere.Radius = Data.AttackRange;
        }

        if (Voice != null)
        {
            Voice.Stream = Data.WarningSound;
        }

        _drop = Data.DropScene;
        _minGold = Data.MinGold;
        _maxGold = Data.MaxGold;

        // El material está marcado como local a la escena, así que cada instancia tiene
        // el suyo y puede llevar su propio color sin pisar a los demás.
        if (Body?.MaterialOverride is StandardMaterial3D material)
        {
            _material = material;
            _material.AlbedoColor = Data.BodyColor;
            _restColor = Data.BodyColor;
        }
    }

    /// <summary>
    /// Se apunta al grupo por su cuenta, para que quien lleve la cuenta de enemigos vivos
    /// no dependa de que cada escena se acuerde de marcarlo.
    /// </summary>
    public override void _EnterTree()
    {
        AddToGroup(Groups.Enemy);
    }

    public override void _ExitTree()
    {
        // Soltar la referencia al recurso: un campo de C# lo mantiene vivo más allá del
        // cierre del motor y Godot lo denuncia como fuga (D-011).
        _material = null;
        Data = null;

        // Parar ANTES de soltar el stream: un sonido a medias al cerrar deja vivo su
        // objeto de reproducción, y Godot lo denuncia como fuga (D-011).
        if (IsInstanceValid(Voice))
        {
            Voice.Stop();
            Voice.Stream = null;
        }

        // El material es local a la escena, así que cada instancia tiene el suyo: hay
        // que soltarlo explícitamente o se queda vivo al cerrar.
        if (IsInstanceValid(Body))
        {
            Body.MaterialOverlay = null;
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

    /// <summary>Dirección horizontal normalizada hacia el jugador.</summary>
    public Vector3 DirectionToTarget()
    {
        if (!HasTarget)
        {
            return Vector3.Zero;
        }

        Vector3 direction = Target.GlobalPosition - GlobalPosition;
        direction.Y = 0.0f;
        return direction.Normalized();
    }

    /// <summary>
    /// Avanza hacia el jugador. <paramref name="strafe"/> desplaza lateralmente: es lo
    /// que hace que el vampiro sea difícil de acertar y el licántropo no.
    /// </summary>
    public void MoveTowardsTarget(double delta, float strafe = 0.0f)
    {
        if (!HasTarget)
        {
            StandStill(delta);
            return;
        }

        Vector3 direction = DirectionToTarget();

        if (!Mathf.IsZeroApprox(strafe))
        {
            Vector3 sideways = direction.Cross(Vector3.Up);
            direction = (direction + sideways * strafe).Normalized();
        }

        Velocity = new Vector3(
            direction.X * Data.MoveSpeed,
            Velocity.Y,
            direction.Z * Data.MoveSpeed);

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
    /// Congela la dirección del ataque. Se llama una vez al empezar: el arranque va en
    /// línea recta, así que si el jugador se aparta durante el aviso, pasa de largo.
    /// </summary>
    public void AimLunge()
    {
        Vector3 direction = HasTarget ? DirectionToTarget() : -GlobalBasis.Z with { Y = 0 };
        _lungeDirection = direction.Normalized();

        FaceTarget();
    }

    public void Lunge(double delta)
    {
        Velocity = new Vector3(
            _lungeDirection.X * Data.LungeSpeed,
            Velocity.Y,
            _lungeDirection.Z * Data.LungeSpeed);

        ApplyGravityAndSlide(delta);
    }

    /// <summary>
    /// Reaparece más cerca del jugador sin recorrer el hueco. Lo usa el vampiro; los
    /// demás nunca lo llaman porque no llevan el estado que lo dispara.
    /// </summary>
    public void BlinkTowardsTarget()
    {
        if (!HasTarget)
        {
            return;
        }

        float distance = DistanceToTarget();
        float step = Mathf.Min(Data.BlinkDistance, Mathf.Max(distance - Data.PrepareRange, 0.0f));

        if (step <= 0.0f)
        {
            return;
        }

        GlobalPosition += DirectionToTarget() * step;
        FaceTarget();
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
        if (_material == null || Data == null)
        {
            return;
        }

        _material.AlbedoColor = active ? Data.TelegraphColor : _restColor;
        _material.EmissionEnabled = active;
        _material.Emission = Data.TelegraphColor;
        _material.EmissionEnergyMultiplier = active ? TelegraphGlow : 0.0f;
    }

    /// <summary>Aviso audible, la otra mitad del telegrafiado. Cada tipo suena distinto.</summary>
    public void Warn()
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

        // Deja de contar como enemigo vivo y deja de estorbar. El nodo sigue existiendo
        // porque de él saldrá el oro en el hito 4, pero un cadáver no debe bloquear el
        // paso ni aparecer en una búsqueda de objetivos.
        RemoveFromGroup(Groups.Enemy);
        SetDeferred(PropertyName.CollisionLayer, 0u);

        States?.SetPhysicsProcess(false);
        States?.SetProcess(false);

        // Diferido: la muerte llega desde un Strike() en pleno ciclo de física, y Godot
        // bloquea tocar 'monitoring' dentro de una llamada de colisión.
        Attack?.SetDeferred(Area3D.PropertyName.Monitoring, false);

        Velocity = Vector3.Zero;
        SetPhysicsProcess(false);

        // Diferido: la muerte llega en pleno ciclo de física, y añadir nodos al árbol
        // desde dentro de una llamada de colisión no es seguro.
        CallDeferred(MethodName.SpawnDrop);
    }

    /// <summary>Suelta el oro donde cayó. Lo recoge el jugador pasando por encima.</summary>
    private void SpawnDrop()
    {
        if (_drop == null || !IsInsideTree())
        {
            return;
        }

        if (_drop.Instantiate() is not Node3D loot)
        {
            return;
        }

        if (loot is ItemPickup pickup)
        {
            pickup.Kind = ItemKind.Gold;
            pickup.Amount = (int)GD.RandRange(_minGold, _maxGold);
        }

        GetParent().AddChild(loot);
        loot.GlobalPosition = GlobalPosition;
    }
}
