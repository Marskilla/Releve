using System.Collections.Generic;

namespace Releve.Engine.Core;

/// <summary>Sélecteur de réponses : tire au hasard sans répéter le même message deux fois de suite pour une même liste (J4).</summary>
public sealed class ReplyPicker
{
    private readonly Random _random;
    private readonly Dictionary<string, int> _lastIndex = new();

    public ReplyPicker(Random random)
    {
        _random = random;
    }

    /// <summary>Choisit un message dans la liste, jamais le même que le précédent pour cette liste.</summary>
    public string Pick(string[] list, string listName)
    {
        if (list.Length == 1)
        {
            return list[0];
        }

        int index;
        do
        {
            index = _random.Next(list.Length);
        }
        while (_lastIndex.TryGetValue(listName, out var last) && last == index);

        _lastIndex[listName] = index;
        return list[index];
    }
}