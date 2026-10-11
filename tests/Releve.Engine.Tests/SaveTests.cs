using System.Security.Cryptography;
using System.Text.Json;
using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Persistence;
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
        var world = BootstrapWorld.Create();
        var state = world.CreateInitialState();
        var engine = new GameEngine(world, state);
        engine.Start();

        engine.Execute("prends caisse");
        engine.Execute("utilise caisse sur commutateur");
        engine.Execute("est");

        _store.Save(state);

        Assert.True(_store.Exists());
        Assert.False(File.Exists(_filePath + ".tmp"));

        var loaded = _store.Load();
        Assert.NotNull(loaded);

        Assert.Equal(RoomId.SandboxB, loaded!.CurrentRoom);
        Assert.Equal(ObjectState.Unlocked, loaded.StateOf(ObjectId.SandboxSwitch));
        Assert.True(loaded.IsInInventory(ObjectId.SandboxItem));
        Assert.Contains(GameEvent.SandboxSwitchUnlocked, loaded.Events);
        Assert.Contains(GameEvent.SandboxAnnouncePlayed, loaded.Events);

        // Le moteur recharge repart du meme etat, sans rejouer l'annonce.
        var restored = new GameEngine(world, loaded);
            var messages = restored.Start();
            Assert.DoesNotContain(messages, m => m.Contains("déclic", StringComparison.Ordinal));
        Assert.Equal(ExitState.Open, restored.GetView().Exits[Direction.West]);
    }

    [Fact]
    public void Serialization_Uses_Enum_Names_And_Expected_Keys()
    {
        var world = BootstrapWorld.Create();
        var state = world.CreateInitialState();
        var json = JsonSerializer.Deserialize<JsonElement>(SaveSerializer.ToJson(state));

        Assert.Equal(1, json.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("SandboxA", json.GetProperty("currentRoom").GetString());
        Assert.True(json.TryGetProperty("locations", out var locations));
        Assert.Equal("Room", locations.GetProperty("SandboxItem").GetProperty("kind").GetString());
        Assert.True(json.TryGetProperty("states", out var states));
        Assert.Equal("Locked", states.GetProperty("SandboxSwitch").GetString());
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
        var data = new SaveData { SchemaVersion = 99, CurrentRoom = RoomId.SandboxB };
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
