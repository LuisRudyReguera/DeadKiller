using Godot;

namespace DeadKillers.Components;

/// <summary>
/// Destella la malla cuando su <see cref="HealthComponent"/> pierde vida. Sin esto no
/// hay forma de saber si un golpe ha entrado hasta que el enemigo cae.
///
/// Usa `MaterialOverlay`, no `MaterialOverride`, para no pelearse con quien esté
/// coloreando el material de base — el licántropo lo usa para telegrafiar.
/// </summary>
[GlobalClass]
public partial class HitFlashComponent : Node
{
    [Export] public MeshInstance3D Target { get; set; }

    [Export] public HealthComponent Health { get; set; }

    // Material que se superpone durante el destello. Viene de la escena: crearlo aquí
    // dejaría un recurso vivo al cerrar el juego.
    [Export] public Material FlashMaterial { get; set; }

    [Export] public float Duration { get; set; } = 0.12f;

    private float _remaining;
    private int _lastKnownHealth = int.MaxValue;

    public override void _Ready()
    {
        SetProcess(false);

        if (Health == null || Target == null || FlashMaterial == null)
        {
            GD.PushWarning($"{GetPath()}: faltan referencias; no habrá destello al recibir daño.");
            return;
        }

        Health.HealthChanged += OnHealthChanged;
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining > 0.0f)
        {
            return;
        }

        Target.MaterialOverlay = null;
        SetProcess(false);
    }

    private void OnHealthChanged(int current, int max)
    {
        // La señal también salta al curarse; solo interesa perder vida.
        bool tookDamage = current < _lastKnownHealth;
        _lastKnownHealth = current;

        if (!tookDamage)
        {
            return;
        }

        _remaining = Duration;
        Target.MaterialOverlay = FlashMaterial;
        SetProcess(true);
    }
}
