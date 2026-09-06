using Godot;
using System.Reflection;
using DeadKillers.Meta;
using DeadKillers.Missions;

namespace DeadKillers.Tests;

public partial class CampaignRegression : Node
{
    public override async void _Ready()
    {
        // No victoria ni acceso al perfil: la prueba no escribe guardados.
        var player = new Node3D();
        player.AddToGroup("player");
        player.AddChild(new DeadKillers.Components.HealthComponent());
        AddChild(player);
        var mission = new Mission();
        mission.AddToGroup(Mission.MissionGroup);
        var flow = new CampaignFlow { ContinueDelay = 0 };
        AddChild(mission);
        AddChild(flow);
        flow.SetProcess(false);
        mission.EmitSignal(Mission.SignalName.MissionEnded, false);
        var timer = typeof(CampaignFlow).GetField("_sinceEnd", BindingFlags.NonPublic | BindingFlags.Instance);
        var armed = typeof(CampaignFlow).GetField("_continueArmed", BindingFlags.NonPublic | BindingFlags.Instance);
        Input.ActionPress("interact");
        flow._Process(1);
        bool held = !(bool)armed.GetValue(flow) && (float)timer.GetValue(flow) >= 0;
        Input.ActionRelease("interact");
        flow._Process(1);
        bool released = (bool)armed.GetValue(flow);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ActionPress("attack_primary");
        flow._Process(1);
        bool attackIgnored = (float)timer.GetValue(flow) >= 0;
        Input.ActionRelease("attack_primary");
        GD.Print($"Campaign input: held E blocked={held}, release arms={released}, attack ignored={attackIgnored}");
        GetTree().Quit(held && released && attackIgnored ? 0 : 1);
    }
}
