using Godot;
using DeadKillers.Components;

namespace DeadKillers.Missions;

/// <summary>
/// La salida del nivel. Está cerrada hasta que la misión da todos los objetivos por
/// cumplidos, y entonces se abre de forma visible.
/// </summary>
[GlobalClass]
public partial class ExitZone : Area3D
{
    // Malla que cambia de aspecto al desbloquearse. Es el aviso de "ya puedes irte".
    [Export] public MeshInstance3D Marker { get; set; }

    [Export] public Material LockedMaterial { get; set; }

    [Export] public Material OpenMaterial { get; set; }

    private Mission _mission;
    private bool _playerInside;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        _mission = GetTree().GetFirstNodeInGroup(Mission.MissionGroup) as Mission;

        if (_mission == null)
        {
            GD.PushWarning($"{Name}: no hay ninguna misión en la escena; la salida no hará nada.");
            return;
        }

        _mission.ExitUnlocked += OnUnlocked;
        Refresh();
    }

    public override void _ExitTree()
    {
        // Ver D-011.
        LockedMaterial = null;
        OpenMaterial = null;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!body.IsInGroup(Groups.Player))
        {
            return;
        }

        _playerInside = true;
        TryFinish();
    }

    private void OnBodyExited(Node3D body)
    {
        if (body.IsInGroup(Groups.Player))
        {
            _playerInside = false;
        }
    }

    private void OnUnlocked()
    {
        Refresh();

        // Si el jugador ya estaba encima cuando se abrió, se va igualmente.
        TryFinish();
    }

    private void TryFinish()
    {
        if (_playerInside && _mission is { ExitOpen: true, IsOver: false })
        {
            _mission.Complete();
        }
    }

    private void Refresh()
    {
        if (Marker == null)
        {
            return;
        }

        Marker.MaterialOverride = _mission is { ExitOpen: true } ? OpenMaterial : LockedMaterial;
    }
}
