using System.Collections.Generic;

namespace Releve.Engine.Core;

/// <summary>
/// Fonction pure de retour a la ligne (word wrap) pour les textes de la console et du visualiseur.
/// </summary>
public static class TextWrapper
{
    /// <summary>
    /// Coupe un texte en lignes ne depassant pas maxWidth.
    /// </summary>
    /// <param name="text">Texte a couper.</param>
    /// <param name="maxWidth">Largeur maximale en unites de mesure (pixels).</param>
    /// <param name="measureWidth">Fonction qui mesure la largeur d'une chaine.</param>
    /// <returns>Liste des lignes (les lignes vides sont conservees).</returns>
    public static IReadOnlyList<string> Wrap(string text, float maxWidth, System.Func<string, float> measureWidth)
    {
        var lines = new List<string>();

        // Normaliser les fins de ligne : \r\n et \r -> \n
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        foreach (var paragraph in normalized.Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var current = new System.Text.StringBuilder();
            foreach (var word in paragraph.Split(' '))
            {
                // Si le mot seul depasse maxWidth, le couper caractere par caractere
                if (measureWidth(word) > maxWidth)
                {
                    // D'abord, vider la ligne courante si elle n'est pas vide
                    if (current.Length > 0)
                    {
                        lines.Add(current.ToString());
                        current.Clear();
                    }

                    // Couper le mot caractere par caractere
                    var remaining = word;
                    while (remaining.Length > 0)
                    {
                        var charsToAdd = new System.Text.StringBuilder();
                        foreach (var c in remaining)
                        {
                            var testStr = charsToAdd.Length == 0 ? c.ToString() : charsToAdd.ToString() + c;
                            if (measureWidth(testStr) <= maxWidth)
                            {
                                charsToAdd.Append(c);
                            }
                            else
                            {
                                break;
                            }
                        }

                        lines.Add(charsToAdd.ToString());
                        remaining = remaining[charsToAdd.Length..];
                    }

                    continue;
                }

                var candidate = current.Length == 0 ? word : current + " " + word;
                if (measureWidth(candidate) <= maxWidth)
                {
                    current.Clear();
                    current.Append(candidate);
                    continue;
                }

                lines.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }

            lines.Add(current.ToString());
        }

        // Si le texte est vide, retourner une liste avec une seule chaine vide
        if (lines.Count == 0)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }
}