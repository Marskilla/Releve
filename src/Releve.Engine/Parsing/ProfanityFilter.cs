using System.Collections.Generic;

namespace Releve.Engine.Parsing;

/// <summary>Filtre de grossièretés : mots isolés et expressions (suites contiguës) déjà normalisés.</summary>
public static class ProfanityFilter
{
    private static readonly HashSet<string> _words = new(StringComparer.Ordinal)
    {
        // Mots isolés
        "merde", "merdes", "merdique", "merdiques", "merdeux", "merdeuse", "merdier",
        "emmerde", "emmerdes", "emmerder", "emmerdant", "emmerdante", "emmerdeur", "emmerdeuse",
        "putain", "putains", "pute", "putes", "putasse", "putasses",
        "con", "cons", "conne", "connes", "connard", "connards", "connasse", "connasses", "connerie", "conneries", "conard", "conasse",
        "encule", "encules", "enculee", "enculees", "enculer", "enculeur",
        "enfoire", "enfoires", "enfoiree",
        "salaud", "salauds", "salope", "salopes", "saloperie", "saloperies", "salopard", "salopards",
        "bordel", "bordels",
        "foutre",
        "chier", "chie", "chies", "chiant", "chiante", "chiants", "chiantes", "chiasse", "chieur", "chieuse",
        "couille", "couilles", "couillon", "couillons", "burne", "burnes",
        "bite", "bites", "zob",
        "branler", "branle", "branleur", "branleuse", "branlette", "branlettes",
        "nique", "niques", "niquer",
        "baise", "baises", "baiser",
        "pisse", "pisser",
        "pouffiasse", "poufiasse", "petasse", "petasses", "garce", "garces",
        "pourriture", "crevure", "raclure",
        "abruti", "abrutie", "abrutis", "cretin", "cretine", "cretins", "debile", "debiles", "idiot", "idiote", "idiots", "imbecile", "imbeciles",
        "trouduc",
        "fdp", "ntm"
    };

    private static readonly List<string[]> _expressions = new()
    {
        // Expressions (suites de mots)
        new[] { "va", "te", "faire", "foutre" },
        new[] { "va", "te", "faire", "enculer" },
        new[] { "va", "te", "faire", "voir" },
        new[] { "va", "chier" },
        new[] { "fait", "chier" },
        new[] { "faire", "chier" },
        new[] { "ta", "gueule" },
        new[] { "fils", "pute" },
        new[] { "fille", "pute" },
        new[] { "tete", "noeud" },
        new[] { "trou", "cul" },
        new[] { "leche", "cul" },
        new[] { "lechecul" },
        new[] { "ras", "cul" }
    };

    /// <summary>Vrai si un mot isolé est dans la liste, ou si une expression apparaît comme suite contiguë de mots.</summary>
    public static bool Contains(IReadOnlyList<string> normalizedWords)
    {
        // Mots isolés
        foreach (var word in normalizedWords)
        {
            if (_words.Contains(word))
            {
                return true;
            }
        }

        // Expressions (suites contiguës)
        foreach (var expr in _expressions)
        {
            if (expr.Length > normalizedWords.Count)
            {
                continue;
            }

            for (int i = 0; i <= normalizedWords.Count - expr.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < expr.Length; j++)
                {
                    if (normalizedWords[i + j] != expr[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return true;
                }
            }
        }

        return false;
    }
}