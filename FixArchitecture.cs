using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string path = @"D:\MesDocuments\_MesDocuments\Programmes\Releve\Docs\ArchitectureTechnique.md";
        string content = File.ReadAllText(path, System.Text.Encoding.UTF8);

        // Replace the console description line
        string oldConsole = "Flèches haut/bas : rappel de l'historique des commandes saisies.";
        string newConsole = "**Rappel de l'historique des commandes** : `Shift + Haut` / `Shift + Bas` (teste `IsKeyDown` pour Shift, `IsKeyPressed` pour la flèche).";
        content = content.Replace(oldConsole, newConsole);

        // Replace the 9.4 section
        string oldSection = @"### 9.4 Touches de défilement

- Page précédente / Page suivante : défilement de l'historique de la console (et du visualiseur de texte, §9.8).
- Ctrl + Haut / Ctrl + Bas : défilement de l'inventaire.

Le Ctrl est testé avec `IsKeyDown`, les flèches avec `IsKeyPressed`. Un conflit possible avec le rappel de l'historique de la console (la flèche Haut y rappelle l'historique) est signalé au §15.";

        string newSection = @"### 9.4 Touches de défilement

- **Flèches Haut/Bas/Gauche/Droite** : déplacement dans le jeu (Nord/Sud/Ouest/Est), gérées par `GameApp.UpdateGame` via `IsKeyPressed` / `IsKeyPressedRepeat`.
- **Ctrl + Haut / Ctrl + Bas** : défilement de l'inventaire.
- **Shift + Haut / Shift + Bas** : rappel de l'historique de la console (commandes saisies précédemment).
- Page précédente / Page suivante : défilement de l'historique de la console (et du visualiseur de texte, §9.8).";

        content = content.Replace(oldSection, newSection);

        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        Console.WriteLine("Done");
    }
}