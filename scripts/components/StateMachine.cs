using Godot;
using Godot.Collections;

namespace DeadKillers.Components;

/// <summary>
/// Máquina de estados por nodos: cada hijo de tipo <see cref="State"/> es un estado,
/// y se salta de uno a otro por el nombre del nodo.
/// </summary>
[GlobalClass]
public partial class StateMachine : Node
{
    [Signal]
    public delegate void StateChangedEventHandler(StringName stateName);

    // Estado en el que arranca. Si se deja vacío, se usa el primer hijo de tipo State.
    [Export] public State InitialState { get; set; }

    // Entidad dueña de la máquina, que se pasa a cada estado. Si se deja vacío se usa
    // el nodo padre, que es donde cuelga la máquina en la práctica.
    [Export] public Node Agent { get; set; }

    public State CurrentState { get; private set; }

    private readonly Dictionary<StringName, State> _states = new();

    public override void _Ready()
    {
        Agent ??= GetParent();

        foreach (Node child in GetChildren())
        {
            if (child is not State state)
            {
                continue;
            }

            _states[state.Name] = state;
            state.Setup(Agent);
            state.TransitionRequested += OnTransitionRequested;

            InitialState ??= state;
        }

        if (InitialState == null)
        {
            GD.PushError($"{GetPath()}: la máquina de estados no tiene ningún hijo de tipo State.");
            return;
        }

        CurrentState = InitialState;
        CurrentState.Enter();
        EmitSignal(SignalName.StateChanged, CurrentState.Name);
    }

    public override void _Process(double delta)
    {
        CurrentState?.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        CurrentState?.PhysicsUpdate(delta);
    }

    private void OnTransitionRequested(StringName nextState)
    {
        if (!_states.TryGetValue(nextState, out State next))
        {
            // Un nombre mal escrito no da error por su cuenta: rompería la IA en silencio.
            GD.PushError($"{GetPath()}: no existe el estado '{nextState}'.");
            return;
        }

        if (next == CurrentState)
        {
            return;
        }

        CurrentState?.Exit();
        CurrentState = next;
        CurrentState.Enter();
        EmitSignal(SignalName.StateChanged, CurrentState.Name);
    }
}
