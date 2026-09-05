using Godot;
using System.Collections.Generic;

namespace DeadKillers.Components;

/// <summary>
/// Los efectos que se pintan ENCIMA del cuerpo: el aviso de ataque y el destello al
/// recibir daño. Los dos van juntos aquí porque los dos usan la capa de superposición
/// y sin un único dueño se pisarían.
///
/// Usa `MaterialOverlay`, nunca `MaterialOverride`: así el modelo conserva su paleta.
/// Prioridad: el destello manda sobre el aviso, porque recibir un golpe es lo más
/// urgente que hay que comunicar.
/// </summary>
[GlobalClass]
public partial class BodyEffects : Node
{
    // Cuerpo visible. Puede ser una malla suelta o un modelo con varias dentro.
    [Export] public Node3D Body { get; set; }

    [Export] public HealthComponent Health { get; set; }

    [Export] public Material FlashMaterial { get; set; }

    // Aviso previo al ataque. Los enemigos lo traen de su `.tres`; el jugador no lo usa.
    [Export] public Material TelegraphMaterial { get; set; }

    [Export] public float FlashDuration { get; set; } = 0.12f;

    private List<MeshInstance3D> _meshes;
    private float _flashLeft;
    private bool _telegraphing;
    private int _lastKnownHealth = int.MaxValue;

    public override void _Ready()
    {
        SetProcess(false);

        _meshes = Meshes.CollectFrom(Body);

        if (_meshes.Count == 0)
        {
            GD.PushWarning($"{GetPath()}: el cuerpo no tiene ninguna malla; no habrá efectos.");
        }

        if (Health != null)
        {
            Health.HealthChanged += OnHealthChanged;
        }
    }

    public override void _ExitTree()
    {
        // Ver D-011: un campo de C# que apunta a un Resource lo mantiene vivo al cerrar.
        if (_meshes != null)
        {
            Meshes.SetOverlay(_meshes, null);
            _meshes = null;
        }

        FlashMaterial = null;
        TelegraphMaterial = null;
    }

    public override void _Process(double delta)
    {
        _flashLeft -= (float)delta;

        if (_flashLeft > 0.0f)
        {
            return;
        }

        _flashLeft = 0.0f;
        SetProcess(false);
        Refresh();
    }

    /// <summary>Enciende o apaga el aviso de ataque.</summary>
    public void SetTelegraph(bool active)
    {
        if (_telegraphing == active)
        {
            return;
        }

        _telegraphing = active;
        Refresh();
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

        _flashLeft = FlashDuration;
        SetProcess(true);
        Refresh();
    }

    private void Refresh()
    {
        if (_meshes == null)
        {
            return;
        }

        Material overlay = _flashLeft > 0.0f
            ? FlashMaterial
            : _telegraphing ? TelegraphMaterial : null;

        Meshes.SetOverlay(_meshes, overlay);
    }
}
