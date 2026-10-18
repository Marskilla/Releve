# Jeu « Relève » — Compte rendu du PoC V2

> Compte rendu — version 0.1
> **État : PoC V2 terminé.** Le contenu décrit par « ImplementationPOC2V1.0.md » est livré, jouable de bout en bout dans le jeu réel, build et tests au vert (103 tests).
> Documents liés : `ImplementationPOC2V1.0.md` (le plan, ses §4 à §7 servent de référence aux tableaux ci-dessous), `ArchitectureTechniqueV0.3.md` mis à jour en V0.4, `NoteIntentionJeuAventureV5.md` (comportement), `UniversScenarioLoreJeuAventureV0.7.md` (lieux et lore), `AGENTS.md` (règles de travail du dépôt).

## 1. Résultat

Le PoC V2 demande de consolider le moteur et l’interface sur le même contenu de lore que le PoC V1 : corriger les défauts constatés au test du V1, enrichir l’interface et le vocabulaire, introduire des composants réutilisables (visualiseur de texte), et valider le scénario de référence V2 de bout en bout. C’est fait :

- **Moteur corrigé et enrichi** : `Go` sans direction ne plante plus (« Vous ne pouvez pas aller par là. »), action `Read` et nouveaux verbes (Talk, Use, Read, Go), mots de sortie sur les sorties (`Exit.Words`), « Ouvre » sur les portes avec réponses conditionnées, « Use » sans instrument répond « Précisez sur quoi l’utiliser. », « Read » sans cible répond « Il n’y a rien à lire ici. »
- **Interface enrichie** : console avec deux types de ligne (saisie/réponse) et couleurs distinctes, boutons de défilement cliquables (haut/bas), panneau des flèches en croix avec flèches pleines/creuses et ordre de sommets corrigé, image du lieu étirée sur toute la zone utile, menu à trois entrées (Continuer/Nouvelle partie/Aide) avec titre et fond d’écran, visualiseur de texte modal superposé.
- **Sauvegarde automatique** après toute commande à `StateChanged` (déplacement inclus), plus sauvegarde de fermeture dans un bloc `finally`.
- **Tests** : 103 tests verts (83 hérités du PoC V1 + 20 nouveaux), dont 20 propres aux évolutions du PoC V2.
- **Scénario de référence V2** (§7.2 du plan) rejoué dans le jeu réel : corbeille, « va porte », « utilise badge », mots de sortie, flèches, couleurs, image étirée, menu, aide, sauvegarde après « ouvre armoire » puis arrêt brutal.
- **Outillage d’automatisation** hors build recalibré (`Get-ReleveArrowStates` sur la nouvelle géométrie des flèches, limites documentées dans `agentTools/README.md` et architecture §16).

## 2. Travail réalisé

### 2.1 Moteur (`src/Releve.Engine/`)

| Élément | Modification | Test associé |
|---|---|---|
| `GameAction.Read` | Nouvelle action dans l’énumération | `Read_Sans_Cible_Rien_A_Lire`, `Read_Afficheur_Et_Machines_Pareil_Que_Look` |
| `Vocabulary` | Verbes Talk (interroge/demande), Use (pousse/actionne), Read (lis/lire/lisez), Go (sors/sortir/monte/monter/descends/descendre/entre/entrer/rentre/rentrer/retourne/retourner) | `Verbes_Talk_Ajoutes`, `Verbes_Use_Ajoutes`, `Verbes_Go_Ajoutes` |
| `Exit.Words` | Liste optionnelle de mots de sortie par sortie | `Mots_De_Sortie_Prioritaires_Sur_Alias_Pour_Go`, `Nouveaux_Verbes_De_Deplacement` |
| `CommandParser.Parse` | Paramètre optionnel `exitWords` ; règle de reconnaissance des mots de sortie prioritaires sur les alias pour les verbes Go | `ExitWordsTests` (7 cas) |
| `GameEngine.Execute` | `Go` sans direction → « Vous ne pouvez pas aller par là. » sans exception | `Go_Sans_Direction_Ne_Leve_Pas_Exception` |
| `GameEngine.DefaultBehaviour` | `Read` sans cible → « Il n’y a rien à lire ici. » ; `Use` sans instrument → « Précisez sur quoi l’utiliser. » | `Read_Sans_Cible_Rien_A_Lire`, `Use_Sans_Instrument_Precisez_Cible` |
| `Handle` (pièces) | « Ouvre » sur GlassDoor/StairsDoor/TechDoor/Elevator avec réponses conditionnées | `Ouvre_Portes_Selon_Etat` |
| `BedroomObjects` | Corbeille : description selon l’emplacement du sac poubelle | `Corbeille_Selon_Emplacement_Sac` |

**A1 (écart d’alias « prends badge »)** : accepté comme dans le PoC V1, documenté par le test `Badge_Is_Reachable_Only_After_Opening_The_Wardrobe`. Aucun changement de moteur.

### 2.2 Interface (`src/Releve.Game/Ui/`)

| Élément | Modification | Test / Vérification |
|---|---|---|
| `ConsolePanel` | Deux types de ligne (saisie joueur / réponse jeu), couleurs `ConsoleInput`/`ConsoleResponse` ; boutons cliquables haut/bas (3 lignes par clic, répétition en maintien, grisé quand impossible) | Jeu réel (§7.2, point 7 : couleurs distinctes) |
| `Palette` | `ConsoleInput`, `ConsoleResponse`, `ArrowOpen`, `ArrowBlocked` | — |
| `ArrowsPanel` | Disposition en croix centrée ; Nord/Sud sur la ligne centrale, Ouest/Est sur la ligne du milieu ; ordre de sommets anti-horaire ; flèche pleine (`Open`) vs flèche creuse (`Blocked`) | Jeu réel (§7.2, point 6 : disposition en croix, flèches pleines/creuses) |
| `RoomViewPanel` | Image étirée pour remplir exactement `ViewBounds` ; cadre redessiné après l’image | Jeu réel (§7.2, point 8 : plus de bande vide) |
| `Fonts` | Seconde taille de police (titre ~64 px) avec `MeasureTitleWidth`/`DrawTitle` | Menu (§7.2, point 9) |
| `MenuScreen` | Trois entrées (Continuer, Nouvelle partie, Aide), retour typé `MenuChoice`, navigation cyclique, « Continuer » grisé sans sauvegarde | Jeu réel (§7.2, point 9) |
| `TextViewer` (nouveau) | Visualiseur modal, superposé, opaque, retour à la ligne automatique, défilement Page/↑/↓/Début/Fin, bouton « Fermer », fermeture par Échap/Entrée | Jeu réel (§7.2, point 9) |

### 2.3 Sauvegarde (`src/Releve.Game/GameApp.cs`)

| Élément | Modification | Test / Vérification |
|---|---|---|
| `Submit` | Sauvegarde après toute commande à `StateChanged` (déplacement inclus), uniquement si une partie est en cours | `Save_Then_Reload_Restores_Room_Inventory_And_States` (hérité) + jeu réel (§7.2, point 10) |
| `Run` / bloc `finally` | Sauvegarde de fermeture déplacée dans un `finally` pour couvrir aussi une sortie sur exception | Jeu réel (§7.2, point 10) |

### 2.4 Outillage d’automatisation (`agentTools/`)

| Élément | Modification |
|---|---|
| `ReleveGameTools.psm1` | `Get-ReleveArrowStates` recalibré sur la nouvelle géométrie des flèches (Nord ligne 1, Ouest/Est ligne 2, Sud ligne 3) |
| `README.md` | Documentation de la limite de validation humaine (Retour arrière avec vrai clavier, clics souris) ; couleurs `ArrowOpen`/`ArrowBlocked` |
| `Run-Hidden.ps1` | Inchangé (déjà présent) |
| `Send-Commands.ps1` | Inchangé (déjà présent) |

### 2.5 Documentation

| Fichier | Modification |
|---|---|
| `Docs/ArchitectureTechniqueV0.3.md` | Passé en V0.4 : §6.6 (écart d’alias accepté), §7.3 (Go sans direction, Read sans cible, Use sans instrument), §8 (sauvegarde sur changement d’état + fermeture), §9.2/§9.3 (image étirée, flèches en croix, console avec types de ligne et boutons), §15 (écarts mis à jour), §16 (limites automatisation clavier, recalibrage zones flèches) |
| `AGENTS.md` | Contrat d’assets complété (`menu/`, `texts/`), mention du PoC V2, limite de l’automatisation clavier (validation humaine §7.3), chemins des documents déplacés |
| `README.md` | Section Sauvegarde complétée, liste des assets (police, images, menu, textes, audio), liens vers les docs déplacées |

## 3. Vérification dans le jeu réel

Le scénario de référence V2 (§7.2 du plan) a été joué dans le jeu lancé, depuis une nouvelle partie. Les vérifications ci-dessous ont été effectuées par observation directe du rendu :

| # | Vérification | Résultat |
|---|---|---|
| 1 | Chambre : « regarde corbeille » (avec sac), « prends sac », « regarde corbeille » (vide) | Description de la corbeille adaptée |
| 2 | « va porte » → « Vous ne pouvez pas aller par là. » sans plantage | Aucune exception |
| 3 | « utilise badge » → « Précisez sur quoi l’utiliser. » | Message correct |
| 4 | « sors » depuis la chambre (lecteur déverrouillé) → couloir ; « monte » → bloqué ; escalier débloqué → accueil | Mots de sortie fonctionnels |
| 5 | « lis afficheur » → la date affichée | Action Read fonctionnelle |
| 6 | Fiche patient, puis « sors » → rue ; « entre » → accueil | Mots de sortie (rue, hôpital) fonctionnels |
| 7 | Flèches : disposition en croix, flèches pleines/creuses correctes dans les quatre lieux | Disposition conforme au plan |
| 8 | Couleurs : saisie et réponses en couleurs distinctes | Deux couleurs de ligne distinctes |
| 9 | Image du lieu : sur la chambre, le couloir et l’accueil, plus aucune bande vide entre l’image et le cadre | Image étirée sur `ViewBounds` |
| 10 | Menu : image de fond, titre en grand, trois entrées ; Aide s’ouvre (plein écran), défile, se ferme ; retour au menu avec la même sélection | Menu à trois entrées fonctionnel |
| 11 | Sauvegarde : après « ouvre armoire » (sans changement de pièce), arrêt brutal du processus, relance, « Continuer » : armoire ouverte | Sauvegarde automatique + fermeture OK |

**États de flèche dans les quatre lieux** (observés à l’écran) :

| Lieu | Nord | Ouest | Sud | Est |
|---|---|---|---|---|
| Chambre | absente | absente | absente | bloquée → ouverte (après déverrouillage) |
| Couloir | bloquée | ouverte | absente | bloquée (sans destination) |
| Accueil | bloquée (sans destination) | bloquée → ouverte (après borne) | ouverte | absente |
| Rue | absente | absente | absente | ouverte |

**Textes et accents** : la fiche patient, les guillemets français, l’apostrophe typographique et les accents s’affichent correctement dans la console, le menu et le visualiseur (caractères précomposés, police DejaVu Sans). Aucun caractère décomposé dans le contenu livré (recherche `[\u0300-\u036f]` négative sur les textes de jeu).

## 4. Écart constaté par rapport au document d’implémentation

Un seul écart, déjà signalé et **documenté dans un test** plutôt que corrigé silencieusement (décision A1 du plan, acceptée par l’utilisateur) :

- Attendu (§4.1 du PoC V1, étape 3) : « prends badge » avant « ouvre armoire » doit répondre « objet inconnu ».
- Observé : le badge est bien invisible tant que l’armoire est fermée, mais l’alias « badge » correspond partiellement à « le lecteur de badge », présent dans la même pièce. Le moteur résout donc le lecteur et refuse la prise : « Vous ne pouvez pas prendre cela. »

Cause : la règle de résolution partielle de l’analyseur (§6.6 de l’architecture) : un objet invisible reste partiellement identifiable par ses alias. **Point à trancher** : corriger le moteur (un objet invisible ne devrait pas entrer dans les candidats) ou accepter le comportement. Le choix appartient à l’utilisateur ; rappelé au §15 de l’architecture V0.4.

## 5. Critères d’acceptation (plan §8)

| Critère | État | Preuve |
|---|---|---|
| Scénario de référence V2 de bout en bout dans le jeu réel | atteint | §3 ci-dessus, points 1 à 11 |
| Scénario V1 non régressé | atteint | `dotnet test` : tous les tests PoC V1 verts |
| Build et tests au vert | atteint | `dotnet build` : 0 erreur ; `dotnet test` : 103/103 |
| Accents corrects partout (architecture §2) | atteint | §3, recherches de caractères décomposés négatives |
| Documentation mise à jour (architecture V0.4, AGENTS.md, README.md) | atteint | §2.5 |
| Compte rendu du PoC V2 rédigé | atteint | ce document |

## 6. Ce qui reste approximatif, points à valider

| # | Point | Nature | Statut |
|---|---|---|---|
| 1 | Textes provisoires du lot A (corbeille, portes, etc.) et de C3/C4 (aide) | Contenu | À relire à l’oreille / à confirmer |
| 2 | Mots de sortie (tableau A3 du plan) | Contenu | À confirmer par l’utilisateur |
| 3 | Couleurs de la console et des flèches, taille et pas de défilement des boutons | Affichage | Validation humaine (§7.3 du plan) |
| 4 | Indicateurs du visualiseur ; fermeture par Échap ou Entrée | Affichage | Validation humaine (§7.3 du plan) |
| 5 | Rendu de l’image du lieu étirée (déformation ~2 %) et du menu (lisibilité du texte sur l’image de fond) | Affichage | Validation humaine (§7.3 du plan) |
| 6 | Touche Retour arrière avec un vrai clavier (répétition en maintien) | Saisie | Validation humaine (§7.3 du plan) — agentTools ne peut pas la valider |
| 7 | Clics souris sur les boutons de défilement de la console et sur « Fermer » du visualiseur | Souris | Validation humaine (§7.3 du plan) — agentTools ne peut pas la valider |
| 8 | Rue provisoire sans lore ni image (`Street` sans PNG → rectangle de remplacement) | Contenu | Prévu par l’architecture, à remplacer quand le §5.4 du lore sera rédigé |

## 7. Classification des validations (selon §7.3 du plan)

**Vérifié dans le jeu réel** (par l’automatisation et l’observation directe) :
- Scénario V2 complet (11 vérifications du tableau §3)
- États de flèche dans les quatre lieux
- Image étirée sans bande vide
- Menu à trois entrées, fond, titre, aide plein écran
- Sauvegarde automatique après « ouvre armoire » puis arrêt brutal
- Accents et caractères typographiques

**Vérifié que dans les tests** (moteur, non visualisable à l’écran) :
- `Go` sans direction sans exception
- `Read` sans cible / `Use` sans instrument
- Mots de sortie prioritaires sur les alias pour Go
- `Exit.Words` cohérence (uniques, non vides, pas de direction cardinale)
- Corbeille selon l’emplacement du sac
- Nouveau vocabulaire (Talk, Use, Read, Go)
- `WorldConsistencyTests` avec mots de sortie

**Attendant une validation humaine** (§7.3 du plan) :
- Retour arrière avec répétition en maintien sur vrai clavier
- Clics souris sur boutons de défilement et « Fermer » du visualiseur
- Lisibilité des couleurs, flèches, menu
- Aspect de l’image étirée et du fond de menu

## 8. Pour rejouer la vérification

```powershell
# 1. lancer le jeu
dotnet run --project src/Releve.Game

# 2. menu sans sauvegarde : supprimer Documents/Releve/save.dat, puis relancer

# 3. depuis un processus SANS FENETRE (agentTools) :
Import-Module .\agentTools\ReleveGameTools.psm1
Wait-ReleveGameWindow
Send-ReleveGameCommands -CommandFile .\agentTools\scenarios\poc-reference.txt

# 4. vérifier sans quitter le jeu
Save-ReleveGameWindowShot -Path .\shot.png
Get-ReleveArrowStates -PngFile .\shot.png
```

La recette complète et les pièges d’injection clavier sont dans `agentTools/README.md`.

## 9. Pour la suite (hors périmètre, à titre de mémoire)

Déclenchement du visualiseur depuis le moteur (lettre des parents, crédits), vrai texte d’aide, molette de souris, dialogues plus riches, variantes d’image selon l’état d’un objet, support Linux/macOS.
