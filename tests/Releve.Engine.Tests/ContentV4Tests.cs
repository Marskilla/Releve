using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Tests de contenu V4 (J4).</summary>
public class ContentV4Tests
{
    private static GameEngine CreateEngine()
    {
        var world = GameWorld.Create();
        return new GameEngine(world, world.CreateInitialState());
    }

    private static GameEngine EngineInCorridor()
    {
        var engine = CreateEngine();
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");
        return engine;
    }

    private static GameEngine EngineInLobby()
    {
        var engine = EngineInCorridor();
        engine.Execute("utilise badge sur lecteur escalier");
        engine.Execute("nord");
        return engine;
    }

    [Fact]
    public void Bedroom_Materiel_Aliases_Return_Same_As_Machine()
    {
        var engine = CreateEngine();
        engine.Start();

        var lookMateriel = engine.Execute("regarde materiel");
        var examineMaterielMedical = engine.Execute("examine materiel medical");
        var lookMachine = engine.Execute("regarde machine");

        Assert.Equal(lookMachine.Messages, lookMateriel.Messages);
        Assert.Equal(lookMachine.Messages, examineMaterielMedical.Messages);
    }

    [Fact]
    public void Corridor_TechDoor_And_TechReader_Aliases_Work()
    {
        var engine = EngineInCorridor();

        // « utilise badge sur lecteur local technique » -> refus
        var useTechReader = engine.Execute("utilise badge sur lecteur local technique");
        Assert.Equal(["Le lecteur refuse le badge. Son voyant clignote en rouge."], useTechReader.Messages);

        // « regarde porte local technique » -> description de la porte
        var lookTechDoor = engine.Execute("regarde porte local technique");
        Assert.Contains("hublot", lookTechDoor.Messages[0]);

        // « porte » seule et « lecteur » seul restent ambigus
        var porteAmbiguous = engine.Execute("regarde porte");
        Assert.Contains("Precisez", porteAmbiguous.Messages[0]);

        var lecteurAmbiguous = engine.Execute("utilise badge sur lecteur");
        Assert.Contains("Precisez", lecteurAmbiguous.Messages[0]);
    }

    [Fact]
    public void Lobby_Description_Contains_Afficheur()
    {
        var lobby = EngineInLobby();
        var start = lobby.Start();
        Assert.Contains("afficheur", start[0]);
    }

    [Fact]
    public void Lobby_Examine_Horloge_And_Afficheur_Return_Date()
    {
        var lobby = EngineInLobby();

        var examineHorloge = lobby.Execute("examine horloge");
        var examineAfficheur = lobby.Execute("examine afficheur");
        var regardeAfficheur = lobby.Execute("regarde afficheur");
        var lisAfficheur = lobby.Execute("lis afficheur");

        Assert.Contains("9 octobre 2173", examineHorloge.Messages[0]);
        Assert.Contains("9 octobre 2173", examineAfficheur.Messages[0]);
        Assert.Contains("9 octobre 2173", regardeAfficheur.Messages[0]);
        Assert.Contains("9 octobre 2173", lisAfficheur.Messages[0]);
    }

    [Fact]
    public void Lobby_West_Blocked_Returns_WestBlockedMessage()
    {
        var lobby = EngineInLobby();

        var ouest = lobby.Execute("ouest");
        Assert.Equal(["La porte vitrée de l'entrée est fermée. L'androïde s'adresse alors à vous : « S'il vous plaît ! » Il semble vous inviter à venir le voir."], ouest.Messages);
    }

    [Fact]
    public void Lobby_Ouvre_Porte_Vitree_Before_Activation_Returns_WestBlockedMessage()
    {
        var lobby = EngineInLobby();

        var ouvre = lobby.Execute("ouvre porte vitree");
        Assert.Equal(["La porte vitrée de l'entrée est fermée. L'androïde s'adresse alors à vous : « S'il vous plaît ! » Il semble vous inviter à venir le voir."], ouvre.Messages);
    }

    [Fact]
    public void Lobby_Android_Talk_Before_And_After_Activation()
    {
        var lobby = EngineInLobby();

        // Avant activation : reponse d'emargement
        var talkBefore = lobby.Execute("parle androide");
        Assert.Contains("émargement", talkBefore.Messages[0]);
        Assert.Contains("borne du comptoir", talkBefore.Messages[0]);

        // Forcer l'activation de la borne
        lobby.State.States[ObjectId.Terminal] = ObjectState.Activated;

        // Apres activation : reponse de conge
        var talkAfter = lobby.Execute("parle androide");
        Assert.Contains("Merci d'avoir fait appel", talkAfter.Messages[0]);
        Assert.Contains("ne pas vous revoir bientôt", talkAfter.Messages[0]);
    }
}