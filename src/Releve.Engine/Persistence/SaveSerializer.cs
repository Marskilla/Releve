using System.Text.Json;
using System.Text.Json.Serialization;
using Releve.Engine.Core;
using Releve.Engine.World;

namespace Releve.Engine.Persistence;

/// <summary>
/// Serialisation JSON de l'etat. Les enums sont ecrits par leur nom, jamais par leur valeur numerique.
/// </summary>
public static class SaveSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    public static byte[] ToJson(GameState state)
    {
        var data = new SaveData
        {
            SchemaVersion = SaveData.CurrentSchemaVersion,
            CurrentRoom = state.CurrentRoom,
        };

        foreach (var (id, location) in state.Locations)
        {
            data.Locations[id] = new SaveLocation { Kind = location.Kind, Room = location.Room };
        }

        foreach (var (id, objectState) in state.States)
        {
            data.States[id] = objectState;
        }

        data.Events.AddRange(state.Events);

        return JsonSerializer.SerializeToUtf8Bytes(data, Options);
    }

    public static SaveData? ToSaveData(byte[] json)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveData>(json, Options);
        }
        catch (JsonException)
        {
            return null; // fichier illisible : ignore proprement
        }
    }

    public static GameState ToState(SaveData data)
    {
        var state = new GameState { CurrentRoom = data.CurrentRoom };

        foreach (var (id, location) in data.Locations)
        {
            state.Locations[id] = new Location(location.Kind, location.Room);
        }

        foreach (var (id, objectState) in data.States)
        {
            state.States[id] = objectState;
        }

        foreach (var gameEvent in data.Events)
        {
            state.Events.Add(gameEvent);
        }

        return state;
    }
}
