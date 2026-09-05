using Godot;
using DeadKillers.Level;

namespace DeadKillers.Missions;

/// <summary>
/// Destrucción: romper nidos, ataúdes o altares. Cuenta los rompibles que hay al empezar
/// y se cumple cuando no queda ninguno.
/// </summary>
[GlobalClass]
public partial class DestroyObjective : Objective
{
    // Si se deja vacío, cuenta TODOS los rompibles del nivel. Con un nombre, solo los
    // que se llamen así: permite tener barriles decorativos y altares que sí cuentan.
    [Export] public string OnlyNamed { get; set; } = string.Empty;

    private int _total;
    private int _remaining;

    public override string ProgressText => IsComplete
        ? $"{Description} — hecho"
        : $"{Description} — quedan {_remaining} de {_total}";

    public override void Setup(Mission mission)
    {
        base.Setup(mission);

        foreach (Node node in GetTree().GetNodesInGroup(Breakable.BreakableGroup))
        {
            if (node is not Breakable breakable)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(OnlyNamed) && !breakable.Name.ToString().Contains(OnlyNamed))
            {
                continue;
            }

            _total++;
            breakable.Broken += OnBroken;
        }

        _remaining = _total;

        if (_total == 0)
        {
            GD.PushWarning($"{GetPath()}: no hay ningún rompible que contar; el objetivo nace cumplido.");
            MarkComplete();
        }
    }

    private void OnBroken()
    {
        _remaining--;
        MarkChanged();

        if (_remaining <= 0)
        {
            MarkComplete();
        }
    }
}
