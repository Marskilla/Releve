using Raylib_cs;

namespace Releve.Game.Ui;

/// <summary>
/// Fondu à l'écran (écran noir) pour les transitions (§9.1, J7).
/// États : Idle (inactif), Out (sortie vers le noir), In (entrée depuis le noir).
/// </summary>
public sealed class ScreenFade
{
    public const float OutDuration = 0.6f;
    public const float InDuration = 0.8f;

    public enum State { Idle, Out, In }
    public enum FadeEvent { None, OutFinished, InFinished }

    private State _state = State.Idle;
    private float _timer;

    public State CurrentState => _state;
    public bool IsActive => _state != State.Idle;

    /// <summary>Démarre le fondu de sortie (vers le noir).</summary>
    public void StartOut()
    {
        _state = State.Out;
        _timer = 0f;
    }

    /// <summary>Démarre le fondu d'entrée (depuis le noir).</summary>
    public void StartIn()
    {
        _state = State.In;
        _timer = 0f;
    }

    /// <summary>Met à jour le fondu. Renvoie l'événement de fin si applicable.</summary>
    public FadeEvent Update(float dt)
    {
        if (_state == State.Idle)
        {
            return FadeEvent.None;
        }

        float duration = _state == State.Out ? OutDuration : InDuration;
        _timer += dt;

        if (_timer >= duration)
        {
            _timer = duration;
            if (_state == State.Out)
            {
                _state = State.Idle;
                return FadeEvent.OutFinished;
            }
            else
            {
                _state = State.Idle;
                return FadeEvent.InFinished;
            }
        }

        return FadeEvent.None;
    }

    /// <summary>Dessine le fondu par-dessus tout l'écran.</summary>
    public void Draw()
    {
        if (_state == State.Idle)
        {
            return;
        }

        float duration = _state == State.Out ? OutDuration : InDuration;
        float t = _timer / duration;

        float opacity = _state == State.Out ? t : 1f - t;
        byte alpha = (byte)Math.Clamp(opacity * 255f, 0f, 255f);

        var color = new Color((byte)0, (byte)0, (byte)0, alpha);
        Raylib.DrawRectangle(0, 0, Layout.ScreenWidth, Layout.ScreenHeight, color);
    }
}