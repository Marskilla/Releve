$content = Get-Content 'D:\MesDocuments\_MesDocuments\Programmes\Releve\Docs\ArchitectureTechnique.md' -Raw -Encoding UTF8

$oldConsole = "Flèches haut/bas : rappel de l'historique des commandes saisies."
$newConsole = "**Rappel de l'historique des commandes** : `Ctrl + Haut` / `Ctrl + Bas` (teste `IsKeyDown` pour Ctrl, `IsKeyPressed` pour la flèche)."
$content = $content.Replace($oldConsole, $newConsole)

$oldSection = @"
### 9.4 Touches de défilement

- Page précédente / Page suivante : défilement de l'historique de la console (et du visualiseur de texte, §9.8).
- Ctrl + Haut / Ctrl + Bas : défilement de l'inventaire.

Le Ctrl est testé avec `IsKeyDown`, les flèches avec `IsKeyPressed`. Un conflit possible avec le rappel de l'historique de la console (la flèche Haut y rappelle l'historique) est signalé au §15.
"@

$newSection = @"
### 9.4 Touches de défilement

- **Flèches Haut/Bas/Gauche/Droite** : déplacement dans le jeu (Nord/Sud/Ouest/Est), gérées par `GameApp.UpdateGame` via `IsKeyPressed` / `IsKeyPressedRepeat`.
- **Ctrl + Haut / Ctrl + Bas** : défilement de l'inventaire **et** rappel de l'historique de la console (commandes saisies précédemment).
- Page précédente / Page suivante : défilement de l'historique de la console (et du visualiseur de texte, §9.8).
"@

$content = $content.Replace($oldSection, $newSection)

Set-Content 'D:\MesDocuments\_MesDocuments\Programmes\Releve\Docs\ArchitectureTechnique.md' -Value $content -Encoding UTF8
Write-Host 'Done'