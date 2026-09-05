using Godot;
using DeadKillers.Components;
using DeadKillers.Weapons;

namespace DeadKillers.Meta;

/// <summary>
/// Vuelca las mejoras compradas sobre el jugador al empezar la misión. Es el único sitio
/// donde el perfil guardado toca al juego: todo lo demás sigue sin saber que existe.
/// </summary>
[GlobalClass]
public partial class Loadout : Node
{
    [Export] public HealthComponent Health { get; set; }

    [Export] public AmmoPouch Ammo { get; set; }

    [Export] public WeaponHolder Weapons { get; set; }

    // Valores de docs/BALANCE.md. Están aquí como [Export] y no como constantes para
    // poder ajustarlos jugando sin recompilar.
    [Export] public int HealthPerLevel { get; set; } = 25;

    [Export] public float ReloadDiscountPerLevel { get; set; } = 0.12f;

    [Export] public int ArrowsPerLevel { get; set; } = 20;

    [Export] public int BoltsPerLevel { get; set; } = 8;

    [Export] public int PowderPerLevel { get; set; } = 3;

    public override void _Ready()
    {
        GameProfile profile = GameProfile.Current;

        ApplyHealth(profile);
        ApplyAmmo(profile);
        ApplyReload(profile);
    }

    private void ApplyHealth(GameProfile profile)
    {
        if (Health == null || profile.HealthLevel <= 0)
        {
            return;
        }

        // Reset y no suma directa: deja la vida actual al máximo nuevo, no al viejo.
        Health.Reset(Health.MaxHealth + profile.HealthLevel * HealthPerLevel, Health.FlatArmor);
    }

    private void ApplyAmmo(GameProfile profile)
    {
        if (Ammo == null || profile.AmmoLevel <= 0)
        {
            return;
        }

        int level = profile.AmmoLevel;

        Ammo.RaiseCapacity(AmmoKind.Arrows, level * ArrowsPerLevel);
        Ammo.RaiseCapacity(AmmoKind.Bolts, level * BoltsPerLevel);
        Ammo.RaiseCapacity(AmmoKind.Powder, level * PowderPerLevel);
    }

    private void ApplyReload(GameProfile profile)
    {
        if (Weapons == null || profile.ReloadLevel <= 0)
        {
            return;
        }

        // Multiplicador, no resta: así una recarga de 4 s y otra de 0,8 s mejoran en la
        // misma proporción y ninguna puede quedar en negativo.
        float discount = Mathf.Min(profile.ReloadLevel * ReloadDiscountPerLevel, 0.6f);
        Weapons.ReloadScale = 1.0f - discount;
    }
}
