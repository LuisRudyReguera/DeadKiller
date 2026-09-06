using Godot;

namespace DeadKillers.Components;

/// <summary>Transforma solo el modelo, siempre desde su postura local de reposo.</summary>
[GlobalClass]
public partial class ProceduralAnimator : Node
{
    [Export] public Node3D Body { get; set; }
    [Export] public HealthComponent Health { get; set; }
    [Export] public float ReferenceSpeed { get; set; } = 5.5f;
    [Export] public float StepFrequency { get; set; } = 2.1f;
    [Export] public float BobAmplitude { get; set; } = 0.055f;
    [Export] public float BobCyclesPerStep { get; set; } = 2.0f;
    [Export] public float LeanDegrees { get; set; } = 7.0f;
    [Export] public float SwayDegrees { get; set; } = 4.0f;
    [Export] public float SwayCyclesPerStep { get; set; } = 0.5f;
    [Export] public float SwayPhaseDegrees { get; set; } = 90.0f;
    [Export] public float PrepareLeanDegrees { get; set; } = -12.0f;
    [Export] public Vector3 PrepareScale { get; set; } = new(1.05f, 0.92f, 1.05f);
    [Export] public float PrepareDuration { get; set; } = 0.4f;
    [Export] public float PrepareReleaseTime { get; set; } = 0.08f;
    [Export] public float AttackRiseTime { get; set; } = 0.08f;
    [Export] public float AttackReturnTime { get; set; } = 0.2f;
    [Export] public float AttackLeanDegrees { get; set; } = 22.0f;
    [Export] public float AttackVerticalScale { get; set; } = 1.08f;
    [Export] public float HitDuration { get; set; } = 0.15f;
    [Export] public float HitRecoilDegrees { get; set; } = 6.0f;
    [Export] public float DeathDuration { get; set; } = 0.5f;
    [Export] public float DeathRollDegrees { get; set; } = 82.0f;
    [Export] public float DeathDrop { get; set; } = 0.35f;

    private CharacterBody3D _actor;
    private Transform3D _rest;
    private Transform3D _deathStart;
    private float _phase;
    private float _prepare;
    private bool _preparing;
    private float _attackElapsed;
    private bool _attacking;
    private float _hitElapsed;
    private bool _hit;
    private float _deathElapsed;
    private bool _dead;
    private int _lastHealth;

    public override void _Ready()
    {
        _actor = GetParent() as CharacterBody3D;
        if (_actor == null || Body == null || Body == _actor)
        {
            GD.PushWarning($"{GetPath()}: asigna un modelo visual y un padre CharacterBody3D.");
            SetProcess(false);
            return;
        }
        _rest = Body.Transform;
        if (Health != null)
        {
            _lastHealth = Health.Current;
            Health.HealthChanged += OnHealthChanged;
            Health.Died += OnDied;
        }
    }

    public void SetPreparing(bool active)
    {
        if (_dead) return;
        _preparing = active;
        if (active)
        {
            _prepare = 0;
            _attacking = false;
        }
    }

    public void PlayAttack()
    {
        if (_dead) return;
        _preparing = false;
        _attacking = true;
        _attackElapsed = 0;
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(Body) || !IsInstanceValid(_actor)) return;
        float step = (float)delta;
        if (_dead)
        {
            _deathElapsed += step;
            float progress = Progress(_deathElapsed, DeathDuration);
            var fallen = new Transform3D(
                new Basis(Vector3.Forward, Mathf.DegToRad(DeathRollDegrees)) * _rest.Basis,
                _rest.Origin + Vector3.Down * DeathDrop);
            Body.Transform = _deathStart.InterpolateWith(fallen, Ease(progress));
            if (progress >= 1) { Body.Transform = fallen; SetProcess(false); }
            return;
        }

        float speed = new Vector2(_actor.Velocity.X, _actor.Velocity.Z).Length();
        float intensity = ReferenceSpeed > 0 ? Mathf.Clamp(speed / ReferenceSpeed, 0, 1) : 0;
        if (intensity > 0) _phase += step * StepFrequency * intensity * Mathf.Tau;
        else _phase = 0;

        float prepareStep = _preparing ? PrepareDuration : PrepareReleaseTime;
        _prepare = Mathf.MoveToward(_prepare, _preparing ? 1 : 0,
            prepareStep > 0 ? step / prepareStep : 1);

        float attack = 0;
        if (_attacking)
        {
            _attackElapsed += step;
            attack = _attackElapsed < AttackRiseTime
                ? Ease(Progress(_attackElapsed, AttackRiseTime))
                : 1 - Ease(Progress(_attackElapsed - AttackRiseTime, AttackReturnTime));
            if (_attackElapsed >= AttackRiseTime + AttackReturnTime) _attacking = false;
        }
        float recoil = 0;
        if (_hit)
        {
            _hitElapsed += step;
            float progress = Progress(_hitElapsed, HitDuration);
            recoil = Mathf.Sin(progress * Mathf.Pi) * HitRecoilDegrees;
            if (progress >= 1) _hit = false;
        }

        float locomotion = intensity * (1 - _prepare) * (1 - attack);
        float bob = Mathf.Sin(_phase * BobCyclesPerStep) * BobAmplitude * locomotion;
        float sway = Mathf.Sin(_phase * SwayCyclesPerStep + Mathf.DegToRad(SwayPhaseDegrees)) * SwayDegrees * locomotion;
        float lean = LeanDegrees * locomotion + PrepareLeanDegrees * _prepare + AttackLeanDegrees * attack - recoil;
        Vector3 scale = Vector3.One.Lerp(PrepareScale, _prepare);
        scale.Y *= Mathf.Lerp(1, AttackVerticalScale, attack);
        var rotation = new Basis(Vector3.Right, -Mathf.DegToRad(lean)) *
                       new Basis(Vector3.Forward, Mathf.DegToRad(sway));
        Body.Transform = new Transform3D(rotation * _rest.Basis.ScaledLocal(scale), _rest.Origin + Vector3.Up * bob);
    }

    private static float Progress(float elapsed, float duration) => duration > 0 ? Mathf.Clamp(elapsed / duration, 0, 1) : 1;
    private static float Ease(float progress) => Mathf.SmoothStep(0, 1, progress);

    private void OnHealthChanged(int current, int maximum)
    {
        if (current < _lastHealth && current > 0 && !_dead) { _hit = true; _hitElapsed = 0; }
        _lastHealth = current;
    }

    private void OnDied()
    {
        if (_dead || !IsInstanceValid(Body)) return;
        _dead = true;
        _deathStart = Body.Transform;
        _deathElapsed = 0;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(Health))
        {
            Health.HealthChanged -= OnHealthChanged;
            Health.Died -= OnDied;
        }
        Body = null;
        Health = null;
        _actor = null;
    }
}
