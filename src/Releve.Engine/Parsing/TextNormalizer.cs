namespace Releve.Engine.Parsing;

/// <summary>
/// Normalisation identique pour la saisie et pour les alias (§6.2).
/// </summary>
public static class TextNormalizer
{
    public const char Apostrophe = '\'';

    public static readonly string TypographicApostrophe = "\u2019";

    private static readonly HashSet<string> Articles = new(StringComparer.Ordinal)
    {
        "le", "la", "les", "l", "un", "une", "des", "du", "de", "d",
    };

    // Prepositions : les formes non canoniques sont ramenees a une forme unique (« a »).
    private static readonly Dictionary<string, string> Prepositions = new(StringComparer.Ordinal)
    {
        ["sur"] = "sur",
        ["avec"] = "avec",
        ["a"] = "a",
        ["au"] = "a",
        ["aux"] = "a",
        ["dans"] = "dans",
        ["vers"] = "vers",
        ["contre"] = "contre",
    };

    public static bool IsPreposition(string word) => Prepositions.ContainsKey(word);

    public static string CanonicalPreposition(string word) =>
        Prepositions.TryGetValue(word, out var c) ? c : word;

    /// <summary>Decoupe et normalise un texte en mots exploitables.</summary>
    public static IReadOnlyList<string> Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var lower = text.ToLowerInvariant()
            .Replace("œ", "oe")
            .Replace("æ", "ae");

        var decomposed = lower.Normalize(System.Text.NormalizationForm.FormD);
        var cleaned = new System.Text.StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (c is Apostrophe or '\u2019')
            {
                cleaned.Append(' '); // "l'armoire" -> "l armoire"
                continue;
            }

            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue; // accent supprime
            }

            if (char.IsLetterOrDigit(c) || c == ' ')
            {
                cleaned.Append(c);
            }
            // le reste (ponctuation, symboles) est supprime
        }

        var words = cleaned.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var kept = new List<string>(words.Length);
        foreach (var word in words)
        {
            if (Articles.Contains(word))
            {
                continue; // articles supprimes
            }

            kept.Add(CanonicalPreposition(word));
        }

        return kept;
    }
}
