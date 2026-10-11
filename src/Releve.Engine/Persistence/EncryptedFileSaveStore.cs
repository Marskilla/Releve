using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Persistence;

/// <summary>
/// Sauvegarde chiffree dans un fichier unique sous Documents/Releve/save.dat.
/// Ecriture atomique : fichier temporaire puis remplacement (§8).
/// </summary>
public sealed class EncryptedFileSaveStore : ISaveStore
{
    private readonly string _filePath;

    public EncryptedFileSaveStore(string? filePath = null)
    {
        _filePath = filePath ?? DefaultPath();
    }

    public static string DefaultPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Releve",
        "save.dat");

    public bool Exists() => File.Exists(_filePath);

    public GameState? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            var plain = SaveCrypto.Decrypt(File.ReadAllBytes(_filePath));
            if (plain is null)
            {
                return null; // fichier corrompu ou chiffre avec une autre cle
            }

            var data = SaveSerializer.ToSaveData(plain);
            if (data is null)
            {
                return null;
            }

            // Version de schema : un contenu d'une version inconnue est ignore.
            return data.SchemaVersion == SaveData.CurrentSchemaVersion
                ? SaveSerializer.ToState(data)
                : null;
        }
        catch (Exception)
        {
            return null; // aucune exception ne doit traverser la couche de sauvegarde
        }
    }

    public void Save(GameState state)
    {
        var payload = SaveCrypto.Encrypt(SaveSerializer.ToJson(state));

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporary = _filePath + ".tmp";
        File.WriteAllBytes(temporary, payload);
        File.Move(temporary, _filePath, overwrite: true);
    }
}
