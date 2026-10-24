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

        // Comportement accepte (A1) : le badge est invisible tant que l'armoire est fermee, mais
        // « badge » resout partiellement vers « le lecteur de badge » de la meme piece. Le moteur
        // refuse donc la prise au lieu de signaler un objet inconnu.
        Assert.Equal(["Vous ne pouvez pas prendre cela."], engine.Execute("prends badge").Messages);

        Assert.Equal(["Vous ouvrez l’armoire."], engine.Execute("ouvre armoire").Messages);
        Assert.Equal(["Pris."], engine.Execute("prends badge").Messages);
    }

    [Fact]
    public void Corbeille_Selon_Emplacement_Sac()
    {
        var engine = CreateEngine();

        // Sac dans la chambre : la corbeille le contient.
        Assert.Equal(
            ["Une corbeille. Elle ne contient qu’un sac poubelle vide."],
            engine.Execute("regarde corbeille").Messages);

        // Le sac passe dans l'inventaire : la corbeille est vide.
        engine.Execute("prends sac");
        Assert.Equal(["Une corbeille, vide."], engine.Execute("regarde corbeille").Messages);
    }

    [Fact]
    public void Nouveaux_Verbes_De_Deplacement()
    {
        var engine = CreateEngine();

        // « sors » depuis la chambre = Est (mot de sortie, verbe compte lui-meme).
        var sors = engine.Execute("sors");
        Assert.Equal(
            ["La porte est solidement fermée et le lecteur de badge affiche un rouge fixe."],
            sors.Messages);
        Assert.Equal(ExitState.Blocked, engine.GetView().Exits[Direction.East]);

        // Debloquer, puis « sortir » fonctionne.
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        var sortir = engine.Execute("sortir");
        Assert.Contains("couloir", sortir.Messages[0], StringComparison.Ordinal);
        Assert.Equal(RoomId.TechCorridor, engine.State.CurrentRoom);
    }

    [Fact]
    public void Mots_De_Sortie_Prioritaires_Sur_Alias_Pour_Go()
    {
        var lobby = EngineInLobby();
        lobby.Execute("passe le badge sur la borne"); // Ouest desormais ouvert

        // « va rue » depuis l'accueil : « rue » est un mot de sortie (Ouest), prioritaire sur
        // l'alias « rue » de la porte vitree.
        Assert.Equal(ExitState.Open, lobby.GetView().Exits[Direction.West]);
        var ouest = lobby.Execute("va rue");
        Assert.Contains("rue vide", ouest.Messages[0], StringComparison.Ordinal);
        Assert.Equal(RoomId.Street, lobby.State.CurrentRoom);
    }

    [Fact]
    public void Regarde_Escalier_Reste_Look_Pas_Deplacement()
    {
        var corridor = EngineInCorridor();

        // « regarde escalier » n'est pas un deplacement : l'alias remporte pour les non-Go.
        var look = corridor.Execute("regarde escalier");
        Assert.Contains("vitrage", look.Messages[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Go_Sans_Mot_De_Sortie_Correspondant_Repond_CannotGo()
    {
        var corridor = EngineInCorridor();

        // « sors » dans le couloir : pas de mot de sortie correspondant.
        var sors = corridor.Execute("sors");
        Assert.Equal(["Vous ne pouvez pas aller par là."], sors.Messages);
        Assert.False(sors.StateChanged);
        Assert.Equal(RoomId.TechCorridor, corridor.State.CurrentRoom);
    }

    [Fact]
    public void Go_Sans_Direction_Ne_Leve_Pas_Exception()
    {
        var engine = CreateEngine();

        // « va porte » dans la chambre : « porte » n'est pas un mot de sortie.
        var r1 = engine.Execute("va porte");
        Assert.Equal(["Vous ne pouvez pas aller par là."], r1.Messages);
        Assert.False(r1.StateChanged);

        // « aller lit » : « lit » n'est pas un mot de sortie.
        var r2 = engine.Execute("aller lit");
        Assert.Equal(["Vous ne pouvez pas aller par là."], r2.Messages);

        // « va ascenseur » depuis l'accueil : « ascenseur » est un mot de sortie (Nord), mais
        // la sortie est bloquee -> message de blocage, pas d'exception.
        var lobby = EngineInLobby();
        var r3 = lobby.Execute("va ascenseur");
        Assert.Contains("ascenseur", r3.Messages[0], StringComparison.Ordinal);
        Assert.False(r3.StateChanged);
    }

    [Fact]
    public void Read_Sans_Cible_Rien_A_Lire()
    {
        var engine = CreateEngine();

        var result = engine.Execute("lis");
        Assert.Equal(["Il n’y a rien à lire ici."], result.Messages);
        Assert.False(result.StateChanged);
    }

    [Fact]
    public void Read_Afficheur_Et_Machines_Pareil_Que_Look()
    {
        var engine = CreateEngine();

        // Machines dans la chambre.
        var machines = engine.Execute("lis machines");
        Assert.Contains("Dossier partiel", machines.Messages[0]);
        Assert.False(machines.StateChanged);

        // Aller a l'accueil pour l'afficheur.
        var lobby = EngineInLobby();
        var afficheur = lobby.Execute("lis afficheur");
        Assert.Contains("9 octobre 2173", afficheur.Messages[0]);
        Assert.False(afficheur.StateChanged);
    }

    [Fact]
    public void Use_Sans_Instrument_Precisez_Cible()
    {
        var engine = CreateEngine();
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");

        // Badge dans l'inventaire, mais « utilise badge » n'a pas d'instrument.
        var result = engine.Execute("utilise badge");
        Assert.Equal(["Précisez sur quoi l’utiliser."], result.Messages);
        Assert.False(result.StateChanged);
    }

    [Fact]
    public void Ouvre_Portes_Selon_Etat()
    {
        // Chambre : porte pleine (non dans la table A3, comportement inchangé).
        var bedroom = CreateEngine();
        var porteVerrouillee = bedroom.Execute("ouvre porte");
        Assert.Equal(
            ["La porte est solidement fermée et le lecteur de badge affiche un rouge fixe."],
            porteVerrouillee.Messages);

        // Couloir : porte de l'escalier verrouillee.
        var corridor = EngineInCorridor();
        var escalierVerrouille = corridor.Execute("ouvre porte de l'escalier");
        Assert.Equal(
            ["La porte de l’escalier est fermée. Le lecteur de badge affiche un rouge fixe."],
            escalierVerrouille.Messages);

        // Couloir : apres deverrouillage, porte de l'escalier ouverte.
        corridor.Execute("utilise badge sur lecteur escalier");
        var escalierOuvert = corridor.Execute("ouvre porte de l'escalier");
        Assert.Equal(["La porte de l’escalier est déverrouillée."], escalierOuvert.Messages);

        // Couloir : porte du local technique, toujours refusee.
        var localTechnique = corridor.Execute("ouvre porte technique");
        Assert.Equal(
            ["La porte du local technique est fermée. Le lecteur de badge affiche un rouge fixe."],
            localTechnique.Messages);

        // Accueil : porte vitree verrouillee.
        var lobby = EngineInLobby();
        var vitreeFermee = lobby.Execute("ouvre porte vitree");
        Assert.Equal(["La porte vitrée de l’entrée reste fermée."], vitreeFermee.Messages);

        // Accueil : apres activation du borne, porte vitree ouverte.
        lobby.Execute("passe le badge sur la borne");
        var vitreeOuverte = lobby.Execute("ouvre porte vitree");
        Assert.Equal(["La porte vitrée de l’entrée se déverrouille."], vitreeOuverte.Messages);

        // Accueil : ascenseur, toujours refuse.
        var ascenseur = lobby.Execute("ouvre ascenseur");
        Assert.Equal(
            ["Les portes de l’ascenseur restent closes. Le lecteur de badge affiche un rouge fixe."],
            ascenseur.Messages);
    }

    [Fact]
    public void Verbes_Talk_Ajoutes()
    {
        var lobby = EngineInLobby();

        var interroge = lobby.Execute("interroge androide");
        Assert.Equal(
            ["L’androïde s’anime : « Veuillez passer votre badge sur la borne pour sortir. »"],
            interroge.Messages);

        var demande = lobby.Execute("demande androide");
        Assert.Equal(
            ["L’androïde s’anime : « Veuillez passer votre badge sur la borne pour sortir. »"],
            demande.Messages);
    }

    [Fact]
    public void Verbes_Use_Ajoutes()
    {
        var corridor = EngineInCorridor();

        // « pousse » et « actionne » sont des verbes Use.
        var pousse = corridor.Execute("pousse lecteur escalier");
        Assert.Equal(["Rien ne se passe."], pousse.Messages); // cible pas dans l'inventaire -> NothingHappens

        // « utilise badge sur lecteur escalier » fonctionne deja (instrument present).
        var actionne = corridor.Execute("actionne badge sur lecteur escalier");
        Assert.Equal(["Le voyant passe au vert : la porte de l’escalier se déverrouille."], actionne.Messages);
    }

    [Fact]
    public void Verbes_Go_Ajoutes()
    {
        var engine = CreateEngine();
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est"); // maintenant dans le couloir

        // « monte » depuis le couloir (bloque, lecteur de l'escalier encore verrouille).
        var monte = engine.Execute("monte");
        Assert.Equal(
            ["La porte de l’escalier est fermée. Le lecteur de badge affiche un rouge fixe."],
            monte.Messages);

        // « descendre » depuis l'accueil (vers le couloir, ouvert).
        var lobby = EngineInLobby();
        var descends = lobby.Execute("descendre");
        Assert.Contains("couloir", descends.Messages[0], StringComparison.Ordinal);
        Assert.Equal(RoomId.TechCorridor, lobby.State.CurrentRoom);
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

        // 1. depart : message d'accueil, puis description courte ; fleche est bloquee.
        var start = engine.Start();
        Assert.Equal(2, start.Count);
        Assert.Equal("Alors que vous ouvrez doucement les yeux, une voix de synthèse annonce : « Réveil terminé, constantes stables. » En vous examinant, vous constatez que vous portez une chemise d’hôpital, la tenue standard des malades alités.", start[0]);
        Assert.Equal("La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu’une armoire.", start[1]);
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
            Assert.DoesNotContain(messages, m => m.Contains("ouvrez doucement les yeux", StringComparison.Ordinal));
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
