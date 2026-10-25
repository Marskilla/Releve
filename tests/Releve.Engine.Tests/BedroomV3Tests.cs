using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Parsing;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Tests de la chambre version V3 (§5.3).</summary>
public class BedroomV3Tests
{
    private static GameEngine CreateEngine()
    {
        var world = GameWorld.Create();
        return new GameEngine(world, world.CreateInitialState());
    }

    private static GameEngine EngineInCorridor()
    {
        var engine = CreateEngine();
        engine.Start(); // ajoute WakeAnnouncementDone
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");
        engine.Execute("utilise badge sur lecteur");
        engine.Execute("est");
        return engine;
    }

    [Fact]
    public void Start_NewGame_Returns_Intro_Then_Description()
    {
        var engine = CreateEngine();
        var messages = engine.Start();

        Assert.Equal(2, messages.Count);
        Assert.Equal("Alors que vous ouvrez doucement les yeux, une voix de synthèse annonce : « Réveil terminé, constantes stables. » En vous examinant, vous constatez que vous portez une chemise d'hôpital, la tenue standard des malades alités.", messages[0]);
        Assert.Equal("La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu'une armoire.", messages[1]);
        Assert.Contains(GameEvent.WakeAnnouncementDone, engine.State.Events);
    }

    [Fact]
    public void Start_ReloadedState_Returns_Only_Description()
    {
        var world = GameWorld.Create();
        var state = world.CreateInitialState();
        state.Events.Add(GameEvent.WakeAnnouncementDone);

        var engine = new GameEngine(world, state);
        var messages = engine.Start();

        Assert.Single(messages);
        Assert.Equal("La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu'une armoire.", messages[0]);
    }

    [Fact]
    public void Reenter_Bedroom_Shows_Only_Description()
    {
        var corridor = EngineInCorridor();
        var reenter = corridor.Execute("ouest");
        Assert.Single(reenter.Messages);
        Assert.Equal("La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu'une armoire.", reenter.Messages[0]);
    }

    [Fact]
    public void Look_Without_Target_Variants_Return_DetailedDescription()
    {
        var engine = CreateEngine();
        engine.Start();

        // Avec sac present
        var look = engine.Execute("regarde");
        Assert.Equal("La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu'une armoire. Dans un coin se trouve une corbeille et son sac vide. Une porte contrôlée par un lecteur de badge constitue la seule sortie.", look.Messages[0]);

        var observe = engine.Execute("observe");
        Assert.Equal(look.Messages, observe.Messages);

        var regardePiece = engine.Execute("regarde piece");
        Assert.Equal(look.Messages, regardePiece.Messages);

        var regardeDansPiece = engine.Execute("regarde dans la piece");
        Assert.Equal(look.Messages, regardeDansPiece.Messages);

        var regardeAutourMoi = engine.Execute("regarde autour de moi");
        Assert.Equal(look.Messages, regardeAutourMoi.Messages);

        var regardeIci = engine.Execute("regarde ici");
        Assert.Equal(look.Messages, regardeIci.Messages);

        // Prendre le sac -> description detaillee sans sac
        engine.Execute("prends sac");
        var lookSansSac = engine.Execute("regarde");
        Assert.Equal("La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu'une armoire. Dans un coin se trouve une corbeille. Une porte contrôlée par un lecteur de badge constitue la seule sortie.", lookSansSac.Messages[0]);
    }

    [Fact]
    public void Look_In_Corridor_Returns_Short_Description()
    {
        var corridor = EngineInCorridor();
        var observe = corridor.Execute("observe");
        Assert.Equal("Un couloir de service étroit, sans fenêtre. Quelques plafonniers sont faibles ou éteints ; une poussière légère recouvre le sol.", observe.Messages[0]);
    }

    [Fact]
    public void Regarde_Chambre_In_Corridor_Returns_UnknownObject()
    {
        var corridor = EngineInCorridor();
        var result = corridor.Execute("regarde chambre");
        Assert.Equal(["Vous ne voyez pas cela ici."], result.Messages);
    }

    [Fact]
    public void WorldConsistency_No_ObjectAlias_Contains_RoomWord()
    {
        var world = GameWorld.Create();

        foreach (var (roomId, room) in world.Rooms)
        {
            var aliases = world.Objects.Values
                .Where(def => def.InitialLocation.Kind == LocationKind.Room
                    && def.InitialLocation.Room == roomId)
                .SelectMany(def => def.Aliases.Select(alias => string.Join(" ", TextNormalizer.Normalize(alias))))
                .ToList();

            foreach (var alias in aliases)
            {
                var words = alias.Split(' ');
                foreach (var word in words)
                {
                    Assert.False(Vocabulary.RoomWords.Contains(word),
                        $"L'alias '{alias}' de la pièce {roomId} contient le mot de pièce '{word}'.");
                }
            }
        }
    }
}