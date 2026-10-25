using System.IO;
using System.Text.Json;
using Releve.Engine.Persistence;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Tests pour SettingsStore (J5-7).</summary>
public class SettingsTests
{
    [Fact]
    public void Missing_File_Returns_Defaults()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var filePath = Path.Combine(tempDir, "settings.json");

        var store = new SettingsStore(filePath);
        var settings = store.Load();

        Assert.True(settings.Music);
        Assert.False(settings.Fullscreen);
    }

    [Fact]
    public void Roundtrip_Save_Load_Restores_Values()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var filePath = Path.Combine(tempDir, "settings.json");

        var store = new SettingsStore(filePath);
        var original = new GameSettings(Music: false, Fullscreen: true);
        store.Save(original);

        var loaded = store.Load();

        Assert.False(loaded.Music);
        Assert.True(loaded.Fullscreen);
    }

    [Fact]
    public void Invalid_Json_Returns_Defaults_No_Exception()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var filePath = Path.Combine(tempDir, "settings.json");

        Directory.CreateDirectory(tempDir);
        File.WriteAllText(filePath, "{ invalid json }");

        var store = new SettingsStore(filePath);
        var settings = store.Load();

        Assert.True(settings.Music);
        Assert.False(settings.Fullscreen);
    }

    [Fact]
    public void Unknown_Field_Is_Ignored_Known_Fields_Read()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var filePath = Path.Combine(tempDir, "settings.json");

        Directory.CreateDirectory(tempDir);
        var json = JsonSerializer.Serialize(new { music = false, fullscreen = true, unknownField = "ignored" }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(filePath, json);

        var store = new SettingsStore(filePath);
        var settings = store.Load();

        Assert.False(settings.Music);
        Assert.True(settings.Fullscreen);
    }

    [Fact]
    public void Save_In_Nonexistent_Directory_Creates_Directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var filePath = Path.Combine(tempDir, "settings.json");

        var store = new SettingsStore(filePath);
        var settings = new GameSettings(Music: false, Fullscreen: false);
        store.Save(settings);

        Assert.True(Directory.Exists(tempDir));
        Assert.True(File.Exists(filePath));
    }
}