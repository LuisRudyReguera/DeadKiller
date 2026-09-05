using Godot;

namespace DeadKillers.Tools;

/// <summary>
/// Herramienta de desarrollo: hace capturas del juego en marcha y cierra al terminar.
/// Sirve para poder enseñar cómo se ve sin tener que estar delante del ordenador.
///
/// No forma parte del juego. Se cuelga de una escena a mano cuando hace falta.
/// </summary>
public partial class ScreenshotTaker : Node
{
    // Segundos, desde el arranque, en los que dispara cada captura.
    [Export] public float[] Moments { get; set; } = { 1.2f, 3.0f, 5.0f, 7.5f };

    [Export] public string Prefix { get; set; } = "shot";

    // Margen tras la última captura antes de cerrar, para que termine de escribir.
    [Export] public float QuitDelay { get; set; } = 0.6f;

    private double _elapsed;
    private int _next;
    private double _doneAt = -1.0;

    public override void _Process(double delta)
    {
        _elapsed += delta;

        if (_doneAt >= 0.0)
        {
            if (_elapsed >= _doneAt)
            {
                GetTree().Quit();
            }

            return;
        }

        if (_next >= Moments.Length)
        {
            _doneAt = _elapsed + QuitDelay;
            return;
        }

        if (_elapsed < Moments[_next])
        {
            return;
        }

        Capture(_next + 1);
        _next++;
    }

    private void Capture(int number)
    {
        Image image = GetViewport().GetTexture()?.GetImage();

        if (image == null)
        {
            GD.PushWarning($"{Name}: el viewport no ha devuelto imagen; ¿se está ejecutando sin ventana?");
            return;
        }

        string path = $"user://{Prefix}_{number}.png";
        Error result = image.SavePng(path);

        GD.Print(result == Error.Ok
            ? $"[CAPTURA] {number} -> {ProjectSettings.GlobalizePath(path)}"
            : $"[CAPTURA] fallo al guardar {number}: {result}");
    }
}
