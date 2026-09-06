using Godot;
using System;
using System.Collections.Generic;
using DeadKillers.Level;
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

    [Signal]
    public delegate void EnemyRosterChangedEventHandler();

    public int TotalEnemies { get; private set; }
    public int RemainingEnemies { get; private set; }
    public int PendingAmbushes => _pendingSpawners.Count;

    private readonly Dictionary<HealthComponent, HealthComponent.DiedEventHandler> _enemyHandlers = new();
    private readonly Dictionary<EnemySpawner, LevelResponder.ActivatedEventHandler> _spawnerHandlers = new();
    private readonly HashSet<EnemySpawner> _pendingSpawners = new();

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
        WatchSpawners(GetParent() ?? this);
        WatchEnemies();

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
        if (ExitOpen)
        {
            End(won: true);
        }
    }

    // ------------------------------------------------------------------ interno

    private void WatchEnemies()
    {
        foreach (Node enemy in GetTree().GetNodesInGroup(Groups.Enemy))
        {
            RegisterEnemy(enemy);
        }
    }

    private void RegisterEnemy(Node enemy)
    {
        HealthComponent health = HealthComponent.FindIn(enemy);
        if (IsOver || health == null || _enemyHandlers.ContainsKey(health))
        {
            return;
        }

        // Un hermano puede no haber ejecutado Ready cuando se registra la misión.
        if (health.IsNodeReady() && health.IsDead)
        {
            return;
        }

        bool counted = false;
        HealthComponent.DiedEventHandler died = () =>
        {
            if (counted || IsOver) return;
            counted = true;
            Kills++;
            RemainingEnemies--;
            EmitSignal(SignalName.EnemyRosterChanged);
        };
        _enemyHandlers.Add(health, died);
        health.Died += died;
        TotalEnemies++;
        RemainingEnemies++;
        EmitSignal(SignalName.EnemyRosterChanged);
    }

    private void OnEnemySpawned(Node3D enemy) => RegisterEnemy(enemy);

    private void WatchSpawners(Node root)
    {
        if (root is EnemySpawner spawner && spawner.EnemyScene != null)
        {
            if (!spawner.HasActivated) _pendingSpawners.Add(spawner);
            LevelResponder.ActivatedEventHandler activated = () =>
            {
                // Spawned ya registró toda la oleada antes de Activated.
                _pendingSpawners.Remove(spawner);
                EmitSignal(SignalName.EnemyRosterChanged);
            };
            _spawnerHandlers.Add(spawner, activated);
            spawner.Spawned += OnEnemySpawned;
            spawner.Activated += activated;
        }
        foreach (Node child in root.GetChildren()) WatchSpawners(child);
    }

    public override void _ExitTree()
    {
        foreach (var pair in _enemyHandlers)
            if (IsInstanceValid(pair.Key)) pair.Key.Died -= pair.Value;
        foreach (var pair in _spawnerHandlers)
        {
            if (!IsInstanceValid(pair.Key)) continue;
            pair.Key.Spawned -= OnEnemySpawned;
            pair.Key.Activated -= pair.Value;
        }
        _enemyHandlers.Clear();
        _spawnerHandlers.Clear();
        _pendingSpawners.Clear();
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
