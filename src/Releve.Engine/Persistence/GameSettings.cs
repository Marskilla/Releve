using System.Text.Json.Serialization;

namespace Releve.Engine.Persistence;

/// <summary>Réglages utilisateur : musique et plein écran.</summary>
public sealed record GameSettings(bool Music = true, bool Fullscreen = false);