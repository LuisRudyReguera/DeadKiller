namespace DeadKillers.Missions;

/// <summary>
/// Qué es una cosa que se recoge del suelo. Las armas van por su cuenta
/// (<c>WeaponPickup</c>) porque llevan un recurso entero detrás, no una cantidad.
/// </summary>
public enum ItemKind
{
    Gold,
    Key,
    Arrows,
    Bolts,
    Powder,
}
