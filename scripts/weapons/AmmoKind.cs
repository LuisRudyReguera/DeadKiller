namespace DeadKillers.Weapons;

/// <summary>
/// Tipos de munición. No hay munición universal: es lo que obliga a alternar armas
/// (docs/DESIGN.md). `None` es para el cuerpo a cuerpo.
/// </summary>
public enum AmmoKind
{
    None,
    Arrows,
    Bolts,
    Powder,
}
