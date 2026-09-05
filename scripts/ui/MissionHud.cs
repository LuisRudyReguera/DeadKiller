using Godot;
using System.Text;
using DeadKillers.Components;
using DeadKillers.Missions;
using DeadKillers.Weapons;

namespace DeadKillers.Ui;

/// <summary>
/// Lo que el jugador necesita saber sin apartar la vista del combate: vida, arma, munición
/// y qué le falta para poder salir.
///
/// Solo lee y pinta. No decide nada: se suscribe a las señales de quien sí decide.
/// </summary>
public partial class MissionHud : CanvasLayer
{
    [Export] public Label HealthLabel { get; set; }

    [Export] public Label WeaponLabel { get; set; }

    [Export] public Label ObjectivesLabel { get; set; }

    [Export] public Label TimeLabel { get; set; }

    private Mission _mission;
    private HealthComponent _health;
    private WeaponHolder _weapons;

    public override void _Ready()
    {
        _mission = GetTree().GetFirstNodeInGroup(Mission.MissionGroup) as Mission;

        Node player = GetTree().GetFirstNodeInGroup(Groups.Player);
        _health = HealthComponent.FindIn(player);
        _weapons = FindWeapons(player);

        if (_health != null)
        {
            _health.HealthChanged += OnHealthChanged;
            OnHealthChanged(_health.Current, _health.MaxHealth);
        }

        if (_weapons != null)
        {
            _weapons.WeaponChanged += OnWeaponChanged;
            _weapons.AmmoStateChanged += OnAmmoChanged;
        }

        if (_mission != null)
        {
            _mission.ObjectivesChanged += RefreshObjectives;
        }

        RefreshWeapon();
        RefreshObjectives();
    }

    public override void _Process(double delta)
    {
        if (TimeLabel == null || _mission == null)
        {
            return;
        }

        int total = (int)_mission.ElapsedSeconds;
        TimeLabel.Text = $"{total / 60:00}:{total % 60:00}";
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

    private void OnHealthChanged(int current, int max)
    {
        if (HealthLabel != null)
        {
            HealthLabel.Text = $"Vida  {current} / {max}";
        }
    }

    private void OnWeaponChanged(int index, string displayName)
    {
        RefreshWeapon();
    }

    private void OnAmmoChanged(int inMagazine, int reserve)
    {
        RefreshWeapon();
    }

    private void RefreshWeapon()
    {
        if (WeaponLabel == null || _weapons?.Current == null)
        {
            return;
        }

        WeaponData weapon = _weapons.Current;

        if (!weapon.NeedsAmmo)
        {
            WeaponLabel.Text = weapon.DisplayName;
            return;
        }

        int reserve = _weapons.Ammo?.Available(weapon.Ammo) ?? 0;

        // Sin cargador no tiene sentido enseñar "0 / 30": se enseña solo la reserva.
        string load = weapon.UsesMagazine ? $"{_weapons.InMagazine} / {reserve}" : reserve.ToString();
        string reloading = _weapons.IsReloading ? "  (recargando)" : string.Empty;

        WeaponLabel.Text = $"{weapon.DisplayName}   {load}{reloading}";
    }

    private void RefreshObjectives()
    {
        if (ObjectivesLabel == null || _mission == null)
        {
            return;
        }

        var text = new StringBuilder();

        foreach (Objective objective in _mission.Objectives)
        {
            text.Append(objective.IsComplete ? "[x] " : "[ ] ");
            text.AppendLine(objective.ProgressText);
        }

        text.AppendLine(_mission.ExitOpen
            ? "La salida está abierta."
            : "La salida está cerrada.");

        ObjectivesLabel.Text = text.ToString().TrimEnd();
    }
}
