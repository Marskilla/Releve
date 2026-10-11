using System.Text.Json.Serialization;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Persistence;

/// <summary>Acces a la sauvegarde unique.</summary>
public interface ISaveStore
{
    bool Exists();

    /// <summary>null si absent, illisible ou version inconnue.</summary>
    GameState? Load();

    void Save(GameState state);
}

/// <summary>Contenu sauvegarde : etat uniquement, aucune definition (texte, image, sortie).</summary>
public sealed class SaveData
{
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("currentRoom")]
    public RoomId CurrentRoom { get; set; }

    [JsonPropertyName("locations")]
    public Dictionary<ObjectId, SaveLocation> Locations { get; set; } = new();

    [JsonPropertyName("states")]
    public Dictionary<ObjectId, ObjectState> States { get; set; } = new();

    [JsonPropertyName("events")]
    public List<GameEvent> Events { get; set; } = new();
}

/// <summary>Emplacement d'un objet sauvegarde.</summary>
public sealed class SaveLocation
{
    [JsonPropertyName("kind")]
    public LocationKind Kind { get; set; }

    [JsonPropertyName("room")]
    public RoomId? Room { get; set; }
}
