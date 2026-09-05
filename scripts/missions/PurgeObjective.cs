using Godot;
using DeadKillers.Components;

namespace DeadKillers.Missions;

/// <summary>
/// Purga: limpiar la zona de monstruos. Cuenta los enemigos vivos al empezar y se cumple
/// cuando no queda ninguno.
/// </summary>
[GlobalClass]
public partial class PurgeObjective : Objective
{
    private int _total;
    private int _remaining;

    public override string ProgressText => IsComplete
        ? $"{Description} — hecho"
        : $"{Description} — quedan {_remaining} de {_total}";

    public override void Setup(Mission mission)
    {
        base.Setup(mission);

        foreach (Node enemy in GetTree().GetNodesInGroup(Groups.Enemy))
        {
            HealthComponent health = HealthComponent.FindIn(enemy);
            if (health == null)
            {
                continue;
            }

            _total++;
            health.Died += OnEnemyDied;
        }

        _remaining = _total;

        // Una purga sin nadie a quien purgar ya está cumplida.
        if (_total == 0)
        {
            MarkComplete();
        }
    }

    private void OnEnemyDied()
    {
        _remaining--;
        MarkChanged();

        if (_remaining <= 0)
        {
            MarkComplete();
        }
    }
}
