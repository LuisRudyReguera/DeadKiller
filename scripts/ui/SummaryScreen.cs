using Godot;
using System.Text;
using DeadKillers.Components;
using DeadKillers.Missions;
using DeadKillers.Weapons;

namespace DeadKillers.Ui;

/// <summary>
/// Pantalla de resumen al terminar la misión, se gane o se pierda: bajas, puntería, oro y
/// tiempo. Aquí se reintenta.
/// </summary>
public partial class SummaryScreen : CanvasLayer
{
    [Export] public Control Panel { get; set; }

    [Export] public Label TitleLabel { get; set; }

    [Export] public Label StatsLabel { get; set; }

    [Export] public Label HintLabel { get; set; }

    // Margen antes de aceptar la tecla de reintentar: sin él, el clic con el que mataste
    // al último enemigo reinicia la partida sin que te dé tiempo a leer nada.
    [Export] public float InputDelay { get; set; } = 0.8f;

    private float _sinceShown = -1.0f;

    public override void _Ready()
    {
        Visible = false;

        if (GetTree().GetFirstNodeInGroup(Mission.MissionGroup) is Mission mission)
        {
            mission.MissionEnded += OnMissionEnded;
        }
        else
        {
            GD.PushWarning($"{Name}: no hay ninguna misión; el resumen no aparecerá nunca.");
        }
    }

    public override void _Process(double delta)
    {
        if (_sinceShown < 0.0f)
        {
            return;
        }

        _sinceShown += (float)delta;

        if (_sinceShown < InputDelay)
        {
            return;
        }

        if (HintLabel != null && !HintLabel.Visible)
        {
            HintLabel.Visible = true;
        }

        if (Input.IsActionJustPressed("interact") || Input.IsActionJustPressed("attack_primary"))
        {
            GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
        }
    }

    private void OnMissionEnded(bool won)
    {
        Visible = true;
        _sinceShown = 0.0f;

        if (HintLabel != null)
        {
            HintLabel.Visible = false;
        }

        if (TitleLabel != null)
        {
            TitleLabel.Text = won ? "Misión cumplida" : "Has caído";
        }

        if (StatsLabel != null)
        {
            StatsLabel.Text = BuildStats();
        }
    }

    private string BuildStats()
    {
        var mission = GetTree().GetFirstNodeInGroup(Mission.MissionGroup) as Mission;
        if (mission == null)
        {
            return string.Empty;
        }

        Node player = GetTree().GetFirstNodeInGroup(Groups.Player);
        WeaponHolder weapons = FindWeapons(player);

        int made = weapons?.AttacksMade ?? 0;
        int landed = weapons?.AttacksLanded ?? 0;

        // Sin un solo ataque no hay puntería que calcular; un 0 % sería mentira.
        string accuracy = made > 0 ? $"{landed * 100 / made} %  ({landed} de {made})" : "—";

        int seconds = (int)mission.ElapsedSeconds;

        var text = new StringBuilder();
        text.AppendLine($"Bajas       {mission.Kills}");
        text.AppendLine($"Puntería    {accuracy}");
        text.AppendLine($"Oro         {mission.Gold}");
        text.Append($"Tiempo      {seconds / 60:00}:{seconds % 60:00}");

        return text.ToString();
    }

    private static WeaponHolder FindWeapons(Node player)
    {
        if (player == null)
        {
            return null;
        }

        foreach (Node child in player.GetChildren())
        {
            if (child is WeaponHolder holder)
            {
                return holder;
            }
        }

        return null;
    }
}
