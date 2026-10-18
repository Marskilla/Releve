using Releve.Engine.Core;
using Releve.Engine.Parsing;
using Releve.Engine.Tests.World;
using Releve.Engine.World;
using Xunit;
namespace Releve.Engine.Tests;

public class ParserTests
{
    private static readonly WorldModel World = TestWorldFactory.Create();

    private static ParseResult Parse(GameState state, string input) =>
        CommandParser.Parse(input, TestScope.Visible(state, World));

    [Theory]
    [InlineData("prends clef", GameAction.Take, ObjectId.TestKey, null)]
    [InlineData("prendre cle", GameAction.Take, ObjectId.TestKey, null)]
    [InlineData("ramasse clé", GameAction.Take, ObjectId.TestKey, null)]
    [InlineData("prenez la CLEF", GameAction.Take, ObjectId.TestKey, null)]
    [InlineData("prennez petite clef", GameAction.Take, ObjectId.TestKey, null)]
    [InlineData("regarde armoire", GameAction.Look, ObjectId.TestCabinet, null)]
    [InlineData("examine le placard", GameAction.Look, ObjectId.TestCabinet, null)]
    [InlineData("pose clef", GameAction.Drop, ObjectId.TestKey, null)]
    [InlineData("depose la clef", GameAction.Drop, ObjectId.TestKey, null)]
    [InlineData("jette clef", GameAction.Drop, ObjectId.TestKey, null)]
    [InlineData("ouvre l'armoire", GameAction.Open, ObjectId.TestCabinet, null)]
    [InlineData("ferme placard", GameAction.Close, ObjectId.TestCabinet, null)]
    [InlineData("parle a armoire", GameAction.Talk, ObjectId.TestCabinet, null)]
    [InlineData("appuie sur armoire", GameAction.Use, ObjectId.TestCabinet, null)]
    [InlineData("saisis le livre ancien", GameAction.Take, ObjectId.TestBook, null)]
    [InlineData("utilise clef sur armoire", GameAction.Use, ObjectId.TestCabinet, ObjectId.TestKey)]
    [InlineData("utilise la clef sur l’armoire", GameAction.Use, ObjectId.TestCabinet, ObjectId.TestKey)]
    [InlineData("utilise clef au placard", GameAction.Use, ObjectId.TestCabinet, ObjectId.TestKey)]
    public void Parse_Verb_Variants(string input, GameAction expected, ObjectId target, ObjectId? instrument)
    {
        var result = Parse(World.CreateInitialState(), input);

        Assert.Equal(ParseError.None, result.Error);
        Assert.NotNull(result.Command);
        Assert.Equal(expected, result.Command!.Action);
        Assert.Equal(target, result.Command.Target);
        Assert.Equal(instrument, result.Command.Instrument);
        Assert.Null(result.Command.Direction);
    }

    [Theory]
    [InlineData("est", Direction.East)]
    [InlineData("E", Direction.East)]
    [InlineData("Ouest", Direction.West)]
    [InlineData("n", Direction.North)]
    [InlineData("va vers est", Direction.East)]
    [InlineData("va a ouest", Direction.West)]
    [InlineData("aller a nord", Direction.North)]
    [InlineData("allez vers sud", Direction.South)]
    [InlineData("avance est", Direction.East)]
    public void Parse_Directions(string input, Direction expected)
    {
        var result = Parse(World.CreateInitialState(), input);

        Assert.Equal(ParseError.None, result.Error);
        Assert.Equal(GameAction.Go, result.Command!.Action);
        Assert.Equal(expected, result.Command.Direction);
    }

    [Fact]
    public void Parse_Look_And_Inventory_Without_Target()
    {
        var state = World.CreateInitialState();

        var look = Parse(state, "regarde");
        Assert.Equal(GameAction.Look, look.Command!.Action);
        Assert.Null(look.Command.Target);

        var inventory = Parse(state, "i");
        Assert.Equal(GameAction.Inventory, inventory.Command!.Action);
    }

    [Fact]
    public void Parse_Ambiguous_Object_Lists_Candidates()
    {
        var result = Parse(World.CreateInitialState(), "regarde livre");

        Assert.Equal(ParseError.Ambiguous, result.Error);
        Assert.Contains("livre ancien", result.CandidateNames!);
        Assert.Contains("livre de comptes", result.CandidateNames!);
        Assert.Equal("Précisez : livre ancien ou livre de comptes.", result.ErrorMessage);
    }

    [Fact]
    public void Parse_Invisible_Object_Is_Unknown()
    {
        var state = World.CreateInitialState();
        var hidden = Parse(state, "regarde carnet");
        Assert.Equal(ParseError.UnknownObject, hidden.Error);
        Assert.Equal("Vous ne voyez pas cela ici.", hidden.ErrorMessage);
    }

    [Fact]
    public void Parse_Unknown_Object_And_Verb()
    {
        var unknown = Parse(World.CreateInitialState(), "prends dragon");
        Assert.Equal(ParseError.UnknownObject, unknown.Error);

        var verb = Parse(World.CreateInitialState(), "saute");
        Assert.Equal(ParseError.UnknownVerb, verb.Error);
        Assert.Equal("Cette commande n’est pas comprise.", verb.ErrorMessage);
    }

    [Fact]
    public void Parse_Empty_And_Missing_Object()
    {
        var empty = Parse(World.CreateInitialState(), "   ");
        Assert.Equal(ParseError.Empty, empty.Error);
        Assert.Equal(string.Empty, empty.ErrorMessage);

        var missing = Parse(World.CreateInitialState(), "prends");
        Assert.Equal(ParseError.MissingObject, missing.Error);
        Assert.Equal("Précisez l’objet de la commande.", missing.ErrorMessage);
    }

    /// <summary>Mots de sortie : passages directs et rejet quand aucun mot ne correspond.</summary>
    public class ExitWordsTests
    {
        private static ParseResult ParseWithExits(string input, IReadOnlyDictionary<string, Direction> exitWords) =>
            CommandParser.Parse(input, Array.Empty<ObjectDef>(), exitWords);

        [Fact]
        public void Verbe_Go_Seul_Est_Mot_De_Sortie()
        {
            var exits = new Dictionary<string, Direction> { ["sors"] = Direction.East };
            var result = ParseWithExits("sors", exits);

            Assert.Equal(ParseError.None, result.Error);
            Assert.Equal(Direction.East, result.Command!.Direction);
        }

        [Fact]
        public void Mot_De_Sortie_Apres_Verbe_Go()
        {
            var exits = new Dictionary<string, Direction> { ["rue"] = Direction.West };
            var result = ParseWithExits("va rue", exits);

            Assert.Equal(ParseError.None, result.Error);
            Assert.Equal(Direction.West, result.Command!.Direction);
        }

        [Fact]
        public void Mot_De_Sortie_Avec_Preposition()
        {
            var exits = new Dictionary<string, Direction> { ["escalier"] = Direction.North };
            var result = ParseWithExits("monte vers escalier", exits);

            Assert.Equal(ParseError.None, result.Error);
            Assert.Equal(Direction.North, result.Command!.Direction);
        }

        [Fact]
        public void Verbe_Go_Sans_Mot_De_Sortie_Retourne_MissingObject()
        {
            var exits = new Dictionary<string, Direction> { ["sors"] = Direction.East };
            var result = ParseWithExits("va", exits);

            Assert.Equal(ParseError.MissingObject, result.Error);
        }

        [Fact]
        public void Nouveau_Verbe_Go_Sans_Mot_De_Sortie_Retourne_Direction_Nulle()
        {
            var exits = new Dictionary<string, Direction> { ["sors"] = Direction.East };
            var result = ParseWithExits("sortir", exits);

            // "sortir" n'est pas dans la liste : verbe inconnu comme mot de sortie => direction nulle.
            Assert.Equal(ParseError.None, result.Error);
            Assert.Null(result.Command!.Direction);
        }

        [Fact]
        public void Mots_Melanges_Sortie_et_Non_Sortie_Retourne_Direction_Nulle()
        {
            var exits = new Dictionary<string, Direction> { ["rue"] = Direction.West };
            var result = ParseWithExits("va porte", exits);

            Assert.Equal(ParseError.None, result.Error);
            Assert.Null(result.Command!.Direction);
        }

        [Fact]
        public void Verbe_Non_Go_Ignore_Les_Mots_De_Sortie()
        {
            var exits = new Dictionary<string, Direction> { ["rue"] = Direction.West };
            var result = ParseWithExits("regarde rue", exits);

            Assert.Equal(ParseError.UnknownObject, result.Error);
        }
    }
}

/// <summary>Portee de test, calculee comme celle du moteur (§6.6).</summary>
internal static class TestScope
{
    public static IReadOnlyCollection<ObjectDef> Visible(GameState state, WorldModel world) => world.Objects.Values
        .Where(def =>
        {
            if (!state.Locations.TryGetValue(def.Id, out var location))
            {
                return false;
            }

            var accessible = location.Kind == LocationKind.Inventory
                || (location.Kind == LocationKind.Room && location.Room == state.CurrentRoom);
            return accessible && (def.VisibleWhen is null || def.VisibleWhen(state));
        })
        .ToList();
}
