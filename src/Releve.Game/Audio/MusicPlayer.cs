using System.IO;
using Raylib_cs;
using Releve.Game.Ui;

namespace Releve.Game.Audio;

/// <summary>
/// Bandes son du jeu : une pour le menu/aide, une pour le jeu (§10 de la note d'intention).
/// Chaque flux est en boucle et mis à jour à chaque image. Fichier absent ou périphérique
/// indisponible : le jeu continue sans son (§9.6).
/// </summary>
public sealed class MusicPlayer : IDisposable
{
    public const string MenuTrack = "menu";
    public const string GameTrack = "game";

    private readonly Dictionary<string, Music> _tracks = new(StringComparer.Ordinal);
    private bool _audioReady;
    private string? _current;

    public bool Enabled => _audioReady;

    public MusicPlayer()
    {
        try
        {
            Raylib.InitAudioDevice();
            _audioReady = Raylib.IsAudioDeviceReady();
        }
        catch (Exception)
        {
            _audioReady = false;
        }
    }

    /// <summary>Charge les pistes présentes ; les autres restent silencieuses.</summary>
    public void LoadTracks()
    {
        foreach (var name in new[] { MenuTrack, GameTrack })
        {
            var path = TrackPath(name);
            if (path is null)
            {
                continue;
            }

            try
            {
                var music = Raylib.LoadMusicStream(path);
                music.Looping = true;
                _tracks[name] = music;
            }
            catch (Exception)
            {
                // piste illisible : ignorée, le jeu continue sans elle
            }
        }
    }

    public void Play(string name)
    {
        if (!_audioReady || _current == name)
        {
            return;
        }

        Stop();

        if (!_tracks.TryGetValue(name, out var music))
        {
            return;
        }

        _current = name;
        Raylib.PlayMusicStream(music);
    }

    public void Stop()
    {
        if (!_audioReady || _current is null)
        {
            return;
        }

        if (_tracks.TryGetValue(_current, out var music))
        {
            Raylib.StopMusicStream(music);
        }

        _current = null;
    }

    /// <summary>À appeler une fois par image pendant la lecture.</summary>
    public void Update()
    {
        if (!_audioReady || _current is null)
        {
            return;
        }

        if (_tracks.TryGetValue(_current, out var music))
        {
            Raylib.UpdateMusicStream(music);
        }
    }

    private static string? TrackPath(string name)
    {
        foreach (var extension in new[] { ".ogg", ".mp3" })
        {
            var candidate = Assets.Combine("audio", name + extension);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public void Dispose()
    {
        foreach (var music in _tracks.Values)
        {
            try
            {
                Raylib.StopMusicStream(music);
                Raylib.UnloadMusicStream(music);
            }
            catch (Exception)
            {
                // libération best-effort
            }
        }

        _tracks.Clear();

        if (_audioReady)
        {
            try
            {
                Raylib.CloseAudioDevice();
            }
            catch (Exception)
            {
                // fermeture best-effort
            }
        }
    }
}
