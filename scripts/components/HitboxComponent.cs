using Godot;

namespace DeadKillers.Components;

/// <summary>
/// Área que reparte daño cuando alguien la dispara con <see cref="Strike"/>. No sabe
/// a quién golpea: busca un <see cref="HealthComponent"/> en lo que tenga dentro.
/// Sirve igual para la espada del jugador y para el mordisco de un enemigo.
/// </summary>
[GlobalClass]
public partial class HitboxComponent : Area3D
{
    [Signal]
    public delegate void HitEventHandler(Node3D target);

    [Export] public int Damage { get; set; } = 10;

    // La maza es el caso que ignora la armadura (docs/BALANCE.md).
    [Export] public bool IgnoresArmor { get; set; }

    // 360 golpea en cualquier dirección; menos, un cono hacia delante (-Z).
    // La espada corta es de 60° (docs/BALANCE.md).
    [Export] public float ArcDegrees { get; set; } = 360.0f;

    private const float MinDirectionLength = 0.0001f;

    /// <summary>
    /// Resuelve el golpe contra quien esté dentro del área en este instante.
    /// Devuelve a cuántos ha alcanzado.
    /// </summary>
    public int Strike()
    {
        int landed = 0;

        foreach (Node3D body in GetOverlappingBodies())
        {
            if (!IsWithinArc(body))
            {
                continue;
            }

            HealthComponent health = HealthComponent.FindIn(body);
            if (health == null || health.IsDead)
            {
                continue;
            }

            health.TakeDamage(Damage, IgnoresArmor);
            EmitSignal(SignalName.Hit, body);
            landed++;
        }

        return landed;
    }

    /// <summary>
    /// El arco se mide en horizontal desde el frente del área. Sin esto, la espada
    /// golpearía también a lo que tuviera a la espalda.
    /// </summary>
    private bool IsWithinArc(Node3D body)
    {
        if (ArcDegrees >= 360.0f)
        {
            return true;
        }

        Vector3 toBody = body.GlobalPosition - GlobalPosition;
        toBody.Y = 0.0f;

        Vector3 forward = -GlobalBasis.Z;
        forward.Y = 0.0f;

        if (toBody.LengthSquared() < MinDirectionLength || forward.LengthSquared() < MinDirectionLength)
        {
            return true;
        }

        float angle = Mathf.RadToDeg(forward.Normalized().AngleTo(toBody.Normalized()));
        return angle <= ArcDegrees * 0.5f;
    }
}
