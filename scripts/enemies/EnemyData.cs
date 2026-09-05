using Godot;

namespace DeadKillers.Enemies;

/// <summary>
/// Todo lo que distingue a un arquetipo de enemigo, en un `.tres`. Mismo principio que
/// <c>WeaponData</c> (D-004): añadir un tipo nuevo es crear datos, no escribir una clase.
///
/// Los valores son los de docs/BALANCE.md.
/// </summary>
[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string DisplayName { get; set; } = "Sin nombre";

    // --- Vida ---

    [Export] public int MaxHealth { get; set; } = 40;

    // Se resta del daño recibido, con un mínimo de 1. Solo el demonio la tiene.
    [Export] public int FlatArmor { get; set; }

    // --- Movimiento ---

    [Export] public float MoveSpeed { get; set; } = 5.0f;

    // Velocidad del arranque de ataque. Igual a MoveSpeed si el tipo no embiste.
    [Export] public float LungeSpeed { get; set; } = 11.0f;

    // --- Percepción ---

    [Export] public float SightRange { get; set; } = 12.0f;

    // Mayor que SightRange a propósito: si fueran iguales, entraría y saldría de
    // persecución en el borde exacto.
    [Export] public float LoseSightRange { get; set; } = 16.0f;

    // --- Combate ---

    [Export] public int Damage { get; set; } = 15;

    [Export] public bool IgnoresArmor { get; set; }

    // Alcance del golpe.
    [Export] public float AttackRange { get; set; } = 1.5f;

    // Amplitud del golpe, en grados.
    [Export] public float AttackArcDegrees { get; set; } = 120.0f;

    // Distancia a la que deja de acercarse y empieza a prepararse.
    [Export] public float PrepareRange { get; set; } = 2.5f;

    // Segundos entre que empieza a telegrafiar y el golpe conecta. NUNCA puede ser 0:
    // es el margen que tiene el jugador para reaccionar y lo que hace justo al juego.
    [Export] public float AttackWarning { get; set; } = 0.4f;

    [Export] public float AlertDuration { get; set; } = 0.6f;

    [Export] public float LungeDuration { get; set; } = 0.25f;

    // Pausa tras atacar. Es la ventana en la que el jugador puede contraatacar.
    [Export] public float RecoveryDuration { get; set; } = 0.6f;

    // --- Comportamientos opcionales ---
    // Los usan los estados que cada arquetipo tenga en su escena. Un tipo que no lleve
    // el estado correspondiente simplemente los ignora.

    // Cuánto se desplaza de lado mientras se acerca. 0 = va en línea recta.
    [Export] public float StrafeStrength { get; set; }

    // Segundos entre parpadeos. 0 = no parpadea.
    [Export] public float BlinkInterval { get; set; }

    [Export] public float BlinkDistance { get; set; } = 4.0f;

    // --- Presentación ---

    [Export] public Color BodyColor { get; set; } = new(0.55f, 0.11f, 0.11f);

    // Color al que vira mientras avisa. Tiene que leerse de un vistazo desde la cámara.
    [Export] public Color TelegraphColor { get; set; } = new(1.0f, 0.72f, 0.15f);

    // Aviso sonoro propio de cada arquetipo: es criterio de cierre del hito 3 que no
    // suenen todos igual.
    [Export] public AudioStream WarningSound { get; set; }

    // --- Recompensa (se usa a partir del hito 4) ---

    [Export] public int MinGold { get; set; } = 5;

    [Export] public int MaxGold { get; set; } = 10;
}
