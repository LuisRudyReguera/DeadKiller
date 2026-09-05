using Godot;
using Godot.Collections;

namespace DeadKillers.Level;

/// <summary>
/// Suelta enemigos donde estén sus hijos marcadores. Sirve para emboscadas: se pone una
/// zona en el pasillo y esto detrás de la puerta.
///
/// Si no tiene marcadores, los suelta en su propia posición.
/// </summary>
[GlobalClass]
public partial class EnemySpawner : LevelResponder
{
    [Signal]
    public delegate void SpawnedEventHandler(Node3D enemy);

    [Export] public PackedScene EnemyScene { get; set; }

    // Cuántos por marcador. Con 1 y tres marcadores salen tres.
    [Export] public int PerPoint { get; set; } = 1;

    // Separación entre los que salen del mismo marcador, para que no nazcan encajados.
    [Export] public float Spread { get; set; } = 1.2f;

    // Aparecer al arrancar el nivel en vez de esperar a que algo lo dispare.
    [Export] public bool OnStart { get; set; }

    public override void _Ready()
    {
        if (EnemyScene == null)
        {
            GD.PushWarning($"{Name}: sin escena de enemigo asignada; no soltará nada.");
            return;
        }

        if (OnStart)
        {
            // Diferido: al arrancar, el nivel todavía se está montando.
            CallDeferred(MethodName.Activate);
        }
    }

    public override void _ExitTree()
    {
        EnemyScene = null;   // ver D-011
    }

    protected override void OnActivate()
    {
        if (EnemyScene == null)
        {
            return;
        }

        Array<Node3D> points = SpawnPoints();

        foreach (Node3D point in points)
        {
            for (int i = 0; i < Mathf.Max(PerPoint, 1); i++)
            {
                SpawnOne(point.GlobalPosition, i);
            }
        }
    }

    /// <summary>Los hijos Node3D marcan dónde aparecen. Sin hijos, aparece aquí mismo.</summary>
    private Array<Node3D> SpawnPoints()
    {
        var points = new Array<Node3D>();

        foreach (Node child in GetChildren())
        {
            if (child is Node3D point)
            {
                points.Add(point);
            }
        }

        if (points.Count == 0)
        {
            points.Add(this);
        }

        return points;
    }

    private void SpawnOne(Vector3 origin, int index)
    {
        if (EnemyScene.Instantiate() is not Node3D enemy)
        {
            GD.PushError($"{Name}: la escena asignada no es un Node3D.");
            return;
        }

        // En círculo alrededor del marcador: dos enemigos en el mismo punto se empujarían.
        float angle = index * Mathf.Tau / Mathf.Max(PerPoint, 1);
        var offset = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * (index == 0 ? 0.0f : Spread);

        Node parent = GetParent() ?? GetTree().CurrentScene;
        parent.AddChild(enemy);
        enemy.GlobalPosition = origin + offset;

        EmitSignal(SignalName.Spawned, enemy);
    }
}
