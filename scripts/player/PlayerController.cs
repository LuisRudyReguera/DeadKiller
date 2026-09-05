using Godot;
using DeadKillers.Components;

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

	// Cadencia de la espada corta en segundos (docs/BALANCE.md).
	// Provisional: en el hito 2 esto sale de un WeaponData y no de aquí.
	[Export] public float AttackCooldown { get; set; } = 0.5f;

	[Export] public HealthComponent Health { get; set; }

	[Export] public HitboxComponent Sword { get; set; }

	[Export] public AudioStreamPlayer3D SwingSound { get; set; }

	[Export] public AudioStreamPlayer3D HitSound { get; set; }

	// Margen mínimo para no llamar a LookAt sobre la propia posición:
	// Godot emite un error si el objetivo coincide con el origen.
	private const float MinAimDistance = 0.01f;

	private float _gravity;
	private float _cooldown;
	private bool _isDead;
	private Camera3D _camera;

	public override void _Ready()
	{
		// La gravedad sale de los ajustes del proyecto, no de una constante en el código.
		_gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

		if (Health != null)
		{
			Health.Died += OnDied;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			// Sigue cayendo si hiciera falta, pero deja de obedecer al teclado.
			Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
			ApplyGravity(delta);
			MoveAndSlide();
			return;
		}

		ApplyGravity(delta);
		ApplyMovement();
		AimAtCursor();
		UpdateAttack(delta);
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
	/// Espadazo. El arco y el alcance viven en el HitboxComponent; aquí solo está
	/// la cadencia, porque es lo que decide cuándo se puede volver a golpear.
	/// </summary>
	private void UpdateAttack(double delta)
	{
		if (_cooldown > 0.0f)
		{
			_cooldown -= (float)delta;
		}

		if (Sword == null || _cooldown > 0.0f || !Input.IsActionPressed("attack_primary"))
		{
			return;
		}

		_cooldown = AttackCooldown;
		SwingSound?.Play();

		if (Sword.Strike() > 0)
		{
			HitSound?.Play();
		}
	}

	private void OnDied()
	{
		_isDead = true;

		// Diferido: la muerte llega desde un Strike() en pleno ciclo de física, y Godot
		// bloquea tocar 'monitoring' dentro de una llamada de colisión.
		Sword?.SetDeferred(Area3D.PropertyName.Monitoring, false);
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
