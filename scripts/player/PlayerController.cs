using Godot;

namespace DeadKillers.Player;

/// <summary>
/// Movimiento del jugador con WASD relativo a la cámara y apuntado al cursor.
/// Cuerpo y mirada son independientes: se puede retroceder mirando hacia delante.
/// </summary>
public partial class PlayerController : CharacterBody3D
{
	// Velocidad de desplazamiento en m/s (docs/BALANCE.md).
	[Export] public float MoveSpeed { get; set; } = 5.5f;

	// Altura sobre los pies del plano contra el que se resuelve el apuntado.
	// A la altura del pecho, tal como pide docs/DESIGN.md.
	[Export] public float AimHeight { get; set; } = 1.2f;

	// Margen mínimo para no llamar a LookAt sobre la propia posición:
	// Godot emite un error si el objetivo coincide con el origen.
	private const float MinAimDistance = 0.01f;

	private float _gravity;
	private Camera3D _camera;

	public override void _Ready()
	{
		// La gravedad sale de los ajustes del proyecto, no de una constante en el código.
		_gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	}

	public override void _PhysicsProcess(double delta)
	{
		ApplyGravity(delta);
		ApplyMovement();
		AimAtCursor();
		MoveAndSlide();
	}

	private void ApplyGravity(double delta)
	{
		if (IsOnFloor())
		{
			return;
		}

		Velocity += Vector3.Down * _gravity * (float)delta;
	}

	/// <summary>
	/// Traduce el WASD a direcciones del mundo usando la orientación de la cámara,
	/// no la del personaje: pulsar W siempre mueve hacia arriba de la pantalla.
	/// </summary>
	private void ApplyMovement()
	{
		Camera3D camera = ResolveCamera();
		if (camera == null)
		{
			return;
		}

		Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");

		// Se aplanan los ejes de la cámara para que la inclinación de 60° no
		// meta componente vertical en el movimiento.
		Vector3 forward = -camera.GlobalBasis.Z with { Y = 0 };
		Vector3 right = camera.GlobalBasis.X with { Y = 0 };
		Vector3 direction = (right.Normalized() * input.X - forward.Normalized() * input.Y).Normalized();

		Velocity = new Vector3(direction.X * MoveSpeed, Velocity.Y, direction.Z * MoveSpeed);
	}

	/// <summary>
	/// Lanza un rayo desde la cámara a través del cursor y lo corta contra un plano
	/// horizontal a la altura del pecho. El personaje mira a ese punto.
	/// </summary>
	private void AimAtCursor()
	{
		Camera3D camera = ResolveCamera();
		if (camera == null)
		{
			return;
		}

		Vector2 cursor = GetViewport().GetMousePosition();
		Vector3 rayOrigin = camera.ProjectRayOrigin(cursor);
		Vector3 rayDirection = camera.ProjectRayNormal(cursor);

		var aimPlane = new Plane(Vector3.Up, GlobalPosition.Y + AimHeight);
		Vector3? hit = aimPlane.IntersectsRay(rayOrigin, rayDirection);
		if (hit == null)
		{
			return;
		}

		// Se descarta la altura del impacto: el personaje gira solo sobre su eje vertical.
		var target = new Vector3(hit.Value.X, GlobalPosition.Y, hit.Value.Z);
		if (GlobalPosition.DistanceSquaredTo(target) < MinAimDistance * MinAimDistance)
		{
			return;
		}

		LookAt(target, Vector3.Up);
	}

	/// <summary>
	/// La cámara activa no existe todavía cuando el jugador ejecuta _Ready si el nodo
	/// va antes en el árbol, así que se resuelve la primera vez que hace falta.
	/// </summary>
	private Camera3D ResolveCamera()
	{
		return _camera ??= GetViewport().GetCamera3D();
	}
}
