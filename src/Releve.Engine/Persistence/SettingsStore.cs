using System.Text.Json;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Persistence;

/// <summary>
/// Stockage des reglages utilisateur (J5-6).
/// Fichier JSON non chiffre, ecriture atomique, dossier cree au besoin.
/// Exceptions d'E/S avalees.
/// </summary>
public sealed class SettingsStore
{
    private readonly string _filePath;

    public SettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? DefaultPath();
    }

    public static string DefaultPath()
    {
        var saveDir = Path.GetDirectoryName(EncryptedFileSaveStore.DefaultPath());
        if (string.IsNullOrEmpty(saveDir))
        {
            return string.Empty;
        }
        return Path.Combine(saveDir, "settings.json");
    }

    public GameSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new GameSettings();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<GameSettings>(json, JsonOptions);
            return settings ?? new GameSettings();
        }
        catch (Exception)
        {
            return new GameSettings();
        }
    }

    public void Save(GameSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var temporary = _filePath + ".tmp";
            File.WriteAllText(temporary, json);
            File.Move(temporary, _filePath, overwrite: true);
        }
        catch (Exception)
        {
            // E/S avalee
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}