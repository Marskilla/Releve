using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Persistence;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Tests propres au contenu du PoC (§4) et scenario de reference (§4.1).</summary>
public class PocContentTests
{
    private static GameEngine CreateEngine()
    {
        var world = GameWorld.Create();

        // La piece de depart du monde est la chambre de reveil : CreateInitialState suffit.
        return new GameEngine(world, world.CreateInitialState());
    }

    /// <summary>Moteur place dans le couloir technique, badge en poche.</summary>
    private static GameEngine EngineInCorridor()
    {
        var engine = CreateEngine();
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");
        return engine;
    }

    /// <summary>Moteur place a l'accueil, porte de l'escalier deverrouillee.</summary>
    private static GameEngine EngineInLobby()
    {
        var engine = EngineInCorridor();
        engine.Execute("utilise badge sur lecteur escalier");
        engine.Execute("nord");
        return engine;
    }

    [Fact]
    public void Dead_End_Exits_Are_Always_Blocked()
    {
        var corridor = EngineInCorridor();
        Assert.Equal(ExitState.Blocked, corridor.GetView().Exits[Direction.East]);

        var lobby = EngineInLobby();
        Assert.Equal(ExitState.Blocked, lobby.GetView().Exits[Direction.North]);
    }

    [Fact]
    public void Every_Exit_Has_A_Return_Except_Dead_Ends()
    {
        var corridor = EngineInCorridor();
        Assert.Equal(ExitState.Open, corridor.GetView().Exits[Direction.West]);

        var lobby = EngineInLobby();
        Assert.Equal(ExitState.Open, lobby.GetView().Exits[Direction.South]);

        lobby.Execute("passe le badge sur la borne");
        Assert.Equal(ExitState.Open, lobby.GetView().Exits[Direction.West]);

        var street = lobby;
        street.Execute("ouest");
        Assert.Equal(RoomId.Street, street.State.CurrentRoom);
        Assert.Equal(ExitState.Open, street.GetView().Exits[Direction.East]);
    }

    [Fact]
    public void Corridor_Bare_Door_And_Reader_Are_Ambiguous()
    {
        var engine = EngineInCorridor();

        var reader = engine.Execute("utilise badge sur lecteur");
        Assert.Equal(
            ["Précisez : le lecteur de l’escalier ou le lecteur du local technique."],
            reader.Messages);

        var door = engine.Execute("regarde porte");
        Assert.Equal(
            ["Précisez : la porte de l’escalier ou la porte du local technique."],
            door.Messages);
    }

    [Fact]
    public void Badge_Is_Reachable_Only_After_Opening_The_Wardrobe()
    {
        var engine = CreateEngine();

        // Ecart signale par rapport au PoC §4.1 (etape 3) : avant l'ouverture de l'armoire, le badge est
        // invisible mais « badge » reste resolu partiellement vers « le lecteur de badge » de la meme piece.
        // Le moteur refuse donc la prise au lieu de signaler un objet inconnu.
        Assert.Equal(["Vous ne pouvez pas prendre cela."], engine.Execute("prends badge").Messages);

        Assert.Equal(["Vous ouvrez l’armoire."], engine.Execute("ouvre armoire").Messages);
        Assert.Equal(["Pris."], engine.Execute("prends badge").Messages);
    }

    [Fact]
    public void Patient_Record_Is_Displayed_Verbatim()
    {
        var lobby = EngineInLobby();
        var result = lobby.Execute("passe le badge sur la borne");

        Assert.Equal(
        [
            "FICHE PATIENT",
            "Nom : Marchand",
            "Prénom : Alex",
            "Date d’entrée : 14 mars 2113",
            "Motif d’admission : maladie inconnue évolutive nécessitant un traitement adaptatif",
            "Conditions de sortie : maladie en rémission complète",
            "Adresse de sortie : Alex Marchand, chez M. et Mme Marchand, 12 rue des Tilleuls",
            "La porte vitrée de l’entrée se déverrouille.",
        ], result.Messages);

        // Repeter l'action reaffiche la fiche, sans le message de deverrouillage.
        var again = lobby.Execute("passe le badge sur la borne");
        Assert.Equal("Adresse de sortie : Alex Marchand, chez M. et Mme Marchand, 12 rue des Tilleuls", again.Messages[^1]);
        Assert.DoesNotContain("La porte vitrée de l’entrée se déverrouille.", again.Messages);
    }

    [Fact]
    public void Reference_Scenario_Plays_From_Wake_Up_To_The_Street()
    {
        var engine = CreateEngine();

        // 1. depart : description de la piece, puis annonce de reveil ; fleche est bloquee.
        var start = engine.Start();
        Assert.Equal(2, start.Count);
        Assert.Equal("Vous portez une blouse médicale. La pièce est propre, éclairée, sans fenêtre.", start[0]);
        Assert.Equal("Une voix de synthèse annonce : « Réveil terminé, constantes stables. »", start[1]);
        Assert.Equal(ExitState.Blocked, engine.GetView().Exits[Direction.East]);

        // 2. « est » : message de porte fermee, sans indiquer comment debloquer.
        var east = engine.Execute("est");
        Assert.Equal(
            ["La porte est solidement fermée et le lecteur de badge affiche un rouge fixe."],
            east.Messages);
        Assert.Equal(RoomId.Bedroom, engine.State.CurrentRoom);

        // 3. ouverture de l'armoire, puis prise du badge.
        Assert.Equal(["Vous ouvrez l’armoire."], engine.Execute("ouvre armoire").Messages);
        Assert.Equal(["Pris."], engine.Execute("prends badge").Messages);

        // 4. badge sur le lecteur : la fleche est devient active.
        var unlocked = engine.Execute("utilise badge sur lecteur");
        Assert.Equal(
            ["Vous passez le badge devant le lecteur. Le voyant passe au vert et la porte se déverrouille."],
            unlocked.Messages);
        Assert.Equal(ExitState.Open, engine.GetView().Exits[Direction.East]);

        // 5. couloir technique : fleches nord et est bloquees.
        var entered = engine.Execute("est");
        Assert.Equal(
            "Un couloir de service étroit, sans fenêtre. Quelques plafonniers sont faibles ou éteints ; une poussière légère recouvre le sol.",
            entered.Messages[0]);
        Assert.Equal(RoomId.TechCorridor, engine.State.CurrentRoom);
        Assert.Equal(ExitState.Blocked, engine.GetView().Exits[Direction.North]);
        Assert.Equal(ExitState.Blocked, engine.GetView().Exits[Direction.East]);

        // 6. « lecteur » seul est ambigu dans le couloir.
        Assert.Contains("Précisez", engine.Execute("utilise badge sur lecteur").Messages[0]);

        // 7. lecteur de l'escalier, puis montee vers l'accueil.
        var stairs = engine.Execute("utilise badge sur lecteur escalier");
        Assert.Equal(["Le voyant passe au vert : la porte de l’escalier se déverrouille."], stairs.Messages);
        Assert.Equal(ExitState.Open, engine.GetView().Exits[Direction.North]);

        var lobby = engine.Execute("nord");
        Assert.Contains("hall d’accueil", lobby.Messages[0], StringComparison.Ordinal);
        Assert.Equal(RoomId.HospitalLobby, engine.State.CurrentRoom);

        // 8. sortie ouest bloquee, puis reponse de l'androide.
        var west = engine.Execute("ouest");
        Assert.Equal(["La porte vitrée de l’entrée reste fermée."], west.Messages);

        var talk = engine.Execute("parle à l’androïde");
        Assert.Equal(
            ["L’androïde s’anime : « Veuillez passer votre badge sur la borne pour sortir. »"],
            talk.Messages);

        // 9. badge sur la borne : fiche patient, puis deverrouillage de la porte vitree.
        var record = engine.Execute("passe le badge sur la borne");
        Assert.Equal(8, record.Messages.Count);
        Assert.Equal("FICHE PATIENT", record.Messages[0]);
        Assert.Equal("La porte vitrée de l’entrée se déverrouille.", record.Messages[^1]);
        Assert.Equal(ExitState.Open, engine.GetView().Exits[Direction.West]);

        // 10. rue provisoire : fin du PoC.
        var street = engine.Execute("ouest");
        Assert.Contains("rue vide", street.Messages[0], StringComparison.Ordinal);
        Assert.Equal(RoomId.Street, engine.State.CurrentRoom);
        Assert.Equal(ExitState.Open, engine.GetView().Exits[Direction.East]);
        Assert.Equal(ExitState.Absent, engine.GetView().Exits[Direction.North]);
    }

    [Fact]
    public void Save_Then_Reload_Restores_Room_Inventory_And_States()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ReleveTests", Guid.NewGuid().ToString("n"));
        var filePath = Path.Combine(directory, "save.dat");
        var store = new EncryptedFileSaveStore(filePath);

        try
        {
            var engine = CreateEngine();
            engine.Start();
            engine.Execute("ouvre armoire");
            engine.Execute("prends badge");
            engine.Execute("utilise badge sur lecteur");
            engine.Execute("est");
            engine.Execute("utilise badge sur lecteur escalier");
            engine.Execute("nord");
            engine.Execute("passe le badge sur la borne");
            engine.Execute("ouest");

            store.Save(engine.State);

            var loaded = store.Load();
            Assert.NotNull(loaded);

            var restored = new GameEngine(GameWorld.Create(), loaded!);
            var messages = restored.Start();

            // 11. piece, inventaire et etats identiques ; l'annonce de reveil n'est pas rejouee.
            Assert.Equal(RoomId.Street, restored.State.CurrentRoom);
            Assert.True(restored.State.IsInInventory(ObjectId.Badge));
            Assert.Equal(ObjectState.Open, restored.State.StateOf(ObjectId.BedroomWardrobe));
            Assert.Equal(ObjectState.Unlocked, restored.State.StateOf(ObjectId.BedroomReader));
            Assert.Equal(ObjectState.Unlocked, restored.State.StateOf(ObjectId.StairsReader));
            Assert.Equal(ObjectState.Activated, restored.State.StateOf(ObjectId.Terminal));
            Assert.Contains(GameEvent.WakeAnnouncementDone, restored.State.Events);
            Assert.Equal(ExitState.Open, restored.GetView().Exits[Direction.East]);
            Assert.DoesNotContain(messages, m => m.Contains("voix de synthèse", StringComparison.Ordinal));
        }
        finally
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
