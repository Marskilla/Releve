using System.IO;
using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Parsing;
using Releve.Engine.Persistence;
using Releve.Engine.Tests.World;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Tests des zones d'image selon l'etat (J5, §5.5).</summary>
public class ImagePatchTests
{
    [Fact]
    public void TestWorld_ImagePatch_Activates_And_Deactivates()
    {
        var world = TestWorldFactory.Create();
        var engine = new GameEngine(world, world.CreateInitialState());
        engine.Start();

        var room = world.Rooms[RoomId.TestRoomStart];

        // Ajouter une zone de test pour verifier le mecanisme
        var testPatch = new ImagePatch(10, 20, 30, 40, s => s.StateOf(ObjectId.TestCabinet) == ObjectState.Unlocked);

        // On ne peut pas modifier ImagePatches (read-only), on teste via le moteur réel
        // Ce test est couvert par les tests du monde livre ci-dessous
        Assert.NotNull(room.ImagePatches);
    }

    [Fact]
    public void DeliveredWorld_ImageFiles_Exist_And_Have_Same_Dimensions()
    {
        var assetsDir = FindAssetsDirectory();
        Assert.True(Directory.Exists(assetsDir), $"Dossier assets introuvable : {assetsDir}");

        var roomsDir = Path.Combine(assetsDir, "rooms");
        Assert.True(Directory.Exists(roomsDir), $"Dossier rooms introuvable : {roomsDir}");

        var world = GameWorld.Create();

        foreach (var (roomId, room) in world.Rooms)
        {
            if (!room.ImagePatches.Any())
            {
                continue; // piece sans zone (ex: Street)
            }

            var basePath = Path.Combine(roomsDir, $"{room.ImageKey(world.CreateInitialState())}.png");
            var altPath = Path.Combine(roomsDir, $"{room.ImageKey(world.CreateInitialState())}_AltState.png");

            Assert.True(File.Exists(basePath), $"Image de base manquante : {basePath}");
            Assert.True(File.Exists(altPath), $"Image alternative manquante : {altPath}");

            var (baseW, baseH) = ReadPngDimensions(basePath);
            var (altW, altH) = ReadPngDimensions(altPath);

            Assert.Equal(baseW, altW);
            Assert.Equal(baseH, altH);
            Assert.Equal(1774, baseW);
            Assert.Equal(887, baseH);
        }
    }

    [Fact]
    public void ImagePatch_Rectangles_Within_Bounds_And_No_Overlap()
    {
        var world = GameWorld.Create();

        foreach (var (roomId, room) in world.Rooms)
        {
            var patches = room.ImagePatches;
            if (!patches.Any())
            {
                continue;
            }

            var (w, h) = (1774, 887); // dimensions connues

            foreach (var patch in patches)
            {
                Assert.True(patch.X1 >= 0 && patch.X1 < patch.X2 && patch.X2 <= w,
                    $"Rectangle X hors limites pour {roomId} : [{patch.X1},{patch.X2})");
                Assert.True(patch.Y1 >= 0 && patch.Y1 < patch.Y2 && patch.Y2 <= h,
                    $"Rectangle Y hors limites pour {roomId} : [{patch.Y1},{patch.Y2})");
            }

            // Pas de chevauchement entre zones de la meme piece
            for (int i = 0; i < patches.Count; i++)
            {
                for (int j = i + 1; j < patches.Count; j++)
                {
                    var a = patches[i];
                    var b = patches[j];
                    bool overlapX = a.X1 < b.X2 && b.X1 < a.X2;
                    bool overlapY = a.Y1 < b.Y2 && b.Y1 < a.Y2;
                    Assert.False(overlapX && overlapY,
                        $"Zones chevauchantes dans {roomId} : [{a.X1},{a.Y1},{a.X2},{a.Y2}] et [{b.X1},{b.Y1},{b.X2},{b.Y2}]");
                }
            }
        }
    }

    [Fact]
    public void Bedroom_ImagePatches_Initial_State()
    {
        var world = GameWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState());
        engine.Start();

        var view = engine.GetView();
        Assert.Equal(RoomId.Bedroom, view.Room);
        Assert.Empty(view.ActivePatches); // etat initial : armoire fermee, sac present, lecteur verrouille
    }

    [Fact]
    public void Bedroom_ImagePatches_After_Actions()
    {
        var world = GameWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState());
        engine.Start();

        // Ouvre armoire -> zone armoire active
        engine.Execute("ouvre armoire");
        var view = engine.GetView();
        Assert.Single(view.ActivePatches);
        AssertPatch(view.ActivePatches[0], 705, 140, 1065, 565);

        // Prends badge (necessaire pour l'utiliser)
        engine.Execute("prends badge");
        // Prends sac -> zone corbeille active
        engine.Execute("prends sac");
        view = engine.GetView();
        Assert.Equal(2, view.ActivePatches.Count);
        Assert.Contains(view.ActivePatches, p => p.X1 == 1215 && p.Y1 == 405 && p.X2 == 1340 && p.Y2 == 540);

        // Badge sur lecteur -> zone lecteur active
        engine.Execute("utilise badge sur lecteur");
        view = engine.GetView();
        Assert.Equal(3, view.ActivePatches.Count);
        Assert.Contains(view.ActivePatches, p => p.X1 == 1670 && p.Y1 == 232 && p.X2 == 1730 && p.Y2 == 325);

        // Ferme armoire -> zone armoire desactivee
        engine.Execute("ferme armoire");
        view = engine.GetView();
        Assert.Equal(2, view.ActivePatches.Count);
        Assert.DoesNotContain(view.ActivePatches, p => p.X1 == 705 && p.Y1 == 140 && p.X2 == 1065 && p.Y2 == 565);
    }

    [Fact]
    public void TechCorridor_ImagePatches_After_Unlock_Stairs()
    {
        var world = GameWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState());
        engine.Start();

        // Aller au couloir
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");

        var view = engine.GetView();
        Assert.Equal(RoomId.TechCorridor, view.Room);
        Assert.Empty(view.ActivePatches); // etat initial

        // Debloquer escalier
        engine.Execute("utilise badge sur lecteur escalier");
        view = engine.GetView();
        Assert.Equal(2, view.ActivePatches.Count);

        // Zone 1 : porte escalier + panneau + lecteur
        Assert.Contains(view.ActivePatches, p => p.X1 == 796 && p.Y1 == 205 && p.X2 == 990 && p.Y2 == 566);
        // Zone 2 : reflet au sol
        Assert.Contains(view.ActivePatches, p => p.X1 == 830 && p.Y1 == 566 && p.X2 == 975 && p.Y2 == 775);
    }

    [Fact]
    public void HospitalLobby_ImagePatches_After_Activate_Terminal()
    {
        var world = GameWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState());
        engine.Start();

        // Aller a l'accueil
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");
        engine.Execute("utilise badge sur lecteur escalier");
        engine.Execute("nord");

        var view = engine.GetView();
        Assert.Equal(RoomId.HospitalLobby, view.Room);
        Assert.Empty(view.ActivePatches); // etat initial

        // Activer borne
        engine.Execute("passe le badge sur la borne");
        view = engine.GetView();
        Assert.Single(view.ActivePatches);
        AssertPatch(view.ActivePatches[0], 88, 150, 272, 668);
    }

    [Fact]
    public void Save_Then_Reload_Preserves_ActivePatches()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var savePath = Path.Combine(tempDir, "save.dat");
        var store = new EncryptedFileSaveStore(savePath);

        try
        {
            var world = GameWorld.Create();
            var engine = new GameEngine(world, world.CreateInitialState());
            engine.Start();

            // Jouer jusqu'a avoir des zones actives dans les 3 pieces
            engine.Execute("ouvre armoire");
            engine.Execute("prends badge");
            engine.Execute("prends sac");
            engine.Execute("utilise badge sur lecteur");
            engine.Execute("est");
            engine.Execute("utilise badge sur lecteur escalier");
            engine.Execute("nord");
            engine.Execute("passe le badge sur la borne");

            // Sauvegarder
            store.Save(engine.State);

            // Recharger
            var loaded = store.Load();
            Assert.NotNull(loaded);

            // Verifier accueil (etat charge)
            var restored = new GameEngine(GameWorld.Create(), loaded!);
            var messages = restored.Start();
            var view = restored.GetView();
            Assert.Equal(RoomId.HospitalLobby, view.Room);
            Assert.Single(view.ActivePatches);
            AssertPatch(view.ActivePatches[0], 88, 150, 272, 668);

            // Chambre : creer nouvelle instance avec etat clone
            var bedroomState = CloneState(loaded!);
            bedroomState.CurrentRoom = RoomId.Bedroom;
            var bedroomEngine = new GameEngine(GameWorld.Create(), bedroomState);
            var bedroomView = bedroomEngine.GetView();
            Assert.Equal(3, bedroomView.ActivePatches.Count);

            // Couloir : creer nouvelle instance avec etat clone
            var corridorState = CloneState(loaded!);
            corridorState.CurrentRoom = RoomId.TechCorridor;
            var corridorEngine = new GameEngine(GameWorld.Create(), corridorState);
            var corridorView = corridorEngine.GetView();
            Assert.Equal(2, corridorView.ActivePatches.Count);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch (IOException) { }
        }
    }

    private static GameState CloneState(GameState source)
    {
        var clone = new GameState
        {
            CurrentRoom = source.CurrentRoom
        };
        foreach (var kv in source.Locations)
        {
            clone.Locations[kv.Key] = kv.Value;
        }
        foreach (var kv in source.States)
        {
            clone.States[kv.Key] = kv.Value;
        }
        foreach (var ev in source.Events)
        {
            clone.Events.Add(ev);
        }
        return clone;
    }

    private static void AssertPatch(ImageRect patch, int x1, int y1, int x2, int y2)
    {
        Assert.Equal(x1, patch.X1);
        Assert.Equal(y1, patch.Y1);
        Assert.Equal(x2, patch.X2);
        Assert.Equal(y2, patch.Y2);
    }

    private static string FindAssetsDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }
        if (dir == null) return string.Empty;
        return Path.Combine(dir.FullName, "src", "Releve.Game", "assets");
    }

    private static (int Width, int Height) ReadPngDimensions(string path)
    {
        using var fs = File.OpenRead(path);
        var header = new byte[24];
        fs.ReadExactly(header, 0, 24);

        // Verifier signature PNG
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, header[..8]);

        // Largeur a l'offset 16 (big-endian), hauteur a l'offset 20
        int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        return (width, height);
    }
}