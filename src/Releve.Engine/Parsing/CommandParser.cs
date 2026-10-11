using Releve.Engine.Core;

namespace Releve.Engine.Parsing;

/// <summary>
/// Analyseur de commandes a vocabulaire controle (§6) : normalisation puis resolution des noms
/// dans la portee courante.
/// </summary>
public static class CommandParser
{
    public static ParseResult Parse(string input, IReadOnlyCollection<ObjectDef> scope)
    {
        var words = TextNormalizer.Normalize(input);
        if (words.Count == 0)
        {
            return ParseResult.Fail(ParseError.Empty);
        }

        // Direction seule (« Est ») ou « va [vers] est ».
        var directionAlone = DirectionAt(words, 0);
        if (directionAlone is not null)
        {
            return ParseResult.Ok(new Command(GameAction.Go, Direction: directionAlone));
        }

        if (!Vocabulary.Verbs.TryGetValue(words[0], out var action))
        {
            return ParseResult.Fail(ParseError.UnknownVerb);
        }

        var rest = words.Skip(1).ToList();

        // « va vers est », « aller a est »...
        if (action == GameAction.Go)
        {
            var afterVerb = rest;
            if (afterVerb.Count > 0 && TextNormalizer.IsPreposition(afterVerb[0]))
            {
                afterVerb = afterVerb.Skip(1).ToList();
            }

            var direction = DirectionAt(afterVerb, 0);
            if (direction is not null && afterVerb.Count == 1)
            {
                return ParseResult.Ok(new Command(GameAction.Go, Direction: direction));
            }

            if (afterVerb.Count == 0)
            {
                return ParseResult.Fail(ParseError.MissingObject);
            }
        }

        if (rest.Count == 0)
        {
            return action is GameAction.Look or GameAction.Inventory
                ? ParseResult.Ok(new Command(action))
                : ParseResult.Fail(ParseError.MissingObject);
        }

        // Preposition seule en tete : elle est ignoree.
        if (TextNormalizer.IsPreposition(rest[0]))
        {
            rest = rest.Skip(1).ToList();
            if (rest.Count == 0)
            {
                return ParseResult.Fail(ParseError.MissingObject);
            }
        }

        var split = 0;
        while (split < rest.Count && !TextNormalizer.IsPreposition(rest[split]))
        {
            split++;
        }

        var first = rest.Take(split).ToList();
        var second = rest.Skip(split + (split < rest.Count ? 1 : 0)).ToList();

        var firstTarget = Resolve(first, scope, out var candidates);
        if (firstTarget is null)
        {
            return candidates is null
                ? ParseResult.Fail(ParseError.UnknownObject)
                : ParseResult.Fail(ParseError.Ambiguous, candidates);
        }

        if (second.Count == 0)
        {
            return ParseResult.Ok(new Command(action, Target: firstTarget.Id));
        }

        var secondTarget = Resolve(second, scope, out candidates);
        if (secondTarget is null)
        {
            return candidates is null
                ? ParseResult.Fail(ParseError.UnknownObject)
                : ParseResult.Fail(ParseError.Ambiguous, candidates);
        }

        // Le premier nom est l'instrument, le second est la cible.
        return ParseResult.Ok(new Command(action, Target: secondTarget.Id, Instrument: firstTarget.Id));
    }

    private static Direction? DirectionAt(IReadOnlyList<string> words, int index) =>
        index < words.Count && Vocabulary.Directions.TryGetValue(words[index], out var d) ? d : null;

    private static ObjectDef? Resolve(
        IReadOnlyList<string> group,
        IReadOnlyCollection<ObjectDef> scope,
        out IReadOnlyList<string>? candidates)
    {
        candidates = null;
        var text = string.Join(" ", group);

        var aliases = scope
            .Select(def => (Def: def, Aliases: def.Aliases
                .Select(alias => string.Join(" ", TextNormalizer.Normalize(alias)))
                .Where(alias => alias.Length > 0)
                .ToArray()))
            .ToList();

        foreach (var (def, aliasList) in aliases)
        {
            if (aliasList.Contains(text))
            {
                return def;
            }
        }

        var matches = aliases
            .Where(e => e.Aliases.Any(alias => ContainsAllWords(alias, group)))
            .Select(e => e.Def)
            .ToList();

        if (matches.Count == 1)
        {
            return matches[0];
        }

        if (matches.Count > 1)
        {
            candidates = matches.Select(m => m.Name).ToList();
            return null;
        }

        return null;
    }

    private static bool ContainsAllWords(string alias, IReadOnlyList<string> group)
    {
        var aliasWords = alias.Split(' ');
        return group.All(g => aliasWords.Contains(g, StringComparer.Ordinal));
    }
}
