using Godot;

namespace DeadKillers.Level;

/// <summary>
/// Puerta o reja que bloquea el paso hasta que algo la abre: una llave, una zona pisada,
/// o los objetivos de la misión.
///
/// Al abrirse baja hasta hundirse en el suelo y deja de colisionar.
/// </summary>
[GlobalClass]
public partial class Door : LevelResponder
{
    [Export] public CollisionShape3D Blocker { get; set; }

    [Export] public Node3D Leaf { get; set; }

    // Cuánto baja al abrirse. Un poco más que su altura, para que no asome.
    [Export] public float DropDistance { get; set; } = 3.2f;

    [Export] public float OpenSeconds { get; set; } = 0.9f;

    [Export] public AudioStreamPlayer3D Sound { get; set; }

    private float _elapsed = -1.0f;
    private Vector3 _closedAt;

    public override void _Ready()
    {
        if (Leaf != null)
        {
            _closedAt = Leaf.Position;
        }

        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;

        float t = Mathf.Clamp(_elapsed / Mathf.Max(OpenSeconds, 0.01f), 0.0f, 1.0f);

        // Arranca rápido y frena al final: una puerta pesada no se para en seco.
        float eased = 1.0f - Mathf.Pow(1.0f - t, 3.0f);

        if (Leaf != null)
        {
            Leaf.Position = _closedAt + Vector3.Down * DropDistance * eased;
        }

        if (t >= 1.0f)
        {
            SetProcess(false);
        }
    }

    protected override void OnActivate()
    {
        _elapsed = 0.0f;
        SetProcess(true);

        Sound?.Play();

        // Diferido: puede llegar desde una señal de colisión, y ahí Godot bloquea
        // desactivar formas (ver D-011 y las trampas de Godot con C#).
        Blocker?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
    }
}
