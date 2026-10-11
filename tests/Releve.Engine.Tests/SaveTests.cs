using System.Security.Cryptography;
using System.Text.Json;
using Releve.Engine.Core;
using Releve.Engine.Persistence;
using Releve.Engine.Tests.World;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Sauvegarde chiffree : aller-retour, version inconnue, fichier corrompu (§11).</summary>
public class SaveTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;
    private readonly EncryptedFileSaveStore _store;

    public SaveTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        _filePath = Path.Combine(_directory, "save.dat");
        _store = new EncryptedFileSaveStore(_filePath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }
        catch (IOException)
        {
            // nettoyage best-effort
        }
    }

    [Fact]
    public void Save_Then_Load_Restores_The_Exact_State()
    {
        var world = TestWorldFactory.Create();
        var state = world.CreateInitialState();
        var engine = new GameEngine(world, state);
        engine.Start();

        engine.Execute("prends clef");
        engine.Execute("utilise clef sur armoire");
        engine.Execute("nord");

        _store.Save(state);

        Assert.True(_store.Exists());
        Assert.False(File.Exists(_filePath + ".tmp"));

        var loaded = _store.Load();
        Assert.NotNull(loaded);

        Assert.Equal(RoomId.TestRoomVault, loaded!.CurrentRoom);
        Assert.Equal(ObjectState.Unlocked, loaded.StateOf(ObjectId.TestCabinet));
        Assert.True(loaded.IsInInventory(ObjectId.TestKey));

        // Le moteur recharge repart du meme etat.
        var restored = new GameEngine(world, loaded);
        var messages = restored.Start();
        Assert.Single(messages);
        Assert.Equal(ExitState.Open, restored.GetView().Exits[Direction.South]);
    }

    [Fact]
    public void Serialization_Uses_Enum_Names_And_Expected_Keys()
    {
        var world = TestWorldFactory.Create();
        var state = world.CreateInitialState();
        var json = JsonSerializer.Deserialize<JsonElement>(SaveSerializer.ToJson(state));

        Assert.Equal(1, json.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("TestRoomStart", json.GetProperty("currentRoom").GetString());
        Assert.True(json.TryGetProperty("locations", out var locations));
        Assert.Equal("Room", locations.GetProperty("TestKey").GetProperty("kind").GetString());
        Assert.True(json.TryGetProperty("states", out var states));
        Assert.Equal("Locked", states.GetProperty("TestCabinet").GetString());
        Assert.Equal(JsonValueKind.Array, json.GetProperty("events").ValueKind);
    }

    [Fact]
    public void Corrupted_File_Is_Ignored()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllBytes(_filePath, RandomNumberGenerator.GetBytes(64));

        Assert.True(_store.Exists());
        Assert.Null(_store.Load());
    }

    [Fact]
    public void Unknown_Schema_Version_Is_Ignored()
    {
        Directory.CreateDirectory(_directory);
        var data = new SaveData { SchemaVersion = 99, CurrentRoom = RoomId.Bedroom };
        var json = JsonSerializer.SerializeToUtf8Bytes(data, SaveSerializer.Options);
        File.WriteAllBytes(_filePath, SaveCrypto.Encrypt(json));

        Assert.Null(_store.Load());
    }

    [Fact]
    public void Missing_File_Loads_As_Null()
    {
        Assert.False(_store.Exists());
        Assert.Null(_store.Load());
    }

    [Fact]
    public void Unreadable_Json_Is_Ignored()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllBytes(_filePath, SaveCrypto.Encrypt(new byte[] { 0x28, 0x28, 0x28 }));
        Assert.Null(_store.Load());
    }
}
