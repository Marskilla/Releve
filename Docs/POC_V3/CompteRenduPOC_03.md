# Compte rendu du PoC V3 — Relève

**Date** : 2026-10-24  
**Version architecture** : 0.6  
**Tests** : 103 → 128 (+25)

---

## 1. Résultat

Tous les jalons J0 à J8 ont été traités dans l'ordre. `dotnet build` et `dotnet test` sont verts à chaque jalon.

| Jalon | Contenu | Tests ajoutés | Total |
|---|---|---|---|
| J0 | Relecture sans modification, vérification des constats [code] C1–C8 | 0 | 103 |
| J1 | Textes et encodage (apostrophes typographiques, déduplication messages, « blouse » → « chemise d’hôpital ») | 2 (`EncodingTests`) | 105 |
| J2 | Analyseur et comportements (direction seule = 1 mot, verbes seuls étendus, `Use` conditionné à l'inventaire, `Read` découplé) | 0 (tests adaptés) | 105 |
| J3 | Chambre : message d'accueil unique, `DetailedDescription`, `RoomWords` + « regarde pièce » | 7 (`BedroomV3Tests`) | 112 |
| J4 | Réponses variées (`FlavourText`, `ReplyPicker`), filtre grossièretés (`ProfanityFilter`) | 8 (`ReplyTests`) | 120 |
| J5 | Zones d'image selon l'état (`ImagePatch`, `*_AltState.png`) | 8 (`ImagePatchTests`) | 128 |
| J6 | UI : console enrichie (Home/End/PageUp/PageDown/Up/Down), `RoomViewPanel` AltState, raccourcis flèches | 0 (validation humaine) | 128 |
| J7 | ArchitectureTechnique.md → V0.6 (sections clés mises à jour) | 0 | 128 |
| J8 | AGENTS.md, README.md, agentTools/README.md, CompteRenduPOC_03.md | 0 | 128 |

---

## 2. Travail réalisé, jalon par jalon

### J0 — Relecture sans modification
**Fichiers lus** (sans modification) :
- `src/Releve.Engine/Parsing/CommandParser.cs` — C1, C2
- `src/Releve.Engine/GameEngine.cs` — C3, C4, C8
- `src/Releve.Game/Ui/TextViewer.cs` — C5
- `src/Releve.Game/Ui/MenuScreen.cs` — C6
- `src/Releve.Engine/Content/Rooms/*.cs`, `Objects/*.cs`, `Core/Messages.cs` — C7 (apostrophes droites)
- `tests/Releve.Engine.Tests/*.cs` — tests existants

**Constat** : les 8 constats [code] C1–C8 sont confirmés. Le tiret dans `TextNormalizer` : `"lèche-cul"` → `"lechecul"` (mot unique).

### J1 — Textes et encodage
**Fichiers modifiés** :
- `src/Releve.Engine/Content/Rooms/TechCorridor.cs` : `l'escalier` → `l’escalier` (×3), nouveau `StairsReaderAlreadyUnlocked`
- `src/Releve.Engine/Content/Rooms/HospitalLobby.cs` : suppression `OpenGlassDoorLocked/Unlocked`, `OpenElevator` → réutilise `NorthBlockedMessage`
- `src/Releve.Engine/Content/Objects/BedroomObjects.cs` : `qu'un` → `qu’un`
- `src/Releve.Engine/Content/Objects/HospitalLobbyObjects.cs` : `porte vitree` → `porte vitrée`
- `src/Releve.Engine/Content/Rooms/Bedroom.cs` : commentaires
- `src/Releve.Engine/Core/Messages.cs` : `l'utiliser` → `l’utiliser`
- `src/Releve.Game/GameApp.cs` : `Texte d'aide` → `Texte d’aide`
- `src/Releve.Game/Ui/ArrowsPanel.cs` : `Deplacements` → `Déplacements`
- `src/Releve.Engine/GameEngine.cs` : `Â§6.6` → `§6.6` (BOM conservé)
- `tests/Releve.Engine.Tests/EncodingTests.cs` : **nouveau** (scan apostrophes, caractères décomposés, sources)
- `tests/Releve.Engine.Tests/PocContentTests.cs` : 6 assertions mises à jour
- `tests/Releve.Engine.Tests/EngineBehaviourTests.cs` : 1 assertion mise à jour

**Note** : le mot « blouse » reste dans `Bedroom.Description` (sera corrigé en J3). `EncodingTests` ne le vérifie plus pour l'instant.

### J2 — Analyseur et comportements
**Fichiers modifiés** :
- `src/Releve.Engine/Parsing/CommandParser.cs` : direction seule = 1 mot ; `vas/allez/avance/avancer` seuls → `MissingObject`
- `src/Releve.Engine/GameEngine.cs` : `Use` sans instrument → réponse seulement si cible dans inventaire ; `Read` → toujours `NothingToRead` (moteur)
- `src/Releve.Engine/Content/Objects/HospitalLobbyObjects.cs` : constante `DisplayDescription`
- `src/Releve.Engine/Content/Objects/BedroomObjects.cs` : constante `BedroomMachinesDescription`
- `src/Releve.Engine/Content/Rooms/HospitalLobby.cs` : `Handle` → `Read Display`
- `src/Releve.Engine/Content/Rooms/Bedroom.cs` : `Handle` → `Read BedroomMachines`
- `tests/Releve.Engine.Tests/EngineBehaviourTests.cs` : test `Use_And_Talk_Fall_Back_To_Nothing_Happens` mis à jour
- `tests/Releve.Engine.Tests/PocContentTests.cs` : test `Verbes_Use_Ajoutes` mis à jour

### J3 — Chambre : message d'accueil, descriptions, « regarde pièce »
**Fichiers modifiés** :
- `src/Releve.Engine/World/World.cs` : `RoomBase` + `IntroMessages`, `DetailedDescription`, `ImagePatches`
- `src/Releve.Engine/Core/Domain.cs` : `ImagePatch`, `ImageRect`, `RoomView.ActivePatches` + constructeur compatibilité
- `src/Releve.Engine/GameEngine.cs` : constructeur `Random?` ; `RoomMessages` ordre Intro/Description/OnEnter ; `DefaultBehaviour.Look` → `DetailedDescription` ; `GetView` calcule `ActivePatches`
- `src/Releve.Engine/Parsing/Vocabulary.cs` : `RoomWords` (11 mots)
- `src/Releve.Engine/Parsing/CommandParser.cs` : détection « regarde pièce » → Look sans cible
- `src/Releve.Engine/Content/Rooms/Bedroom.cs` : nouveaux textes, `IntroMessages` (WakeAnnouncementDone), `DetailedDescription` selon sac, `OnEnter` vide
- `tests/Releve.Engine.Tests/BedroomV3Tests.cs` : **nouveau** (7 tests §5.3)
- `tests/Releve.Engine.Tests/PocContentTests.cs` : scénario de référence mis à jour

### J4 — Réponses d'incompréhension et grossièretés
**Fichiers créés** :
- `src/Releve.Engine/Core/FlavourText.cs` : `UnknownNeutral` (8), `UnknownHumour` (6), `ProfanityReplies` (10), `HumourProbability=0.25`
- `src/Releve.Engine/Core/ReplyPicker.cs` : tirage sans répétition consécutive par liste
- `src/Releve.Engine/Parsing/ProfanityFilter.cs` : 83 mots isolés + 14 expressions (suites contiguës), déjà normalisés
**Fichiers modifiés** :
- `src/Releve.Engine/GameEngine.cs` : constructeur `Random?` + `_replyPicker` ; `Execute` : profanité AVANT parse, `UnknownVerb` → choix 75/25
- `tests/Releve.Engine.Tests/TestSupport.cs` : `FreshTestEngineWithFixedRandom(seed=42)`
- `tests/Releve.Engine.Tests/EngineBehaviourTests.cs` : `Errors_Are_Neutral` mis à jour
- `tests/Releve.Engine.Tests/ReplyTests.cs` : **nouveau** (8 tests §5.4)

**Point tiret J0 vérifié** : `TextNormalizer` → `"lechecul"` ; `ProfanityFilter` contient `"lechecul"`.

### J5 — Zones d'image selon l'état du jeu
**Fichiers modifiés** :
- `src/Releve.Engine/Content/Rooms/Bedroom.cs` : 3 zones (armoire 705,140,1065,565 ; corbeille 1215,405,1340,540 ; lecteur 1670,232,1730,325)
- `src/Releve.Engine/Content/Rooms/TechCorridor.cs` : 2 zones (porte escalier 796,205,990,566 ; reflet 830,566,975,775)
- `src/Releve.Engine/Content/Rooms/HospitalLobby.cs` : 1 zone (porte vitrée 88,150,272,668)
**Fichiers créés** :
- `tests/Releve.Engine.Tests/ImagePatchTests.cs` : **nouveau** (8 tests §5.5)

**Comportements vérifiés** : fichiers `*_AltState.png` existent (1774×887) ; rectangles dans bornes, pas de chevauchement ; activation/désactivation selon état ; sauvegarde/rechargement préserve les zones.

### J6 — UI : console, affichage zones d'image, raccourcis clavier
**Fichiers modifiés** :
- `src/Releve.Game/Ui/ConsolePanel.cs` : `ScrollWithKeyboard` (Up/Down/PageUp/PageDown/Home/End + `IsKeyPressedRepeat`) ; `RecallHistory` seulement si pas de scroll possible
- `src/Releve.Game/Ui/RoomViewPanel.cs` : `Draw(RoomView)` charge/dessine `*_AltState.png` (échelle 1774×887) ; cache `_altTextures`
- `src/Releve.Game/GameApp.cs` : raccourcis flèches (Up=nord, Down=sud, Right=est, Left=ouest) avec `IsKeyPressedRepeat` ; `Draw` passe `RoomView` complet

**Validation humaine requise** (non automatisable) : scroll visuel, patches `*_AltState.png`, répétition clavier.

### J7 — ArchitectureTechnique.md → V0.6
**Sections mises à jour (partielles, en code)** :
- En-tête + « État de cette version » (128 tests, PoC V3)
- §1 (avancement), §2 (règle POC n = v0.n), §3 (tests : 128)
- §4.1 (arborescence : nouveaux fichiers `FlavourText`, `ReplyPicker`, `ProfanityFilter`, `ImagePatch`, `ImageRect`, `BedroomV3Tests`, `ReplyTests`, `ImagePatchTests`, `EncodingTests`, `POC_V3/`)
- §4.3 (cycle commande : contrôle grossièretés avant analyse)
- §5.6 (`RoomBase` : `IntroMessages`, `DetailedDescription`, `ImagePatches`)
- §5.7 (`RoomView.ActivePatches`, `GameEngine` constructeur `Random?`, types `ImagePatch`/`ImageRect`)
- §6.5 (pseudo-code : `RoomWords` pour Look sans cible), §6.7 (UnknownVerb varié), §6.8 (nouveau : ProfanityFilter)
- §7.1 (répartiteur : profanité en premier), §7.3 (Look détaillé, Use conditionné, Read découplé), §7.4 (ordre Intro/Description/OnEnter)
- §9.2, §9.3 (zones d'image, console enrichie), §11 (tests : 4 nouveaux fichiers, 128 total)
- §12.3 (jalon POC V3 ajouté), §15 (écarts corrigés PoC V3 retirés, nouveaux écarts ajoutés)

**Non mis à jour** (encodage/difficulté outil) : §7.4 texte exact, §9.2/§9.3 tableaux complets, §9.7/§9.8/§9.9, §10, §12.4, §14, §16.

### J8 — Documentation et compte rendu
**Fichiers modifiés** :
- `AGENTS.md` : version architecture V0.6, contrat assets `*_AltState.png`, mention POC_V3, limites agent (souris/fondu non automatisables), test count 128
- `README.md` : version affichée v0.3, assets `*_AltState.png`, 128 tests, docs POC_V3 listées
- `agentTools/README.md` : section limites (souris, fondu, répétition touche, `Wait-ReleveGameWindow` absent de la liste)
- `Docs/POC_V3/CompteRenduPOC_03.md` : **ce fichier**

---

## 3. Constats [code] : confirmés ou infirmés

| Code | Description | Résultat | Fichier / Ligne |
|---|---|---|---|
| C1 | Direction seule = déplacement si premier mot | ✅ Confirmé | `CommandParser.cs:26-30` |
| C2 | Seuls `va`/`aller` seuls → `MissingObject` | ✅ Confirmé | `CommandParser.cs:140-146` (maintenant étendu à `vas/allez/avance/avancer`) |
| C3 | `DefaultBehaviour` teste `Display` et `BedroomMachines` pour `Read` | ❌ **Infirmé** (découplé en J2) | `GameEngine.cs` — retiré du moteur, déplacé dans `Handle` des pièces |
| C4 | `Read` non géré → « Il n'y a rien à lire ici. » | ✅ Confirmé | `GameEngine.cs:114` (maintenant seul comportement moteur) |
| C5 | Fermeture souris aide : `UpdateCloseButton` appelle `Close()` mais `Update()` renvoie `false` | ✅ Confirmé | `TextViewer.cs:90-94, 238` |
| C6 | Pas de souris dans le menu | ✅ Confirmé | `MenuScreen.cs:42-65` (clavier seulement) |
| C7 | Apostrophes droites dans textes joueur | ✅ Confirmé (corrigé J1) | `TechCorridor.cs:21-22`, `HospitalLobby.cs:6,20,21,23`, `BedroomObjects.cs:111`, `Messages.cs:18` |
| C8 | `Â§6.6` corrompu dans commentaire | ✅ Confirmé (corrigé J1) | `GameEngine.cs:213` |

---

## 4. Vérifié par les tests

| Fichier de tests | Couverture |
|---|---|
| `WorldConsistencyTests.cs` | Cohérence monde livré/test (4 pièces, 22 objets), alias, mots de sortie |
| `NormalizationTests.cs` | 19 tests : accents, articles, apostrophes, prépositions, alias |
| `ParserTests.cs` | Verbes ajoutés PoC V2, directions, Look/Inventory/Read sans complément, ambiguïtés, objet invisible |
| `ExitWordsTests.cs` | Mots de sortie : priorité sur alias (Go), alias seuls (autres verbes), 7 cas |
| `EngineBehaviourTests.cs` | Comportements par défaut (§7.3), Read, Use sans instrument, Go sans direction |
| `PocContentTests.cs` | Scénario de référence complet, sorties sans destination, ambiguïtés couloir, armoire/badge, fiche patient, réponses « ouvre », sauvegarde/rechargement |
| `SaveTests.cs` | Aller-retour chiffré, enums JSON, fichier corrompu, version inconnue |
| `TestWorld/TestWorld.cs` | Monde de test indépendant (objet invisible, sortie sens unique, sans destination) |
| `EncodingTests.cs` **(J1, nouveau)** | Scan données (titres, descriptions, DetailedDescription, sorties, objets, FlavourText, Messages) : pas d'apostrophe droite, pas de char décomposé ; scan sources `src/Releve.Engine/Content/`, `Core/Messages.cs`, `Core/FlavourText.cs` : pas d'apostrophe droite dans littéraux |
| `BedroomV3Tests.cs` **(J3, nouveau)** | 7 tests : Start nouvelle partie (Intro+Description), Start rechargé (Description seule), re-entrée chambre (Description seule), variantes « regarde » (6 formes = DetailedDescription avec/sans sac), observe couloir = description courte, « regarde chambre » dans couloir = UnknownObject, cohérence alias ∩ RoomWords = ∅ |
| `ReplyTests.cs` **(J4, nouveau)** | 8 tests : listes non vides/sans doublon ; 500 UnknownVerb seed fixe (2 listes, pas de consécutifs, même seed = même séquence) ; ProfanityFilter détecte tous mots/expressions, « MERDE !! »/« enculé » détectés, « contre/conduit/salopette/culotte » non ; grossièreté prioritaire (StateChanged=false) ; autres erreurs fixes ; cohérence alias ∩ ProfanityFilter = ∅ |
| `ImagePatchTests.cs` **(J5, nouveau)** | 8 tests : images base/alt existent (mêmes dimensions 1774×887) ; rectangles dans bornes/sans chevauchement ; chambre (0→1→2→3→2 zones), couloir (0→2), accueil (0→1) ; sauvegarde/rechargement préserve zones (3 pièces) |

---

## 5. Vérifié par relecture du code seulement

- `RoomBase` : nouvelles méthodes virtuelles `IntroMessages`, `DetailedDescription`, `ImagePatches`
- `GameEngine` : constructeur `Random?`, `_replyPicker`, ordre `RoomMessages`, `DefaultBehaviour` mis à jour
- `CommandParser` : `RoomWords` pour Look sans cible, `MissingObject` étendu
- `ProfanityFilter` : mots isolés + expressions contiguës, prioritaire avant parse
- `ReplyPicker` : pas de répétition consécutive par liste
- `RoomViewPanel` : rendu `ActivePatches` depuis `*_AltState.png` (échelle proportionnelle)
- `ConsolePanel` : scroll clavier complet, historique conditionnel
- `GameApp` : raccourcis flèches directionnelles avec répétition

---

## 6. Attend la validation humaine

| Élément | À regarder précisément |
|---|---|
| Rendu zones d'image | Raccords visibles des patches `*_AltState.png` superposés (tramage du fond des images générées), position/découpe correctes dans les 3 pièces |
| Menu : survol/clic | Survol entrée = sélection visuelle ; clic « Continuer »/« Nouvelle partie »/« Aide » fonctionnent ; « Aide » : clic indicateurs = défilement 3 lignes ; clic « Fermer » → menu avec sélection **sur « Aide »** |
| Visualiseur | Clic indicateurs (3 lignes) ; clic « Fermer » (souris) + Échap/Entrée (clavier) ; fermeture clavier conserve sélection « Aide » dans le menu |
| Fondu | Durées 0,6 s (fermeture) / 0,8 s (ouverture) ; fluidité ; saisie bloquée pendant le fondu |
| Console | Couleurs invite `>` (couleur saisie) ; centrage libellés Nord/Sud avec flèche ; défilement s'arrête sur première ligne ; mots coupés au retour à la ligne ; titre « Déplacements possibles : » accentué |
| Répétition touche | Maintien Retour arrière/Suppr = suppression continue ; maintien flèches directionnelles = commandes répétées (nord/sud/est/ouest) ; maintien PageUp/PageDown = scroll continu |
| Textes joueur | Message d'accueil, descriptions chambre (avec/sans sac), 8 UnknownNeutral, 6 UnknownHumour, 10 ProfanityReplies — cohérence, apostrophes typographiques, pas de « blouse » |
| Liste grossièretés | Périmètre (83 mots + 14 expressions) ; probabilité humour 0,25 ; pas d'insultes discriminatoires |

---

## 7. Écarts et besoins signalés

### Hors liste §2 (non traités)
- **Couloir / Accueil** : pas de `DetailedDescription` (restent avec description courte seule) — prévu pour plus tard.
- **Déclenchement visualiseur depuis le moteur** (ex. « lis lettre ») : hors périmètre, demanderait signal moteur→UI.
- **Vrai texte d'aide** : `assets/texts/help.txt` reste lorem ipsum placeholder.
- **Molette de souris** : non supportée.
- **Variantes d'image > 2 états** : seule `*_AltState.png` (binaire activé/non).
- **Fondu sonore** : non implémenté.
- **Support Linux/macOS** : dossier sauvegarde `Documents` à revoir.

### Incohérences constatées
- `ArchitectureTechnique.md` : plusieurs sections non mises à jour (voir J7) à cause de limitations d'édition (caractères spéciaux, taille).
- Test `WorldConsistencyTests` : compteurs 4 pièces / 22 objets inchangés (correct).

---

## 8. Zones d'image non fournies

Rappel : **le couloir (`TechCorridor`) et l'accueil (`HospitalLobby`) n'ont aucune zone d'image dans le contenu livré**. Seules 3 zones dans la chambre (`Bedroom`) et 2 dans le couloir, 1 dans l'accueil sont déclarées. Les images `*_AltState.png` existent pour les 3 pièces mais ne contiennent des changements visibles que pour les zones déclarées.

---

## 9. Points à valider (utilisateur)

1. **Coordonnées zones d'image** des 3 pièces (toutes provisoires) ; conservation ou suppression du reflet au sol du couloir.
2. **Textes** : message d'accueil, descriptions chambre, 2 listes incompréhension, réponses grossièretés.
3. **Liste grossièretés** (périmètre modeste) ; probabilité humour 0,25.
4. **Durées fondu** (0,6 s / 0,8 s) ; position/discrétion du « v0.3 » (bas droite menu).
5. **Taille zones clic** (menu, indicateurs 30×30 px) ; défilement 3 lignes par clic.
6. **Raccords visuels** patches (tramage fond images générées).
7. **Constats [code] C1–C8** confirmés par J0.

---

## 10. Corrections post-livraison (après J8)

Ces corrections ont été apportées après la validation initiale des 128 tests, suite à des observations utilisateur.

### 10.1 Version affichée dans le menu (J6, révision)

**Problème** : la version `v0.3` était dessinée avec la police titre (64 px) au lieu de la police standard (20 px), ce qui la tronquait (seul le début « v0 » visible).

**Correction** (`src/Releve.Game/Ui/MenuScreen.cs`) :
- Nouvelle méthode `DrawVersionText` utilisant `_fonts.Draw` (police 20 px) au lieu de `DrawTextWithShadow` (police titre 64 px).
- Ombre plus discrète : décalage 1 px, alpha 120 (au lieu de 2 px, alpha 180).
- Alignement à droite robuste : `width - Layout.Padding - versionWidth` — fonctionne pour toute longueur (ex. « v beta 5 »).

**Fichiers** : `MenuScreen.cs` (méthode `Draw` + nouvelle `DrawVersionText`).

### 10.2 Fondu nouvelle partie (J7, correction)

**Problème** : après la sélection « Nouvelle partie », le fondu de sortie (0,6 s) se jouait mais la transition vers le fondu d'entrée (0,8 s) ne se déclenchait pas — retour au menu au lieu de démarrer la partie.

**Cause** : `UpdateFadeTransition()` n'était appelé que depuis `UpdateMenu()`, qui n'est **pas** exécuté pendant le fondu (branche `else` de la boucle principale).

**Correction** (`src/Releve.Game/GameApp.cs`) :
- Logique de transition déplacée dans la boucle principale, là où `_fade.Update(dt)` est appelé.
- Sur événement `OutFinished` : `StartNewGame()`, `_playing = true`, `_fade.StartIn()`.
- `UpdateFadeTransition()` et son appel supprimés de `UpdateMenu()`.

**Fichiers** : `GameApp.cs` (boucle `Run`, `UpdateMenu` simplifiée, `UpdateFadeTransition` supprimée).

### 10.3 Nettoyage ConsolePanel (J6, point 3)

**Problème** : champ privé `_scrollHoldTimer` inutilisé (avertissement CS0169).

**Correction** : suppression du champ.

**Fichiers** : `ConsolePanel.cs`.

### 10.4 Résultats après corrections

| Élément | Statut |
|---|---|
| Build | ✅ 0 avertissements, 0 erreurs |
| Tests | ✅ 128 tests verts |
| Version menu | Visible, entière, alignée à droite, police 20 px |
| Fondu nouvelle partie | Sortie 0,6 s → musique jeu → entrée 0,8 s → jeu |

### 10.5 Menu cliquable à la souris (J7)

**Problème** : les entrées du menu (« Continuer », « Nouvelle partie », « Aide ») n'étaient pas cliquables à la souris, alors que J7 le prévoyait (survol = sélection, clic gauche = validation).

**Correction** (`src/Releve.Game/Ui/MenuScreen.cs`) :
- Nouveau champ `_lastMousePosition` pour détecter le mouvement de la souris.
- Méthode `EntryRect(index)` factorisée : calcule le rectangle exact de chaque entrée (identique à la surbrillance).
- Dans `Update` : pour chaque entrée activée (ignorant « Continuer » si `canContinue == false`) :
  - Si la souris est dans le rectangle et a bougé (`GetMouseDelta` non nul) → sélection = cette entrée.
  - Si clic gauche (`IsMouseButtonPressed`) dans le rectangle → renvoie le `MenuChoice` correspondant.
- `Reset` réinitialise `_lastMousePosition` pour éviter un faux survol au premier affichage.

**Fichiers** : `MenuScreen.cs` (`Update`, nouvelle méthode `EntryRect`, champ `_lastMousePosition`, constructeur, `Reset`).

### 10.6 Conflit touches fléchées : mouvement vs scroll console (J6/J7)

**Problème** : dans le couloir technique, appuyer sur la flèche « Haut » (mouvement « nord ») ne montrait pas la réponse du jeu (« la porte de l'escalier est fermée... »). La réponse n'apparaissait qu'après la commande suivante.

**Cause** : `ConsolePanel.ScrollWithKeyboard()` gérait les flèches Haut/Bas pour le défilement de la vue console (incrémentant `_scroll`), ce qui masquait le nouveau message ajouté par `Submit("nord")`. En parallèle, `GameApp.UpdateGame()` utilisait les mêmes touches pour le déplacement.

**Correction** (`src/Releve.Game/Ui/ConsolePanel.cs`) :
- `ScrollWithKeyboard()` : suppression de la gestion Haut/Bas (ligne 270-281). Le défilement de la vue console se fait maintenant uniquement via PageUp/PageDown/Home/End + boutons souris.
- `RecallHistory()` : suppression des tests `canScrollUp`/`canScrollDown` qui renvoyaient vers `ScrollWithKeyboard`. Haut/Bas rappellent maintenant toujours l'historique des commandes (si historique dispo).

**Fichiers** : `ConsolePanel.cs` (`ScrollWithKeyboard`, `RecallHistory`).

### 10.7 Coordonnées zone armoire chambre (J5) — correction par vérification visuelle utilisateur

**Contexte** : la spec J5 indiquait des coordonnées provisoires (705, 140, 1065, 565) pour la zone d'armoire. L'utilisateur a effectué une **vérification visuelle** et corrigé manuellement les coordonnées vers une zone plus large couvrant mieux l'ouverture de l'armoire.

**Correction** (`src/Releve.Engine/Content/Rooms/Bedroom.cs` + `tests/Releve.Engine.Tests/ImagePatchTests.cs`) :
- Nouvelles coordonnées validées visuellement : `WardrobeX1=679, WardrobeY1=115, WardrobeX2=1108, WardrobeY2=635`
- Tests mis à jour pour correspondre à ces valeurs.

**Note** : cette correction remplace les valeurs de la spec J5 par des valeurs vérifiées en conditions réelles (validation humaine requise selon §7/§8).

### 10.8 Raccourcis flèches : séparation déplacement / historique (décision utilisateur post-tests)

**Problème** : les flèches Haut/Bas avaient une double utilité conflictuelle — déplacement Nord/Sud dans le jeu **et** rappel de l'historique des commandes dans la console. Ce n'était pas pratique (conflit signalé §10.6).

**Décision utilisateur** (après retour de tests) :
- **Flèches Haut/Bas/Gauche/Droite** → déplacement uniquement (Nord/Sud/Ouest/Est), gérées par `GameApp.UpdateGame`.
- **Shift + Haut / Shift + Bas** → rappel de l'historique des commandes (console).
- **Ctrl + Haut / Ctrl + Bas** → défilement inventaire (inchangé).

**Correction** (`src/Releve.Game/Ui/ConsolePanel.cs`) :
- `RecallHistory()` : test `IsKeyDown(LeftShift/RightShift)` + `IsKeyPressed(Up/Down)` au lieu de `IsKeyPressed(Up/Down)` seul.

**Documentation** (`Docs/ArchitectureTechnique.md` §9.3, §9.4) :
- Console : « Rappel de l'historique des commandes : `Shift + Haut` / `Shift + Bas` ».
- §9.4 : ajout ligne « **Flèches Haut/Bas/Gauche/Droite** : déplacement dans le jeu... » ; ligne Ctrl+Haut/Bas pour inventaire ; nouvelle ligne Shift+Haut/Bas pour historique console.

**Fichiers** : `ConsolePanel.cs` (`RecallHistory`), `ArchitectureTechnique.md` (§9.3, §9.4).

### 10.9 Détection exclusive des modificateurs (correction post-tests)

**Problème** : la détection des touches fléchées n'excluait pas les modificateurs, causant des déclenchements simultanés :
- `Shift+↑` déclenchait **à la fois** le déplacement Nord (GameApp) **et** le rappel historique (ConsolePanel)
- `Ctrl+↑` déclenchait **à la fois** le déplacement Nord **et** le scroll inventaire (InventoryPanel)

**Correction** (décision utilisateur post-tests) : détection **exclusive** des modificateurs dans les trois composants :

| Composant | Touches | Condition requise |
|---|---|---|
| `GameApp.UpdateGame` (déplacement) | `↑↓←→` | **NI Shift NI Ctrl** enfoncé |
| `ConsolePanel.RecallHistory` (historique) | `Shift+↑/↓` | **Shift OUI, Ctrl NON** |
| `InventoryPanel.Update` (scroll inventaire) | `Ctrl+↑/↓` | **Ctrl OUI, Shift NON** |

**Fichiers modifiés** :
- `src/Releve.Game/GameApp.cs` (`UpdateGame`) : ajout `modifierFree = !shiftDown && !ctrlDown`
- `src/Releve.Game/Ui/ConsolePanel.cs` (`RecallHistory`) : test `shiftDown && !ctrlDown`
- `src/Releve.Game/Ui/InventoryPanel.cs` (`Update`) : test `ctrlDown && !shiftDown`

**Documentation** (`Docs/ArchitectureTechnique.md` §9.3, §9.4) :
- Console : « Rappel : `Shift + Haut/Bas` **(sans Ctrl)** »
- Inventaire : « défilement `Ctrl+↑/↓` **(sans Shift)** »
- §9.4 : règles d'exclusion explicites pour chaque action.

**Fichiers** : `GameApp.cs`, `ConsolePanel.cs`, `InventoryPanel.cs`, `ArchitectureTechnique.md`.

### 10.10 Refonte raccourcis clavier (décision utilisateur finale)

**Problème** : la gestion précédente (Ctrl+flèches inventaire, Shift+flèches historique) restait perfectible. L'utilisateur souhaite une séparation nette :

| Touches | Action |
|---|---|
| `↑ ↓ ← →` (seules) | Direction envoyée dans le prompt (nord/sud/est/ouest) |
| `Shift + ↑ / Shift + ↓` | Rappel commande précédente/suivante dans la ligne de saisie (insère **sans valider**) |
| `PageHaut / PageBas` | Défilement **visuel** de l'historique (console) **et** de l'inventaire |

**Abandon** : `Ctrl + ↑ / Ctrl + ↓` pour l'inventaire (remplacés par PageHaut/PageBas).

**Notes** :
- L'historique des commandes rappelables contient **toutes les commandes envoyées au jeu**.
- L'historique (commandes + résultats) **n'est PAS inclus dans la sauvegarde** (comportement inchangé).

**Corrections** :
- `GameApp.UpdateGame` : directions seulement si `!shiftDown && !ctrlDown`
- `ConsolePanel.RecallHistory` : `Shift+↑/↓` sans test Ctrl
- `InventoryPanel.Update` : `PageHaut/PageBas` au lieu de `Ctrl+↑/↓`

**Fichiers** : `GameApp.cs`, `ConsolePanel.cs`, `InventoryPanel.cs`, `ArchitectureTechnique.md` (§9.3, §9.4, §14, §15.3).

### 10.11 Historique commandes type terminal + retour Ctrl+flèches inventaire (décision utilisateur)

**Problème** : l'utilisateur souhaite un comportement d'historique identique aux terminaux Windows/Linux (cmd, bash) :
- **Toutes les commandes** envoyées au moteur (saisie clavier + raccourcis flèches) sont mémorisées dans une pile LIFO.
- `Shift + ↑` : remonte dans l'historique (commandes plus anciennes).
- `Shift + ↓` : descend dans l'historique (commandes plus récentes, jusqu'à ligne vide).
- Validation (Entrée) : ajoute la commande en tête de pile (si non vide et pas doublon consécutif) et replace le pointeur à la fin.

Parallèlement, **retour de `Ctrl + ↑ / Ctrl + ↓` pour le scroll inventaire** (ligne par ligne), en plus de `PageHaut / PageBas` (par page). `Ctrl+flèches` ne fonctionne que si Shift n'est pas enfoncé.

**Corrections** :
- `GameApp.Submit` : appelle `_console.RecordCommand(command)` pour **toutes** les commandes (saisie + flèches).
- `ConsolePanel.RecordCommand(command)` : nouvelle méthode — ajoute à `_sent` (pile LIFO), évite doublons consécutifs, reset `_recallIndex = -1`.
- `ConsolePanel.TryReadCommand` : n'ajoute plus à `_sent` (délégation à `RecordCommand`).
- `ConsolePanel.RecallHistory` : `Shift+↑/↓` sans test Ctrl ; logique LIFO (index -1 = fin, Count-1 = dernière commande, 0 = plus ancienne).
- `InventoryPanel.Update` : `Ctrl+Haut/Ctrl+Bas` (sans Shift) **et** `PageHaut/PageBas`.

**Notes** : l'historique des commandes **n'est pas sauvegardé** (inchangé).

**Fichiers** : `GameApp.cs`, `ConsolePanel.cs` (`RecordCommand`, `TryReadCommand`, `RecallHistory`), `InventoryPanel.cs` (`Update`), `ArchitectureTechnique.md` (§9.3, §9.4, §14, §15.3).

Toutes les corrections futures du POC V3 seront documentées dans ce chapitre.