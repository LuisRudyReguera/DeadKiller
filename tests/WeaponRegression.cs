using Godot;
using DeadKillers.Weapons;

namespace DeadKillers.Tests;

public partial class WeaponRegression : Node
{
    private int _failures;

    public override void _Ready()
    {
        var ammo = new AmmoPouch { Bolts = 10 };
        AddChild(ammo);
        var ranged = new WeaponData { Kind = WeaponKind.Ranged, Ammo = AmmoKind.Bolts, MagazineSize = 2, ReloadTime = 1 };
        var melee = new WeaponData();
        var holder = new WeaponHolder { Ammo = ammo };
        holder.Weapons.Add(ranged);
        holder.Weapons.Add(melee);
        AddChild(holder);
        holder.SetProcess(false);
        holder._Process(0.5);
        holder.Equip(0);
        holder._Process(0.6);
        Check(holder.InMagazine == 2 && ammo.Available(AmmoKind.Bolts) == 8, "same weapon does not restart reload");
        holder.Next();
        holder.Previous();
        Check(holder.InMagazine == 2 && !holder.IsReloading, "loaded weapon returns ready");
        Check(ammo.Available(AmmoKind.Bolts) == 8, "switching preserves reserve");
        holder.Next();
        holder.Previous();
        Check(holder.InMagazine + ammo.Available(AmmoKind.Bolts) == 10, "repeated switches conserve ammunition");
        holder.Free();

        var partialAmmo = new AmmoPouch { Bolts = 1 };
        AddChild(partialAmmo);
        var partial = new WeaponHolder { Ammo = partialAmmo };
        partial.Weapons.Add(ranged);
        partial.Weapons.Add(melee);
        AddChild(partial);
        partial.SetProcess(false);
        partial._Process(2);
        partial.Next();
        partialAmmo.Add(AmmoKind.Bolts, 5);
        partial.Previous();
        Check(partial.InMagazine == 1 && !partial.IsReloading, "partial magazine is immediately available");
        partial.TryReload();
        partial.Next();
        Check(partialAmmo.Available(AmmoKind.Bolts) == 5, "cancelled reload spends no reserve");
        partial.Previous();
        Check(partial.InMagazine == 1, "cancelled reload retains loaded ammunition");
        partial.Free();
        ranged.Dispose();
        melee.Dispose();
        GD.Print($"Weapon regression failures: {_failures}");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Check(bool condition, string label)
    {
        if (condition) GD.Print($"PASS: {label}");
        else { _failures++; GD.PushError($"FAIL: {label}"); }
    }
}
