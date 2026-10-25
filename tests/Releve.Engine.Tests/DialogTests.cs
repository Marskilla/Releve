using Releve.Engine;
using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Persistence;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Tests pour les dialogues modaux (J8).</summary>
public class DialogTests
{
    private static GameEngine CreateEngine()
    {
        var world = GameWorld.Create();
        return new GameEngine(world, world.CreateInitialState());
    }

    private static GameEngine EngineInLobby()
    {
        var engine = CreateEngine();
        engine.Start();
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");
        engine.Execute("utilise badge sur lecteur escalier");
        engine.Execute("nord");
        return engine;
    }

    [Fact]
    public void Badge_On_Terminal_Returns_Dialog_With_Two_Choices()
    {
        var lobby = EngineInLobby();

        var result = lobby.Execute("passe le badge sur la borne");

        Assert.False(result.StateChanged); // Terminal n'est PAS active
        Assert.Equal(["Vous passez le badge devant la borne. La fiche patient s'affiche."], result.Messages);
        Assert.NotNull(result.Dialog);
        Assert.Equal(DialogKind.Text, result.Dialog.Kind);
        Assert.Null(result.Dialog.Title);
        
        // Contenu = fiche patient (7 lignes)
        var expectedRecord = string.Join("\n", new[]
        {
            "FICHE PATIENT",
            "Nom : Marchand - Prenom : Alex",
            "Date d'entree : 14 mars 2113",
            "Motif d'admission : maladie inconnue evolutive necessitant un traitement adaptatif",
            "Conditions de sortie : maladie en remission complete",
            "Adresse de sortie : Alex Marchand, chez M. et Mme Marchand, 12 rue des Tilleuls",
        });
        Assert.Equal(expectedRecord, result.Dialog.Content);
        
        // Deux choix
        Assert.Equal(2, result.Dialog.Choices.Count);
        Assert.Equal(DialogIds.PatientAccept, result.Dialog.Choices[0].Id);
        Assert.Equal("J'accepte de sortir", result.Dialog.Choices[0].Label);
        Assert.Equal(DialogIds.PatientRefuse, result.Dialog.Choices[1].Id);
        Assert.Equal("Je ne suis pas pret a sortir", result.Dialog.Choices[1].Label);
        
        // Defaut et annulation = PatientRefuse
        Assert.Equal(DialogIds.PatientRefuse, result.Dialog.DefaultChoiceId);
        Assert.Equal(DialogIds.PatientRefuse, result.Dialog.CancelChoiceId);
    }

    [Fact]
    public void ResolveDialog_PatientRefuse_Keeps_Terminal_Inactive()
    {
        var lobby = EngineInLobby();
        lobby.Execute("passe le badge sur la borne");

        var result = lobby.ResolveDialog(DialogIds.PatientRefuse);

        Assert.False(result.StateChanged);
        Assert.Equal(["Vous ne validez pas la sortie."], result.Messages);
        Assert.Equal(ObjectState.None, lobby.State.StateOf(ObjectId.Terminal)); // reste non active (etat initial = None)
        Assert.Equal(ExitState.Blocked, lobby.GetView().Exits[Direction.West]); // sortie ouest bloquee
    }

    [Fact]
    public void ResolveDialog_PatientAccept_Activates_Terminal_And_Opens_West()
    {
        var lobby = EngineInLobby();
        lobby.Execute("passe le badge sur la borne");

        var result = lobby.ResolveDialog(DialogIds.PatientAccept);

        Assert.True(result.StateChanged);
        Assert.Equal(["Vous validez la sortie. La porte vitree de l'entree se deverrouille."], result.Messages);
        Assert.Equal(ObjectState.Activated, lobby.State.StateOf(ObjectId.Terminal)); // Terminal active
        Assert.Equal(ExitState.Open, lobby.GetView().Exits[Direction.West]); // sortie ouest ouverte
        
        // Androide donne la replique de conge
        var talk = lobby.Execute("parle androide");
        Assert.Contains("Merci d'avoir fait appel", talk.Messages[0]);
        Assert.Contains("ne pas vous revoir bientot", talk.Messages[0]);
    }

    [Fact]
    public void Badge_On_Terminal_Already_Activated_Returns_Dialog_With_Single_Close()
    {
        var lobby = EngineInLobby();
        lobby.Execute("passe le badge sur la borne");
        lobby.ResolveDialog(DialogIds.PatientAccept); // active la borne
        
        var result = lobby.Execute("passe le badge sur la borne");

        Assert.False(result.StateChanged);
        Assert.NotNull(result.Dialog);
        Assert.Single(result.Dialog.Choices);
        Assert.Equal(DialogIds.Close, result.Dialog.Choices[0].Id);
        Assert.Equal("Fermer", result.Dialog.Choices[0].Label);
        Assert.Equal(DialogIds.Close, result.Dialog.DefaultChoiceId);
        Assert.Equal(DialogIds.Close, result.Dialog.CancelChoiceId);
    }

    [Fact]
    public void ResolveDialog_Unknown_Returns_Empty_Say()
    {
        var lobby = EngineInLobby();

        var result = lobby.ResolveDialog("n'importe quoi");

        Assert.False(result.StateChanged);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Look_TechDoor_Returns_Image_Dialog()
    {
        var engine = CreateEngine();
        engine.Start();
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");

        var result = engine.Execute("regarde porte local technique");

        Assert.False(result.StateChanged);
        Assert.NotNull(result.Dialog);
        Assert.Equal(DialogKind.Image, result.Dialog.Kind);
        Assert.Equal("technicalRoom", result.Dialog.Content);
        Assert.Single(result.Dialog.Choices);
        Assert.Equal(DialogIds.Close, result.Dialog.Choices[0].Id);
        Assert.Equal("Fermer", result.Dialog.Choices[0].Label);
        
        // Description reutilisee depuis TechDoor
        Assert.Contains("hublot", result.Messages[0]);
        Assert.Contains("chaufferie", result.Messages[0]);
    }

    [Fact]
    public void Look_TechReader_Also_Returns_Image_Dialog()
    {
        var engine = CreateEngine();
        engine.Start();
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");

        var result = engine.Execute("regarde lecteur local technique");

        Assert.False(result.StateChanged);
        Assert.NotNull(result.Dialog);
        Assert.Equal(DialogKind.Image, result.Dialog.Kind);
        Assert.Equal("technicalRoom", result.Dialog.Content);
    }

    [Fact]
    public void Read_TechDoor_Does_Not_Trigger_Dialog()
    {
        var engine = CreateEngine();
        engine.Start();
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");

        var result = engine.Execute("lis porte local technique");

        Assert.Null(result.Dialog); // Pas de dialogue pour Read
    }

    [Fact]
    public void Save_Then_Reload_After_Accept_Preserves_Terminal_Activated()
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
            engine.ResolveDialog(DialogIds.PatientAccept); // Active la borne

            store.Save(engine.State);

            var loaded = store.Load();
            Assert.NotNull(loaded);

            var restored = new GameEngine(GameWorld.Create(), loaded!);
            var messages = restored.Start();

            // Terminal reste Active, sortie Ouest ouverte
            Assert.Equal(ObjectState.Activated, restored.State.StateOf(ObjectId.Terminal));
            Assert.Equal(ExitState.Open, restored.GetView().Exits[Direction.West]);
            Assert.Equal(RoomId.HospitalLobby, restored.State.CurrentRoom);
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