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

**Note** : La section « vérifié dans le jeu réel » **n'est pas présente** car l'agent n'a **pas** observé le jeu visuellement. Tous les éléments visuels/interactifs sont **non vérifiés visuellement par l'agent**.