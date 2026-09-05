using Godot;
using DeadKillers.Components;
using DeadKillers.Weapons;

namespace DeadKillers.Player;

/// <summary>
/// Movimiento del jugador con WASD relativo a la cámara y apuntado al cursor.
/// Cuerpo y mirada son independientes: se puede retroceder mirando hacia delante.
///
/// De las armas no sabe nada: traduce la entrada y se la pasa al WeaponHolder.
/// </summary>
public partial class PlayerController : CharacterBody3D
{
	// Velocidad de desplazamiento en m/s (docs/BALANCE.md).
	[Export] public float MoveSpeed { get; set; } = 5.5f;

	// Altura sobre los pies del plano contra el que se resuelve el apuntado.
	// A la altura del pecho, tal como pide docs/DESIGN.md.
	[Export] public float AimHeight { get; set; } = 1.2f;

	[Export] public HealthComponent Health { get; set; }

	[Export] public WeaponHolder Weapons { get; set; }

	// Cuánto sobrevive un clic sin poder ejecutarse. Un clic rápido dura menos que un
	// fotograma de física, así que sin este margen se perdería.
	[Export] public float AttackBufferTime { get; set; } = 0.15f;

	// Margen mínimo para no llamar a LookAt sobre la propia posición:
	// Godot emite un error si el objetivo coincide con el origen.
	private const float MinAimDistance = 0.01f;

	private float _gravity;
	private float _bufferedAttack;
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

	/// <summary>
	/// Los ataques y los cambios de arma se recogen como EVENTOS, no sondeando. Sondear
	/// con Input.IsActionPressed dentro de _PhysicsProcess pierde las pulsaciones más
	/// cortas que un fotograma, y la acción desaparece sin dar ningún error. Con la rueda
	/// del ratón es peor: no tiene estado "pulsado" que sondear, solo eventos sueltos.
	/// </summary>
	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isDead)
		{
			return;
		}

		if (@event.IsActionPressed("attack_primary"))
		{
			_bufferedAttack = AttackBufferTime;
			return;
		}

		if (Weapons == null)
		{
			return;
		}

		if (@event.IsActionPressed("reload"))
		{
			Weapons.TryReload();
		}
		else if (@event.IsActionPressed("weapon_next"))
		{
			Weapons.Next();
		}
		else if (@event.IsActionPressed("weapon_prev"))
		{
			Weapons.Previous();
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

	private void UpdateAttack(double delta)
	{
		if (_bufferedAttack > 0.0f)
		{
			_bufferedAttack -= (float)delta;
		}

		// Mantener pulsado encadena ataques; un clic suelto entra por el búfer.
		bool wantsToAttack = _bufferedAttack > 0.0f || Input.IsActionPressed("attack_primary");

		if (Weapons == null || !wantsToAttack)
		{
			return;
		}

		if (Weapons.TryFire())
		{
			_bufferedAttack = 0.0f;
		}
	}

	private void OnDied()
	{
		_isDead = true;
		_bufferedAttack = 0.0f;

		Weapons?.Holster();
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
