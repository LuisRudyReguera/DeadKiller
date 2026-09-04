using Godot;

namespace DeadKillers.Player;

/// <summary>
/// Sigue al objetivo con suavizado. La cámara cuelga de este nodo con su inclinación
/// y su distancia ya fijadas, de modo que el ángulo cenital nunca cambia: este nodo
/// solo se ocupa de la posición.
/// </summary>
public partial class CameraRig : Node3D
{
	// Nodo al que sigue la cámara. Se asigna arrastrándolo en el inspector.
	[Export] public Node3D Target { get; set; }

	// Cuanto más alto, más pegada va la cámara al objetivo. Provisional, a ajustar jugando.
	[Export] public float FollowSmoothing { get; set; } = 8.0f;

	public override void _Ready()
	{
		if (Target == null)
		{
			GD.PushWarning($"{Name}: falta asignar Target en el inspector; la cámara no seguirá a nadie.");
			return;
		}

		// Arranca ya encima del objetivo para que el primer fotograma no muestre un barrido.
		GlobalPosition = Target.GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Target == null)
		{
			return;
		}

		// Suavizado exponencial: el resultado no depende de los fotogramas por segundo,
		// que es lo que falla si se interpola con un factor fijo.
		float weight = 1.0f - Mathf.Exp(-FollowSmoothing * (float)delta);
		GlobalPosition = GlobalPosition.Lerp(Target.GlobalPosition, weight);
	}
}
