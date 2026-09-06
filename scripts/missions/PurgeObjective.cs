using Godot;

namespace DeadKillers.Missions;

/// <summary>Purga los enemigos iniciales y las emboscadas pendientes del nivel.</summary>
[GlobalClass]
public partial class PurgeObjective : Objective
{
    public override string ProgressText => IsComplete
        ? $"{Description} - hecho"
        : $"{Description} - quedan {Mission.RemainingEnemies} de {Mission.TotalEnemies}"
          + (Mission.PendingAmbushes > 0
              ? $" - emboscadas pendientes: {Mission.PendingAmbushes}"
              : "");

    public override void Setup(Mission mission)
    {
        base.Setup(mission);
        mission.EnemyRosterChanged += RefreshProgress;
        RefreshProgress();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(Mission))
            Mission.EnemyRosterChanged -= RefreshProgress;
    }

    private void RefreshProgress()
    {
        IsComplete = Mission.RemainingEnemies == 0 && Mission.PendingAmbushes == 0;
        MarkChanged();
    }
}
