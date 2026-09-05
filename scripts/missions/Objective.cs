using Godot;

namespace DeadKillers.Missions;

/// <summary>
/// Un objetivo de misión. Las clases derivadas deciden cuándo se cumplen; la misión solo
/// pregunta si están hechos y muestra su texto.
/// </summary>
public partial class Objective : Node
{
    /// <summary>Algo ha cambiado: hay que repintar el HUD y revisar si ya está todo.</summary>
    [Signal]
    public delegate void ChangedEventHandler();

    [Export] public string Description { get; set; } = "Objetivo";

    public bool IsComplete { get; protected set; }

    protected Mission Mission { get; private set; }

    /// <summary>La misión se presenta al objetivo antes de que empiece nada.</summary>
    public virtual void Setup(Mission mission)
    {
        Mission = mission;
    }

    /// <summary>Lo que se lee en el HUD. Por defecto, solo la descripción.</summary>
    public virtual string ProgressText => Description;

    protected void MarkChanged()
    {
        EmitSignal(SignalName.Changed);
    }

    protected void MarkComplete()
    {
        if (IsComplete)
        {
            return;
        }

        IsComplete = true;
        MarkChanged();
    }
}
