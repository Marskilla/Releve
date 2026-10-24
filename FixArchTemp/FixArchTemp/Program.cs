using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string path = @"D:\MesDocuments\_MesDocuments\Programmes\Releve\Docs\ArchitectureTechnique.md";
        string content = File.ReadAllText(path, System.Text.Encoding.UTF8);

        // Fix Inventory line in §9.3 - use regex for flexible matching
        content = Regex.Replace(content,
            @"- \*\*Inventaire\*\* : liste verticale de libellés précédés d'une icône de remplacement \(carré de 16 px\), titre « Inventaire : », défilement visuel \*\*PageHaut / PageBas\*\.",
            "- **Inventaire** : liste verticale de libellés précédés d'une icône de remplacement (carré de 16 px), titre « Inventaire : », défilement **Ctrl+Haut / Ctrl+Bas (sans Shift)** et **PageHaut / PageBas**.");

        // Fix Console line in §9.3 - the line with "Flèches haut/bas : rappel de l'historique des commandes saisies."
        content = Regex.Replace(content,
            @"Flèches haut/bas : rappel de l'historique des commandes saisies\.",
            "**Rappel des commandes saisies** : `Shift + Haut` / `Shift + Bas` (insère la commande dans la ligne de saisie, sans valider). **Historique LIFO** : toutes les commandes envoyées au moteur (saisie clavier ou raccourcis flèches) sont mémorisées ; Shift+Up remonte (plus anciennes), Shift+Down descend (plus récentes, jusqu'à ligne vide). Validation (Entrée) ajoute la commande en tête de pile et replace le pointeur. **Défilement visuel** : `PageHaut` / `PageBas`.");

        // Fix §14 point 3
        content = Regex.Replace(content,
            @"3\.\s*\*\*Touches de défilement\*\* : Page↑/Page↓ pour la console, Ctrl\+↑/↓ pour l'inventaire \(conflit possible, §15\)\.",
            "3. **Touches de défilement** : flèches seules = directions ; Shift+flèches = rappel historique commandes (LIFO) ; Ctrl+flèches = scroll inventaire ligne par ligne ; PageHaut/PageBas = scroll console et inventaire par page (plus de conflit, §15 résolu)."
        );

        // Fix §15.3
        content = Regex.Replace(content,
            @"3\.\s*\*\*Conflit possible Ctrl\+↑/↓\*\* : `ConsolePanel\.RecallHistory` ne teste pas Ctrl ; Ctrl\+↑/↓ \(défilement de l'inventaire\) rappelle aussi l'historique des commandes \(à confirmer en jeu\)\. L'architecture V0\.4 affirmait qu'il n'y avait aucun conflit\.",
            "3. **Conflit touches résolu** : séparation nette — flèches seules (directions), Shift+flèches (historique commandes LIFO), Ctrl+flèches (inventaire ligne par ligne), PageHaut/PageBas (scroll par page console/inventaire)."
        );

        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        Console.WriteLine("Done - regex fixes");
    }
}