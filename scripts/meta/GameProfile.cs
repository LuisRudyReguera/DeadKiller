using Godot;

namespace DeadKillers.Meta;

/// <summary>
/// Lo que sobrevive entre misiones: oro, mejoras compradas y por dónde va la campaña.
///
/// No es un Node ni un Resource a propósito. Es un objeto plano que se guarda en JSON,
/// para que el formato del guardado no dependa de cómo esté hecha una escena hoy.
/// Se guarda SOLO entre misiones, nunca dentro del nivel (D-005).
/// </summary>
public sealed class GameProfile
{
    private const string SavePath = "user://profile.json";

    // Niveles máximos de cada mejora. Comprar más allá no hace nada.
    public const int MaxUpgradeLevel = 3;

    public int Gold { get; set; }

    public int MissionsCompleted { get; set; }

    public int HealthLevel { get; set; }

    public int ReloadLevel { get; set; }

    public int AmmoLevel { get; set; }

    private static GameProfile _current;

    /// <summary>Perfil en uso. Se carga del disco la primera vez que alguien lo pide.</summary>
    public static GameProfile Current => _current ??= Load();

    public int LevelOf(UpgradeKind kind) => kind switch
    {
        UpgradeKind.Health => HealthLevel,
        UpgradeKind.Reload => ReloadLevel,
        UpgradeKind.Ammo => AmmoLevel,
        _ => 0,
    };

    public bool CanBuy(UpgradeKind kind) =>
        LevelOf(kind) < MaxUpgradeLevel && Gold >= PriceOf(kind);

    /// <summary>
    /// Cada nivel cuesta más que el anterior. Los precios están en docs/BALANCE.md.
    /// </summary>
    public int PriceOf(UpgradeKind kind)
    {
        int level = LevelOf(kind);

        if (level >= MaxUpgradeLevel)
        {
            return 0;
        }

        int[] prices = kind switch
        {
            UpgradeKind.Health => new[] { 40, 70, 110 },
            UpgradeKind.Reload => new[] { 50, 90, 140 },
            _ => new[] { 30, 60, 100 },
        };

        return prices[level];
    }

    public bool Buy(UpgradeKind kind)
    {
        if (!CanBuy(kind))
        {
            return false;
        }

        Gold -= PriceOf(kind);

        switch (kind)
        {
            case UpgradeKind.Health:
                HealthLevel++;
                break;
            case UpgradeKind.Reload:
                ReloadLevel++;
                break;
            default:
                AmmoLevel++;
                break;
        }

        return true;
    }

    /// <summary>Empezar de cero. Lo usa el menú de campaña.</summary>
    public static void Reset()
    {
        _current = new GameProfile();
        _current.Save();
    }

    public void Save()
    {
        var data = new Godot.Collections.Dictionary
        {
            { "gold", Gold },
            { "missions", MissionsCompleted },
            { "health", HealthLevel },
            { "reload", ReloadLevel },
            { "ammo", AmmoLevel },
        };

        using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);

        if (file == null)
        {
            GD.PushError($"No se pudo escribir {SavePath}: {FileAccess.GetOpenError()}");
            return;
        }

        file.StoreString(Json.Stringify(data, "  "));
    }

    private static GameProfile Load()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            return new GameProfile();
        }

        using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);

        if (file == null)
        {
            GD.PushWarning($"No se pudo leer {SavePath}; se empieza de cero.");
            return new GameProfile();
        }

        // Un guardado corrupto no debe impedir jugar: se empieza de cero y se avisa.
        if (Json.ParseString(file.GetAsText()).VariantType != Variant.Type.Dictionary)
        {
            GD.PushWarning($"{SavePath} no tiene el formato esperado; se empieza de cero.");
            return new GameProfile();
        }

        var data = Json.ParseString(file.GetAsText()).AsGodotDictionary();

        return new GameProfile
        {
            Gold = Read(data, "gold"),
            MissionsCompleted = Read(data, "missions"),
            HealthLevel = Read(data, "health"),
            ReloadLevel = Read(data, "reload"),
            AmmoLevel = Read(data, "ammo"),
        };
    }

    private static int Read(Godot.Collections.Dictionary data, string key)
    {
        return data.TryGetValue(key, out Variant value) ? (int)value.AsDouble() : 0;
    }
}
