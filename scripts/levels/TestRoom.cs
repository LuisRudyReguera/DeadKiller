using Godot;
using DeadKillers.Components;

namespace DeadKillers.Levels;

/// <summary>
/// Cierra el bucle mínimo del hito 1: matar o morir. Reinicia la escena cuando el
/// jugador cae o cuando ya no queda ningún enemigo vivo.
/// </summary>
public partial class TestRoom : Node3D
{
    // Pausa antes de reiniciar, para que dé tiempo a ver qué ha pasado.
    [Export] public float RestartDelay { get; set; } = 1.2f;

    private float _countdown = -1.0f;
    private int _enemiesAlive;

    public override void _Ready()
    {
        Node player = GetTree().GetFirstNodeInGroup(Groups.Player);
        HealthComponent playerHealth = HealthComponent.FindIn(player);

        if (playerHealth == null)
        {
            GD.PushWarning($"{Name}: el jugador no tiene HealthComponent; no se podrá perder.");
        }
        else
        {
            playerHealth.Died += StartRestart;
        }

        foreach (Node enemy in GetTree().GetNodesInGroup(Groups.Enemy))
        {
            HealthComponent health = HealthComponent.FindIn(enemy);
            if (health == null)
            {
                continue;
            }

            _enemiesAlive++;
            health.Died += OnEnemyDied;
        }
    }

    public override void _Process(double delta)
    {
        if (_countdown < 0.0f)
        {
            return;
        }

        _countdown -= (float)delta;
        if (_countdown > 0.0f)
        {
            return;
        }

        _countdown = -1.0f;

        // Diferido: recargar el árbol desde dentro de su propio procesado no es seguro.
        GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
    }

    private void OnEnemyDied()
    {
        _enemiesAlive--;

        if (_enemiesAlive <= 0)
        {
            StartRestart();
        }
    }

    private void StartRestart()
    {
        // La primera muerte manda: si caen los dos a la vez, no se reinicia dos veces.
        if (_countdown >= 0.0f)
        {
            return;
        }

        _countdown = RestartDelay;
    }
}
