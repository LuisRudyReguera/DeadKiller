using Godot;
using DeadKillers.Meta;

namespace DeadKillers.Ui;

/// <summary>
/// La tienda entre misiones. Es el único sitio donde se gasta el oro, y también el único
/// donde el juego escribe en disco.
/// </summary>
public partial class ShopScreen : Control
{
    [Export] public Label GoldLabel { get; set; }

    [Export] public Label ProgressLabel { get; set; }

    [Export] public Button HealthButton { get; set; }

    [Export] public Button ReloadButton { get; set; }

    [Export] public Button AmmoButton { get; set; }

    [Export] public Button NextButton { get; set; }

    [Export(PropertyHint.File, "*.tscn")] public string[] Missions { get; set; } =
    {
        "res://scenes/levels/Mission01.tscn",
        "res://scenes/levels/Mission02.tscn",
    };

    public override void _Ready()
    {
        HealthButton.Pressed += () => Buy(UpgradeKind.Health);
        ReloadButton.Pressed += () => Buy(UpgradeKind.Reload);
        AmmoButton.Pressed += () => Buy(UpgradeKind.Ammo);
        NextButton.Pressed += StartNextMission;

        Refresh();
    }

    private void Buy(UpgradeKind kind)
    {
        if (!GameProfile.Current.Buy(kind))
        {
            return;
        }

        // Se guarda en cuanto se compra: si el juego se cierra aquí, la compra está hecha.
        GameProfile.Current.Save();
        Refresh();
    }

    private void StartNextMission()
    {
        GameProfile profile = GameProfile.Current;

        if (Missions.Length == 0)
        {
            return;
        }

        // Al pasar la última se vuelve a empezar por la primera: todavía no hay final.
        string next = Missions[profile.MissionsCompleted % Missions.Length];
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, next);
    }

    private void Refresh()
    {
        GameProfile profile = GameProfile.Current;

        GoldLabel.Text = $"Oro   {profile.Gold}";
        ProgressLabel.Text = $"Misiones completadas   {profile.MissionsCompleted}";

        Describe(HealthButton, "Vida", UpgradeKind.Health, "+25 de vida máxima");
        Describe(ReloadButton, "Recarga", UpgradeKind.Reload, "-12 % de tiempo de recarga");
        Describe(AmmoButton, "Munición", UpgradeKind.Ammo, "más capacidad de los tres tipos");

        NextButton.Text = profile.MissionsCompleted == 0
            ? "Empezar la primera misión"
            : "Siguiente misión";
    }

    private static void Describe(Button button, string title, UpgradeKind kind, string effect)
    {
        GameProfile profile = GameProfile.Current;
        int level = profile.LevelOf(kind);

        if (level >= GameProfile.MaxUpgradeLevel)
        {
            button.Text = $"{title}  ({level}/{GameProfile.MaxUpgradeLevel})   al máximo";
            button.Disabled = true;
            return;
        }

        button.Text = $"{title}  ({level}/{GameProfile.MaxUpgradeLevel})   {profile.PriceOf(kind)} de oro\n{effect}";
        button.Disabled = !profile.CanBuy(kind);
    }
}
