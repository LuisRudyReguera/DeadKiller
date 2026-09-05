using Godot;

namespace DeadKillers.Missions;

/// <summary>
/// Llave: encontrar un objeto que abre un paso. Sirve para cualquier cosa contable, no
/// solo llaves: basta con cambiar el tipo y la cantidad.
/// </summary>
[GlobalClass]
public partial class CollectObjective : Objective
{
    [Export] public ItemKind Kind { get; set; } = ItemKind.Key;

    [Export] public int Required { get; set; } = 1;

    private int _collected;

    public override string ProgressText => IsComplete
        ? $"{Description} — hecho"
        : $"{Description} — {_collected} de {Required}";

    public override void Setup(Mission mission)
    {
        base.Setup(mission);

        mission.ItemCollected += OnItemCollected;
    }

    private void OnItemCollected(int kind, int amount)
    {
        if ((ItemKind)kind != Kind)
        {
            return;
        }

        _collected += amount;
        MarkChanged();

        if (_collected >= Required)
        {
            MarkComplete();
        }
    }
}
