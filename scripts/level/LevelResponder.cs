using Godot;

namespace DeadKillers.Level;

/// <summary>
/// Algo que reacciona a un suceso del nivel: una puerta que se abre, una oleada que
/// aparece, una luz que se enciende.
///
/// Quien lo dispara no sabe qué es: le pide `Activate()` y ya. Añadir un tipo de suceso
/// nuevo es heredar de aquí, sin tocar nada de lo que ya existe.
/// </summary>
public partial class LevelResponder : Node3D
{
    /// <summary>Ha reaccionado. Sirve para encadenar sucesos.</summary>
    [Signal]
    public delegate void ActivatedEventHandler();

    // Si está marcado, solo reacciona la primera vez. Es lo normal.
    [Export] public bool Once { get; set; } = true;

    public bool HasActivated { get; private set; }

    public void Activate()
    {
        if (Once && HasActivated)
        {
            return;
        }

        HasActivated = true;

        OnActivate();
        EmitSignal(SignalName.Activated);
    }

    /// <summary>Lo que hace de verdad. Lo rellenan las clases derivadas.</summary>
    protected virtual void OnActivate()
    {
    }
}
