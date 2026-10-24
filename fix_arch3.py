with open(r'D:\MesDocuments\_MesDocuments\Programmes\Releve\Docs\ArchitectureTechnique.md', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the console description line
old_console = "Flèches haut/bas : rappel de l'historique des commandes saisies."
new_console = "**Rappel de l'historique des commandes** : `Shift + Haut` / `Shift + Bas` (teste `IsKeyDown` pour Shift, `IsKeyPressed` pour la flèche)."
content = content.replace(old_console, new_console)

# Replace the 9.4 section
old_section = """### 9.4 Touches de défilement

- Page précédente / Page suivante : défilement de l'historique de la console (et du visualiseur de texte, §9.8).
- Ctrl + Haut / Ctrl + Bas : défilement de l'inventaire.

Le Ctrl est testé avec `IsKeyDown`, les flèches avec `IsKeyPressed`. Un conflit possible avec le rappel de l'historique de la console (la flèche Haut y rappelle l'historique) est signalé au §15."""

new_section = """### 9.4 Touches de défilement

- **Flèches Haut/Bas/Gauche/Droite** : déplacement dans le jeu (Nord/Sud/Ouest/Est), gérées par `GameApp.UpdateGame` via `IsKeyPressed` / `IsKeyPressedRepeat`.
- **Ctrl + Haut / Ctrl + Bas** : défilement de l'inventaire.
- **Shift + Haut / Shift + Bas** : rappel de l'historique de la console (commandes saisies précédemment).
- Page précédente / Page suivante : défilement de l'historique de la console (et du visualiseur de texte, §9.8)."""

content = content.replace(old_section, new_section)

with open(r'D:\MesDocuments\_MesDocuments\Programmes\Releve\Docs\ArchitectureTechnique.md', 'w', encoding='utf-8') as f:
    f.write(content)

print('Done')