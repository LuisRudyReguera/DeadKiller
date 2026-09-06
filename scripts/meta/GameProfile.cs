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

    private static bool IsValidUpgrade(UpgradeKind kind) =>
        kind is UpgradeKind.Health or UpgradeKind.Reload or UpgradeKind.Ammo;

    public bool CanBuy(UpgradeKind kind) =>
        IsValidUpgrade(kind) && LevelOf(kind) >= 0 &&
        LevelOf(kind) < MaxUpgradeLevel && Gold >= PriceOf(kind);

    /// <summary>
    /// Cada nivel cuesta más que el anterior. Los precios están en docs/BALANCE.md.
    /// </summary>
    public int PriceOf(UpgradeKind kind)
    {
        int level = LevelOf(kind);

        if (!IsValidUpgrade(kind) || level < 0 || level >= MaxUpgradeLevel)
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
            case UpgradeKind.Ammo:
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

        return FromJson(file.GetAsText());
    }

    // Separado del disco para verificar perfiles dañados sin tocar la partida real.
    internal static GameProfile FromJson(string text)
    {
        using var json = new Json();
        if (json.Parse(text) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
        {
            return new GameProfile();
        }

        var data = json.Data.AsGodotDictionary();

        return new GameProfile
        {
            Gold = Read(data, "gold"),
            MissionsCompleted = Read(data, "missions"),
            HealthLevel = Read(data, "health", MaxUpgradeLevel),
            ReloadLevel = Read(data, "reload", MaxUpgradeLevel),
            AmmoLevel = Read(data, "ammo", MaxUpgradeLevel),
        };
    }

    private static int Read(Godot.Collections.Dictionary data, string key, int maximum = int.MaxValue)
    {
        if (!data.TryGetValue(key, out Variant value) ||
            value.VariantType is not (Variant.Type.Int or Variant.Type.Float))
        {
            return 0;
        }

        double number = value.AsDouble();
        if (!double.IsFinite(number) || number < 0 || number != System.Math.Truncate(number))
        {
            return 0;
        }

        return (int)System.Math.Min(number, maximum);
    }
}
