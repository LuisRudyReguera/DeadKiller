using Godot;
using DeadKillers.Components;

namespace DeadKillers.Missions;

/// <summary>
/// Lleva la misión: sus objetivos, lo que el jugador recoge y las cifras del resumen.
/// Es el punto por el que se comunican piezas que no tienen por qué conocerse entre sí
/// —una moneda del suelo no necesita saber qué es un objetivo—, y el único que sabe
/// cuándo la misión está ganada o perdida.
/// </summary>
[GlobalClass]
public partial class Mission : Node
{
    public const string MissionGroup = "mission";

    [Signal]
    public delegate void ObjectivesChangedEventHandler();

    [Signal]
    public delegate void ItemCollectedEventHandler(int kind, int amount);

    /// <summary>Todos los objetivos cumplidos: la salida se abre.</summary>
    [Signal]
    public delegate void ExitUnlockedEventHandler();

    /// <summary>Fin de partida. <paramref name="won"/> distingue salir vivo de morir.</summary>
    [Signal]
    public delegate void MissionEndedEventHandler(bool won);

    [Export] public string Title { get; set; } = "Misión sin nombre";

    // Nodo que contiene los objetivos como hijos. Si se deja vacío, se usa este mismo.
    [Export] public Node ObjectiveRoot { get; set; }

    // --- Cifras del resumen ---

    public int Kills { get; private set; }

    public int Gold { get; private set; }

    public double ElapsedSeconds { get; private set; }

    public bool IsOver { get; private set; }

    public bool ExitOpen { get; private set; }

    public Godot.Collections.Array<Objective> Objectives { get; } = new();

    /// <summary>
    /// Se apunta al grupo al ENTRAR al árbol, no en _Ready. Todos los _EnterTree ocurren
    /// antes que cualquier _Ready, así que quien la busque la encuentra sin depender de
    /// en qué orden estén los nodos en la escena.
    /// </summary>
    public override void _EnterTree()
    {
        AddToGroup(MissionGroup);
    }

    public override void _Ready()
    {
        foreach (Node child in (ObjectiveRoot ?? this).GetChildren())
        {
            if (child is not Objective objective)
            {
                continue;
            }

            Objectives.Add(objective);
            objective.Setup(this);
            objective.Changed += OnObjectiveChanged;
        }

        WatchEnemies();
        WatchPlayer();

        EmitSignal(SignalName.ObjectivesChanged);
        CheckObjectives();
    }

    public override void _Process(double delta)
    {
        if (IsOver)
        {
            return;
        }

        ElapsedSeconds += delta;
    }

    /// <summary>Lo llaman las cosas recogidas del suelo.</summary>
    public void Collect(ItemKind kind, int amount)
    {
        if (IsOver || amount <= 0)
        {
            return;
        }

        if (kind == ItemKind.Gold)
        {
            Gold += amount;
        }

        EmitSignal(SignalName.ItemCollected, (int)kind, amount);
    }

    /// <summary>Lo llama la salida cuando el jugador la pisa con todo cumplido.</summary>
    public void Complete()
    {
        End(won: true);
    }

    // ------------------------------------------------------------------ interno

    private void WatchEnemies()
    {
        foreach (Node enemy in GetTree().GetNodesInGroup(Groups.Enemy))
        {
            HealthComponent health = HealthComponent.FindIn(enemy);
            if (health != null)
            {
                health.Died += OnEnemyDied;
            }
        }
    }

    private void WatchPlayer()
    {
        Node player = GetTree().GetFirstNodeInGroup(Groups.Player);
        HealthComponent health = HealthComponent.FindIn(player);

        if (health == null)
        {
            GD.PushWarning($"{Name}: el jugador no tiene HealthComponent; no se podrá perder.");
            return;
        }

        health.Died += OnPlayerDied;
    }

    private void OnEnemyDied()
    {
        Kills++;
        CheckObjectives();
    }

    private void OnPlayerDied()
    {
        End(won: false);
    }

    private void OnObjectiveChanged()
    {
        EmitSignal(SignalName.ObjectivesChanged);
        CheckObjectives();
    }

    /// <summary>
    /// La salida se abre una sola vez. No se vuelve a cerrar aunque algo cambie después:
    /// quitarle al jugador una salida que ya se le había abierto sería desconcertante.
    /// </summary>
    private void CheckObjectives()
    {
        if (IsOver || ExitOpen)
        {
            return;
        }

        foreach (Objective objective in Objectives)
        {
            if (!objective.IsComplete)
            {
                return;
            }
        }

        ExitOpen = true;
        EmitSignal(SignalName.ExitUnlocked);
        EmitSignal(SignalName.ObjectivesChanged);
    }

    private void End(bool won)
    {
        if (IsOver)
        {
            return;
        }

        IsOver = true;
        EmitSignal(SignalName.MissionEnded, won);
    }
}
