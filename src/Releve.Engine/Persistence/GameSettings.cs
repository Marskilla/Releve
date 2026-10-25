using System.Text.Json.Serialization;

namespace Releve.Engine.Persistence;

/// <summary>Réglages utilisateur (J5-5, J5-6).</summary>
public sealed record GameSettings(bool Music = true, bool Fullscreen = false);