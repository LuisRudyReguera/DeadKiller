using Godot;
using DeadKillers.Missions;

namespace DeadKillers.Meta;

/// <summary>
/// Encadena la campaña: al terminar una misión guarda el botín y lleva a la tienda, o
/// reinicia el nivel si el cazador ha caído.
///
/// El guardado ocurre AQUÍ y solo aquí, al cerrar la misión: nunca dentro del nivel
/// (D-005).
/// </summary>
[GlobalClass]
public partial class CampaignFlow : Node
{
    // A dónde se va tras completarla. Vacío = volver a la tienda.
    [Export(PropertyHint.File, "*.tscn")] public string ShopScene { get; set; } = "res://scenes/ui/Shop.tscn";

    // Margen para que dé tiempo a leer el resumen antes de que se pueda continuar.
    [Export] public float ContinueDelay { get; set; } = 0.8f;

    private Mission _mission;
    private bool _won;
    private float _sinceEnd = -1.0f;

    public override void _Ready()
    {
        _mission = GetTree().GetFirstNodeInGroup(Mission.MissionGroup) as Mission;

        if (_mission == null)
        {
            GD.PushWarning($"{Name}: no hay misión en la escena; la campaña no avanzará.");
            return;
        }

        _mission.MissionEnded += OnMissionEnded;
    }

    public override void _Process(double delta)
    {
        if (_sinceEnd < 0.0f)
        {
            return;
        }

        _sinceEnd += (float)delta;

        if (_sinceEnd < ContinueDelay)
        {
            return;
        }

        if (!Input.IsActionJustPressed("interact") && !Input.IsActionJustPressed("attack_primary"))
        {
            return;
        }

        _sinceEnd = -1.0f;

        if (_won)
        {
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, ShopScene);
        }
        else
        {
            GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
        }
    }

    /// <summary>
    /// Al ganar, el oro de la misión pasa al perfil y se guarda. Al perder no se guarda
    /// nada: morir no debe dejar poso, solo volver a intentarlo.
    /// </summary>
    private void OnMissionEnded(bool won)
    {
        _won = won;
        _sinceEnd = 0.0f;

        if (!won)
        {
            return;
        }

        GameProfile profile = GameProfile.Current;
        profile.Gold += _mission.Gold;
        profile.MissionsCompleted++;
        profile.Save();
    }
}
