using Godot;
using DeadKillers.Meta;

namespace DeadKillers.Tests;

public partial class ProfileRegression : Node
{
    private int _failures;

    public override void _Ready()
    {
        var valid = GameProfile.FromJson("{\"gold\":150,\"missions\":2,\"health\":1,\"reload\":2,\"ammo\":3}");
        Check(valid.Gold == 150 && valid.MissionsCompleted == 2 && valid.HealthLevel == 1 && valid.ReloadLevel == 2 && valid.AmmoLevel == 3, "valid profile preserved");
        foreach (string text in new[] { "broken", "[]", "null", "{}" })
        {
            var empty = GameProfile.FromJson(text);
            Check(empty.Gold == 0 && empty.HealthLevel == 0, "invalid or empty profile usable");
        }
        var damaged = GameProfile.FromJson("{\"gold\":-40,\"missions\":-1,\"health\":-1,\"reload\":99,\"ammo\":1.5}");
        Check(damaged.Gold == 0 && damaged.MissionsCompleted == 0 && damaged.HealthLevel == 0 && damaged.ReloadLevel == 3 && damaged.AmmoLevel == 0, "invalid ranges normalized");
        Check(damaged.PriceOf(UpgradeKind.Health) == 40, "damaged level cannot crash shop");
        var types = GameProfile.FromJson("{\"gold\":\"100\",\"health\":true,\"ammo\":[],\"reload\":{}}");
        Check(types.Gold == 0 && types.HealthLevel == 0 && types.AmmoLevel == 0 && types.ReloadLevel == 0, "wrong types rejected");
        var huge = GameProfile.FromJson("{\"gold\":1e30,\"missions\":1e30,\"health\":1e30}");
        Check(huge.Gold == int.MaxValue && huge.MissionsCompleted == int.MaxValue && huge.HealthLevel == 3, "large values cannot overflow");
        var buyer = new GameProfile { Gold = 300 };
        Check(!buyer.Buy((UpgradeKind)999) && buyer.Gold == 300 && buyer.AmmoLevel == 0, "unknown upgrade cannot spend gold");
        Check(buyer.Buy(UpgradeKind.Health) && buyer.HealthLevel == 1 && buyer.Gold == 260, "purchase applies correct price");
        buyer.HealthLevel = 3;
        Check(!buyer.Buy(UpgradeKind.Health) && buyer.Gold == 260, "max level cannot spend gold");
        buyer.HealthLevel = -1;
        Check(!buyer.Buy(UpgradeKind.Health), "invalid in-memory level cannot crash purchase");
        buyer.Gold = 0;
        Check(!buyer.Buy(UpgradeKind.Ammo), "insufficient funds rejected");
        GD.Print($"Profile regression failures: {_failures}");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool condition, string label)
    {
        if (condition) GD.Print($"PASS: {label}");
        else { _failures++; GD.PushError($"FAIL: {label}"); }
    }
}
