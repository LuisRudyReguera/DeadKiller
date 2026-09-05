using Godot;

namespace DeadKillers.Components;

/// <summary>
/// Vida de una entidad. No conoce a su padre: funciona igual en el jugador, en un
/// enemigo o en un barril. Quien quiera reaccionar a los cambios se suscribe a sus señales.
/// </summary>
[GlobalClass]
public partial class HealthComponent : Node
{
    [Signal]
    public delegate void HealthChangedEventHandler(int current, int max);

    [Signal]
    public delegate void DiedEventHandler();

    [Export] public int MaxHealth { get; set; } = 100;

    // Armadura plana: se resta del daño recibido (docs/BALANCE.md).
    [Export] public int FlatArmor { get; set; }

    // La armadura nunca puede anular un golpe del todo (docs/BALANCE.md).
    private const int MinimumDamage = 1;

    public int Current { get; private set; }

    public bool IsDead => Current <= 0;

    public override void _Ready()
    {
        Current = MaxHealth;
    }

    /// <summary>
    /// Busca el componente de vida entre los hijos directos de un nodo. Lo usan los
    /// hitbox para no tener que conocer el tipo concreto de a quién golpean.
    /// </summary>
    public static HealthComponent FindIn(Node node)
    {
        if (node == null)
        {
            return null;
        }

        foreach (Node child in node.GetChildren())
        {
            if (child is HealthComponent health)
            {
                return health;
            }
        }

        return null;
    }

    /// <summary>
    /// Aplica daño descontando la armadura. La maza es el caso de <paramref name="ignoresArmor"/>.
    /// </summary>
    public void TakeDamage(int amount, bool ignoresArmor = false)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        int effective = ignoresArmor ? amount : Mathf.Max(amount - FlatArmor, MinimumDamage);
        Current = Mathf.Max(Current - effective, 0);

        EmitSignal(SignalName.HealthChanged, Current, MaxHealth);

        if (IsDead)
        {
            EmitSignal(SignalName.Died);
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        Current = Mathf.Min(Current + amount, MaxHealth);
        EmitSignal(SignalName.HealthChanged, Current, MaxHealth);
    }
}
