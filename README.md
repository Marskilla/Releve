# Relève

Jeu d'aventure textuel et graphique en C# : une console de texte pour interagir, une vue du lieu, des flèches de déplacement et un inventaire. **Version affichée : v0.3** (PoC V3 — terminé, 128 tests).

## Fonctionnalités principales (PoC V3)

- **Moteur pur** (`Releve.Engine`) : analyseur de commandes français, états objets/pièces, événements, sauvegarde chiffrée AES-256-GCM.
- **Interface raylib** (`Releve.Game`) : console enrichie (scroll clavier + historique LIFO), vue de lieu avec zones d'image actives selon l'état (`*_AltState.png`), inventaire, menu principal cliquable à la souris, fondu au démarrage.
- **Raccourcis clavier** : flèches seules = déplacement ; **Shift+flèches** = rappel historique commandes (LIFO, terminal-like) ; **Ctrl+flèches** = scroll inventaire ; **PageUp/PageDown** = scroll console.
- **Visualiseur de texte** (`TextViewer`) : scroll souris avec indicateurs cliquables (30×30 px, 3 lignes/clic), survol stylé, anti-clic fantôme.
- **Réponses variées** : messages neutres/humoristiques selon contexte, filtre grossièretés.
- **4 lieux, 22 objets, 8 événements** — monde PoC cohérent (chambre, couloir technique, accueil hôpital, rue).

## Structure

| Chemin | Rôle |
|---|---|
| `src/Releve.Engine` | Moteur : monde, analyseur de commandes, états, sauvegarde. Aucune dépendance à l'affichage ou au son. |
| `src/Releve.Game` | Interface raylib : fenêtre, disposition, console, menu, audio. |
| `tests/Releve.Engine.Tests` | Tests xUnit du moteur (cohérence du monde, analyse, sauvegarde) — **128 tests**. |
| `src/Releve.Game/assets` | Ressources : police (`fonts/`), images de lieux (`rooms/`) + variantes `*_AltState.png`, fond de menu (`menu/`), texte d'aide (`texts/`), audio (`audio/`). |

## Prérequis

- SDK .NET 8 (`dotnet --version` → 8.x). Vérifier avec `dotnet --list-sdks`.
- Aucune image ni fichier audio n'est requis : le jeu fonctionne sans.

## Commandes

```powershell
dotnet build                       # compiler la solution complète
dotnet test                        # exécuter les tests
dotnet run --project src/Releve.Game   # lancer le jeu
dotnet publish -c Release -r win-x64 --self-contained   # publier pour Windows x64
```

Le dossier de publication contient l'exécutable, la bibliothèque native `raylib.dll` et les assets.

## Police

La police fournie dans `assets/fonts` est **DejaVu Sans 2.37**, libre de droits (licence Bitstream Vera / domaine public pour les modifications, voir `DejaVuSans-LICENSE.txt`). Elle couvre l'intégralité des caractères accentués du français (œ, Œ, guillemets, points de suspension, tirets). Licence incluse dans le dossier.

## Sauvegarde

La sauvegarde unique est écrite :
1. **À la fermeture du jeu** (bloc `finally`, couvre aussi les sorties sur exception) ;
2. **Après toute commande modifiant l'état** (déplacement inclus) via `StateChanged`.

Le fichier est `Documents/Releve/save.dat`, chiffré (AES-256-GCM). Il s'agit d'une obfuscation contre l'édition manuelle, pas d'une protection.

## Développement

- `Docs/ArchitectureTechnique.md` (V0.6) — architecture technique (reflète le code livré après PoC V3) : structure, modèle de domaine, analyseur, sauvegarde, couche raylib, tests, automatisation.
- `Docs/NoteIntentionJeuAventure.md` (V6) — intention de jeu (interface et comportement) ; elle prévaut sur le document d'architecture en cas de désaccord.
- `Docs/POC_V1/ImplementationPOCV0.1.md` — plan du PoC V1 (implémenté).
- `Docs/POC_V1/CompteRenduPOCV0.1.md` — compte rendu du PoC V1 (4 lieux, 22 objets, 83 tests).
- `Docs/POC_V2/ImplementationPOC2V1.0.md` — plan d'implémentation du PoC V2.
- `Docs/POC_V2/CompteRenduPOC2V0.1.md` — compte rendu du PoC V2 (menu, fond, aide, audio, police).
- `Docs/POC_V3/ImplementationPOC_03.md` — plan du PoC V3 (implémenté, jalons J0–J8).
- `Docs/POC_V3/CompteRenduPOC_03.md` — compte rendu du PoC V3 (128 tests, corrections post-livraison, raccourcis clavier, TextViewer souris).
- `AGENTS.md` — consignes de travail pour les sessions de développement assisté.
