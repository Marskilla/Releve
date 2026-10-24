using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Parsing;
using Releve.Engine.World;
using Xunit;

namespace Releve.Engine.Tests;

/// <summary>Tests des réponses variées et grossièretés (J4, §5.4).</summary>
public class ReplyTests
{
    [Fact]
    public void FlavourText_Lists_Are_NonEmpty_And_No_Duplicates()
    {
        Assert.All(new[] { FlavourText.UnknownNeutral, FlavourText.UnknownHumour, FlavourText.ProfanityReplies }, list =>
        {
            Assert.NotEmpty(list);
            Assert.Equal(list.Distinct().Count(), list.Length);
        });
    }

    [Fact]
    public void UnknownVerb_WithFixedSeed_UsesBothLists_AndNoConsecutiveDuplicates()
    {
        var random = new Random(12345);
        var world = GameWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState(), random);
        engine.Start();

        var allResponses = new List<string>();
        for (int i = 0; i < 500; i++)
        {
            var result = engine.Execute("sefkljfsdlmsfjlmfj");
            var msg = result.Messages[0];
            allResponses.Add(msg);

            // Chaque réponse doit appartenir à UnknownNeutral ∪ UnknownHumour
            var valid = FlavourText.UnknownNeutral.Concat(FlavourText.UnknownHumour).ToArray();
            Assert.Contains(msg, valid);
        }

        // Les deux listes doivent avoir été utilisées
        Assert.True(allResponses.Any(r => FlavourText.UnknownNeutral.Contains(r)));
        Assert.True(allResponses.Any(r => FlavourText.UnknownHumour.Contains(r)));

        // Pas deux fois le même message consécutivement
        for (int i = 1; i < allResponses.Count; i++)
        {
            Assert.NotEqual(allResponses[i - 1], allResponses[i]);
        }
    }

    [Fact]
    public void UnknownVerb_SameSeed_SameSequence()
    {
        var world = GameWorld.Create();
        var engine1 = new GameEngine(world, world.CreateInitialState(), new Random(999));
        var engine2 = new GameEngine(world, world.CreateInitialState(), new Random(999));
        engine1.Start();
        engine2.Start();

        for (int i = 0; i < 50; i++)
        {
            var r1 = engine1.Execute("inconnue" + i).Messages[0];
            var r2 = engine2.Execute("inconnue" + i).Messages[0];
            Assert.Equal(r1, r2);
        }
    }

    [Fact]
    public void ProfanityFilter_Detects_All_Words_And_Expressions()
    {
        // Chaque mot isolé
        foreach (var word in ProfanityFilterContainsWords())
        {
            var words = TextNormalizer.Normalize(word);
            Assert.True(ProfanityFilter.Contains(words), $"Mot non détecté : {word}");
        }

        // Chaque expression
        foreach (var expr in ProfanityFilterExpressions())
        {
            var words = TextNormalizer.Normalize(string.Join(" ", expr));
            Assert.True(ProfanityFilter.Contains(words), $"Expression non détectée : {string.Join(" ", expr)}");
        }

        // Cas particuliers : majuscules, accents
        Assert.True(ProfanityFilter.Contains(TextNormalizer.Normalize("MERDE !!")));
        Assert.True(ProfanityFilter.Contains(TextNormalizer.Normalize("enculé")));

        // Mots légitimes ne déclenchent pas
        Assert.False(ProfanityFilter.Contains(TextNormalizer.Normalize("contre")));
        Assert.False(ProfanityFilter.Contains(TextNormalizer.Normalize("conduit")));
        Assert.False(ProfanityFilter.Contains(TextNormalizer.Normalize("salopette")));
        Assert.False(ProfanityFilter.Contains(TextNormalizer.Normalize("culotte")));
    }

    [Fact]
    public void Profanity_Reply_Blocks_Command_Execution()
    {
        var world = GameWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState(), new Random(42));
        engine.Start();

        // Ouvre l'armoire et prend le badge pour avoir un état connu
        engine.Execute("ouvre armoire");
        engine.Execute("prends badge");

        // Commande avec grossièreté : le badge ne doit PAS être pris (déjà pris), mais l'état ne doit pas changer
        var result = engine.Execute("prends le putain de badge bordel");

        // Réponse de ProfanityReplies
        Assert.Contains(result.Messages[0], FlavourText.ProfanityReplies);

        // L'état n'a pas changé (badge déjà en inventaire, mais la commande n'a pas été exécutée)
        Assert.False(result.StateChanged);
    }

    [Fact]
    public void Profanity_Expression_VaTeFaireFoutre_Returns_ProfanityReply_Not_CannotGo()
    {
        var world = GameWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState(), new Random(42));
        engine.Start();

        var result = engine.Execute("va te faire foutre");

        // Réponse de ProfanityReplies, PAS "Vous ne pouvez pas aller par là."
        Assert.Contains(result.Messages[0], FlavourText.ProfanityReplies);
        Assert.DoesNotContain("allez par là", result.Messages[0]);
        Assert.False(result.StateChanged);
    }

    [Fact]
    public void Other_Errors_Remain_Fixed()
    {
        var world = GameWorld.Create();
        var engine = new GameEngine(world, world.CreateInitialState());
        engine.Start();

        // MissingObject
        Assert.Equal(["Précisez l’objet de la commande."], engine.Execute("prends").Messages);

        // UnknownObject
        Assert.Equal(["Vous ne voyez pas cela ici."], engine.Execute("prends crayon").Messages);

        // Ambiguous (dans le monde de test : "livre" ambigu entre "livre ancien" et "livre de comptes")
        var engine2 = TestSupport.FreshTestEngine();
        var amb = engine2.Execute("regarde livre");
        Assert.StartsWith("Précisez : ", amb.Messages[0]);
    }

    [Fact]
    public void WorldConsistency_No_Alias_Contains_Profanity()
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
                Assert.False(ProfanityFilter.Contains(words),
                    $"L'alias '{alias}' de la pièce {roomId} contient une grossièreté.");
            }
        }
    }

    // Helpers pour accéder aux données privées du filtre via reflection
    private static IEnumerable<string> ProfanityFilterContainsWords()
    {
        var field = typeof(ProfanityFilter).GetField("_words", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (HashSet<string>)field!.GetValue(null)!;
    }

    private static IEnumerable<string[]> ProfanityFilterExpressions()
    {
        var field = typeof(ProfanityFilter).GetField("_expressions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (List<string[]>)field!.GetValue(null)!;
    }
}