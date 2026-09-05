using Godot;

namespace DeadKillers.Weapons;

/// <summary>
/// Definición completa de un arma. Vive en un `.tres`, no en una clase: añadir un arma
/// nueva no debe requerir escribir código (D-004, y es la prueba de cierre del hito 2).
///
/// Los campos son los que fija docs/DESIGN.md; los valores, los de docs/BALANCE.md.
/// </summary>
[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public string DisplayName { get; set; } = "Sin nombre";

    [Export] public WeaponKind Kind { get; set; } = WeaponKind.Melee;

    [Export] public int Damage { get; set; } = 10;

    // Segundos entre golpes. En las armas a distancia con cargador, la cadencia real la
    // marca la recarga; aquí queda en 0.
    [Export] public float Cooldown { get; set; } = 0.5f;

    // Disparos antes de tener que recargar. 0 = sin cargador: tira directamente de la
    // reserva, limitada solo por la cadencia.
    [Export] public int MagazineSize { get; set; }

    [Export] public float ReloadTime { get; set; }

    // Desviación aleatoria del disparo, en grados a cada lado.
    [Export] public float SpreadDegrees { get; set; }

    // Alcance del arco en cuerpo a cuerpo; distancia que recorre el proyectil a distancia.
    [Export] public float Range { get; set; } = 2.0f;

    // Amplitud del golpe cuerpo a cuerpo.
    [Export] public float ArcDegrees { get; set; } = 60.0f;

    [Export] public AmmoKind Ammo { get; set; } = AmmoKind.None;

    [Export] public float ProjectileSpeed { get; set; } = 25.0f;

    // A cuántos enemigos alcanza un mismo proyectil. La ballesta atraviesa uno, así que
    // llega a dos (docs/BALANCE.md).
    [Export] public int MaxTargets { get; set; } = 1;

    // La maza es el arma que ignora la armadura plana.
    [Export] public bool IgnoresArmor { get; set; }

    [Export] public PackedScene ProjectileScene { get; set; }

    // Lo que se ve en la mano del portador. Sin esto, el arma equipada es invisible.
    [Export] public PackedScene ModelScene { get; set; }

    // Cada arma se empuña distinto: la espada apunta adelante y arriba, la ballesta
    // recta al frente. Se corrige aquí en vez de rehacer el modelo.
    [Export] public Vector3 ModelRotationDegrees { get; set; } = Vector3.Zero;

    [Export] public AudioStream FireSound { get; set; }

    [Export] public AudioStream ReloadSound { get; set; }

    /// <summary>Un arma sin cargador no recarga nunca: dispara mientras quede reserva.</summary>
    public bool UsesMagazine => MagazineSize > 0;

    public bool NeedsAmmo => Ammo != AmmoKind.None;
}
