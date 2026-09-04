using Godot;

namespace DeadKillers.Components;

/// <summary>
/// Un estado de una <see cref="StateMachine"/>. Las clases derivadas sobrescriben
/// solo lo que necesitan; los métodos vacíos no cuestan nada.
/// </summary>
[GlobalClass]
public partial class State : Node
{
    // Lo emite el propio estado cuando quiere ceder el paso a otro.
    [Signal]
    public delegate void TransitionRequestedEventHandler(StringName nextState);

    /// <summary>
    /// La máquina entrega aquí la entidad dueña de la máquina de estados, para que el
    /// estado no tenga que buscarla subiendo por el árbol.
    /// </summary>
    public virtual void Setup(Node agent)
    {
    }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void Update(double delta)
    {
    }

    public virtual void PhysicsUpdate(double delta)
    {
    }

    /// <summary>
    /// El nombre debe coincidir con el del nodo hermano al que se quiere saltar.
    /// </summary>
    protected void RequestTransition(StringName nextState)
    {
        EmitSignal(SignalName.TransitionRequested, nextState);
    }
}
