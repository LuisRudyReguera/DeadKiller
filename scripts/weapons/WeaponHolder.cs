using Godot;
using Godot.Collections;
using DeadKillers.Components;

namespace DeadKillers.Weapons;

/// <summary>
/// Lleva las armas del portador y ejecuta lo que diga el `.tres` de la que tenga
/// equipada. No conoce ningún arma concreta: espada, arco y ballesta pasan por aquí
/// sin una sola rama con su nombre. Añadir una cuarta es crear otro `.tres`.
/// </summary>
[GlobalClass]
public partial class WeaponHolder : Node
{
    [Signal]
    public delegate void WeaponChangedEventHandler(int index, string displayName);

    [Signal]
    public delegate void AmmoStateChangedEventHandler(int inMagazine, int reserve);

    [Signal]
    public delegate void ReloadStartedEventHandler(float seconds);

    [Export] public Array<WeaponData> Weapons { get; set; } = new();

    // --- Referencias de la escena ---

    [Export] public HitboxComponent MeleeHitbox { get; set; }

    [Export] public CollisionShape3D MeleeShape { get; set; }

    // Desde dónde salen los proyectiles y hacia dónde miran.
    [Export] public Node3D Muzzle { get; set; }

    // Dónde cuelga el modelo del arma equipada. Está en la mano del portador.
    [Export] public Node3D WeaponSocket { get; set; }

    [Export] public AmmoPouch Ammo { get; set; }

    [Export] public AudioStreamPlayer3D Sound { get; set; }

    // Reproductor aparte para el impacto: si compartiera el de disparo, cada golpe
    // cortaría su propio silbido.
    [Export] public AudioStreamPlayer3D ImpactSound { get; set; }

    // Pivote del arco visible. Se escala al alcance y amplitud reales del arma, para que
    // lo que se ve sea exactamente lo que golpea.
    [Export] public Node3D SwingVisual { get; set; }

    [Export] public float SwingVisualDuration { get; set; } = 0.12f;

    // Medidas de la cuña tal como está modelada en la escena, para poder escalarla.
    [Export] public float SwingVisualBaseHalfWidth { get; set; } = 1.15f;

    [Export] public float SwingVisualBaseRange { get; set; } = 2.0f;

    public WeaponData Current { get; private set; }

    public int CurrentIndex { get; private set; }

    public int InMagazine { get; private set; }

    public bool IsReloading => _reloadLeft > 0.0f;

    // Multiplicador del tiempo de recarga. Lo baja la mejora de la tienda; 1 = sin mejora.
    public float ReloadScale { get; set; } = 1.0f;

    // Puntería de la misión. Un ataque cuerpo a cuerpo cuenta como "acertado" si tocó a
    // alguien; uno a distancia, si su proyectil llegó a alcanzar a alguien.
    public int AttacksMade { get; private set; }

    public int AttacksLanded { get; private set; }

    private float _cooldown;
    private float _reloadLeft;
    private float _swingLeft;

    public override void _Ready()
    {
        if (SwingVisual != null)
        {
            SwingVisual.Visible = false;
        }

        if (MeleeHitbox != null)
        {
            MeleeHitbox.Hit += OnMeleeHit;
        }

        Equip(0);
    }

    private void OnMeleeHit(Node3D target)
    {
        ImpactSound?.Play();
    }

    /// <summary>
    /// Un proyectil puede atravesar a varios; la puntería cuenta el disparo, no las
    /// víctimas, así que solo suma la primera vez.
    /// </summary>
    private void OnProjectileHit()
    {
        AttacksLanded++;
    }

    /// <summary>
    /// Suelta las referencias a recursos al salir del árbol. Un campo de C# que apunta a
    /// un Resource lo mantiene vivo más allá del cierre del juego, y Godot lo denuncia
    /// como "resources still in use at exit".
    /// </summary>
    public override void _ExitTree()
    {
        Current = null;
        Weapons = null;

        // Parar antes de soltar: un sonido a medias al cerrar deja vivo su objeto de
        // reproducción (D-011).
        if (IsInstanceValid(Sound))
        {
            Sound.Stop();
            Sound.Stream = null;
        }

        if (IsInstanceValid(ImpactSound))
        {
            ImpactSound.Stop();
        }
    }

    public override void _Process(double delta)
    {
        float step = (float)delta;

        if (_cooldown > 0.0f)
        {
            _cooldown -= step;
        }

        UpdateSwingVisual(step);
        UpdateReload(step);
    }

    // ---------------------------------------------------------------- equipar

    public void Equip(int index)
    {
        if (Weapons == null || Weapons.Count == 0)
        {
            Current = null;
            return;
        }

        CurrentIndex = Mathf.PosMod(index, Weapons.Count);
        Current = Weapons[CurrentIndex];

        _cooldown = 0.0f;
        _reloadLeft = 0.0f;
        InMagazine = 0;

        ApplyMeleeProfile();
        ShowWeaponModel();

        EmitSignal(SignalName.WeaponChanged, CurrentIndex, Current?.DisplayName ?? string.Empty);
        NotifyAmmoState();

        // Un arma con cargador llega vacía: se carga sola al sacarla.
        if (Current != null && Current.UsesMagazine)
        {
            TryReload();
        }
    }

    public void Next() => Equip(CurrentIndex + 1);

    public void Previous() => Equip(CurrentIndex - 1);

    /// <summary>
    /// Añade un arma recogida del suelo. Devuelve false si ya se llevaba, para que
    /// quien la ofrezca sepa que no debe desaparecer.
    /// </summary>
    public bool AddWeapon(WeaponData weapon)
    {
        if (weapon == null || Weapons.Contains(weapon))
        {
            return false;
        }

        Weapons.Add(weapon);
        Equip(Weapons.Count - 1);

        return true;
    }

    /// <summary>Guarda el arma: al morir no debe seguir golpeando ni recargando.</summary>
    public void Holster()
    {
        _reloadLeft = 0.0f;
        _swingLeft = 0.0f;

        if (SwingVisual != null)
        {
            SwingVisual.Visible = false;
        }

        // Diferido: la muerte puede llegar desde un Strike() en pleno ciclo de física,
        // y Godot bloquea tocar 'monitoring' dentro de una llamada de colisión.
        MeleeHitbox?.SetDeferred(Area3D.PropertyName.Monitoring, false);
    }

    // ---------------------------------------------------------------- disparar

    public bool TryFire()
    {
        if (Current == null || _cooldown > 0.0f || IsReloading)
        {
            return false;
        }

        return Current.Kind == WeaponKind.Melee ? FireMelee() : FireRanged();
    }

    private bool FireMelee()
    {
        _cooldown = Current.Cooldown;
        AttacksMade++;

        Play(Current.FireSound);
        ShowSwing();

        bool landed = MeleeHitbox != null && MeleeHitbox.Strike() > 0;
        if (landed)
        {
            AttacksLanded++;
        }

        return landed;
    }

    private bool FireRanged()
    {
        if (Current.ProjectileScene == null || Muzzle == null)
        {
            GD.PushWarning($"{Name}: '{Current.DisplayName}' no tiene proyectil o boca de disparo.");
            return false;
        }

        if (Current.UsesMagazine)
        {
            if (InMagazine <= 0)
            {
                // Gatillo en seco: en vez de no hacer nada, empieza a recargar.
                TryReload();
                return false;
            }

            InMagazine--;
        }
        else if (Ammo == null || Ammo.Consume(Current.Ammo, 1) <= 0)
        {
            return false;
        }

        SpawnProjectile();
        AttacksMade++;

        _cooldown = Current.Cooldown;
        Play(Current.FireSound);
        NotifyAmmoState();

        if (Current.UsesMagazine && InMagazine <= 0)
        {
            TryReload();
        }

        return true;
    }

    private void SpawnProjectile()
    {
        if (Current.ProjectileScene.Instantiate() is not Projectile projectile)
        {
            GD.PushError($"{Name}: la escena de proyectil de '{Current.DisplayName}' no es un Projectile.");
            return;
        }

        projectile.Speed = Current.ProjectileSpeed;
        projectile.Damage = Current.Damage;
        projectile.MaxTargets = Current.MaxTargets;
        projectile.IgnoresArmor = Current.IgnoresArmor;
        projectile.MaxDistance = Current.Range;

        projectile.TargetHit += OnProjectileHit;

        // Al nivel, no al jugador: si colgara del jugador se movería con él.
        Node parent = GetTree().CurrentScene ?? GetTree().Root;
        parent.AddChild(projectile);

        projectile.GlobalTransform = Muzzle.GlobalTransform;

        if (Current.SpreadDegrees > 0.0f)
        {
            float deviation = (float)GD.RandRange(-Current.SpreadDegrees, Current.SpreadDegrees);
            projectile.RotateY(Mathf.DegToRad(deviation));
        }
    }

    // ---------------------------------------------------------------- recargar

    public void TryReload()
    {
        if (Current == null || !Current.UsesMagazine || IsReloading)
        {
            return;
        }

        if (InMagazine >= Current.MagazineSize)
        {
            return;
        }

        if (Ammo == null || Ammo.Available(Current.Ammo) <= 0)
        {
            return;
        }

        _reloadLeft = Current.ReloadTime * ReloadScale;
        Play(Current.ReloadSound);

        EmitSignal(SignalName.ReloadStarted, _reloadLeft);

        // Una recarga instantánea se resuelve ya, sin esperar al siguiente fotograma.
        if (_reloadLeft <= 0.0f)
        {
            FinishReload();
        }
    }

    private void UpdateReload(float delta)
    {
        if (!IsReloading)
        {
            return;
        }

        _reloadLeft -= delta;
        if (_reloadLeft <= 0.0f)
        {
            FinishReload();
        }
    }

    private void FinishReload()
    {
        _reloadLeft = 0.0f;

        if (Current == null || Ammo == null)
        {
            return;
        }

        InMagazine += Ammo.Consume(Current.Ammo, Current.MagazineSize - InMagazine);
        NotifyAmmoState();
    }

    // ---------------------------------------------------------------- apoyo

    /// <summary>
    /// Traslada el alcance y el arco del arma al hitbox y al aviso visual. Así una espada
    /// y una maza con distinta amplitud se comportan y se ven distinto sin tocar código.
    /// </summary>
    private void ApplyMeleeProfile()
    {
        if (Current == null || MeleeHitbox == null)
        {
            return;
        }

        bool melee = Current.Kind == WeaponKind.Melee;

        MeleeHitbox.Damage = Current.Damage;
        MeleeHitbox.ArcDegrees = Current.ArcDegrees;
        MeleeHitbox.IgnoresArmor = Current.IgnoresArmor;

        if (MeleeShape?.Shape is SphereShape3D sphere && melee)
        {
            sphere.Radius = Current.Range;
        }

        if (SwingVisual == null || !melee)
        {
            return;
        }

        float halfWidth = Mathf.Tan(Mathf.DegToRad(Current.ArcDegrees * 0.5f)) * Current.Range;

        SwingVisual.Scale = new Vector3(
            halfWidth / SwingVisualBaseHalfWidth,
            1.0f,
            Current.Range / SwingVisualBaseRange);
    }

    /// <summary>
    /// Cuelga en la mano el modelo del arma equipada y retira el anterior. Un arma que
    /// no trae modelo simplemente no se ve; no es un error.
    /// </summary>
    private void ShowWeaponModel()
    {
        if (WeaponSocket == null)
        {
            return;
        }

        foreach (Node child in WeaponSocket.GetChildren())
        {
            child.QueueFree();
        }

        if (Current?.ModelScene == null || Current.ModelScene.Instantiate() is not Node3D model)
        {
            return;
        }

        WeaponSocket.AddChild(model);
        model.Position = Vector3.Zero;
        model.RotationDegrees = Current.ModelRotationDegrees;
    }

    private void ShowSwing()
    {
        if (SwingVisual == null)
        {
            return;
        }

        SwingVisual.Visible = true;
        _swingLeft = SwingVisualDuration;
    }

    private void UpdateSwingVisual(float delta)
    {
        if (_swingLeft <= 0.0f)
        {
            return;
        }

        _swingLeft -= delta;

        if (_swingLeft <= 0.0f && SwingVisual != null)
        {
            SwingVisual.Visible = false;
        }
    }

    private void Play(AudioStream stream)
    {
        if (Sound == null || stream == null)
        {
            return;
        }

        Sound.Stream = stream;
        Sound.Play();
    }

    private void NotifyAmmoState()
    {
        int reserve = Current != null && Ammo != null ? Ammo.Available(Current.Ammo) : 0;
        EmitSignal(SignalName.AmmoStateChanged, InMagazine, reserve);
    }
}
