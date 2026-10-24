using Releve.Engine.Core;

namespace Releve.Engine.Parsing;

/// <summary>
/// Analyseur de commandes a vocabulaire controle (§6) : normalisation puis resolution des noms
/// dans la portee courante.
/// </summary>
public static class CommandParser
{
    /// <summary>
    /// Analyse une saisie. <paramref name="exitWords"/> associe, optionnellement, chaque mot de
    /// sortie de la piece courante a sa direction (les appels existants sans ce parametre restent
    /// valides).
    /// </summary>
    public static ParseResult Parse(string input, IReadOnlyCollection<ObjectDef> scope,
        IReadOnlyDictionary<string, Direction>? exitWords = null)
    {
        var words = TextNormalizer.Normalize(input);
        if (words.Count == 0)
        {
            return ParseResult.Fail(ParseError.Empty);
        }

        // Direction seule (« Est », « n ») : déplacement SEULEMENT si c'est le seul mot.
        var directionAlone = DirectionAt(words, 0);
        if (directionAlone is not null && words.Count == 1)
        {
            return ParseResult.Ok(new Command(GameAction.Go, Direction: directionAlone));
        }

        if (!Vocabulary.Verbs.TryGetValue(words[0], out var action))
        {
            return ParseResult.Fail(ParseError.UnknownVerb);
        }

        var rest = words.Skip(1).ToList();

        // Verbes de deplacement : direction exprimee, puis mots de sortie, puis rejet.
        if (action == GameAction.Go)
        {
            return ParseGo(rest, words[0], exitWords);
        }

        if (rest.Count == 0)
        {
            return action is GameAction.Look or GameAction.Inventory or GameAction.Read
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

        // J3.5 : pour Look seulement, si tous les mots restants (hors prepositions) sont dans RoomWords
        // et qu'au moins un n'est pas "moi" -> Look sans cible.
        if (action == GameAction.Look)
        {
            var nonPrepWords = rest.Where(w => !TextNormalizer.IsPreposition(w)).ToList();
            if (nonPrepWords.Count > 0 && nonPrepWords.All(w => Vocabulary.RoomWords.Contains(w))
                && nonPrepWords.Any(w => w != "moi"))
            {
                return ParseResult.Ok(new Command(GameAction.Look));
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

    private static ParseResult ParseGo(IReadOnlyList<string> rest, string verb,
        IReadOnlyDictionary<string, Direction>? exitWords)
    {
        // « va vers est », « aller a est » : la direction doit etre le seul mot restant.
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

        // Mots de sortie : le verbe lui-meme compte s'il figure dans la liste,
        // puis les mots restants (prepositions ignorees).
        var exitWordCandidates = new List<string>();
        if (exitWords is not null && exitWords.TryGetValue(verb, out var _))
        {
            exitWordCandidates.Add(verb);
        }

        foreach (var w in rest)
        {
            if (!TextNormalizer.IsPreposition(w))
            {
                exitWordCandidates.Add(w);
            }
        }

        if (exitWordCandidates.Count > 0)
        {
            if (exitWordCandidates.All(w => exitWords is not null && exitWords.TryGetValue(w, out var _)))
            {
                var dirs = exitWordCandidates.Select(w => exitWords![w]).Distinct().ToList();
                if (dirs.Count == 1)
                {
                    return ParseResult.Ok(new Command(GameAction.Go, Direction: dirs[0]));
                }
            }
        }

        if (rest.Count == 0)
        {
            // « va », « vas », « aller », « allez », « avance », « avancer » seuls → MissingObject.
            if (verb == "va" || verb == "vas" || verb == "aller" || verb == "allez" || verb == "avance" || verb == "avancer")
            {
                return ParseResult.Fail(ParseError.MissingObject);
            }

            // Nouveau verbe de deplacement sans mot de sortie correspondant.
            return ParseResult.Ok(new Command(GameAction.Go, Direction: null));
        }

        // Verbe de deplacement avec des mots qui ne sont pas tous des mots de sortie :
        // le moteur repondra « Vous ne pouvez pas aller par là. »
        return ParseResult.Ok(new Command(GameAction.Go, Direction: null));
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
