# Jeu "Relève" — Implémentation du POC V3

> Document d’implémentation — version 1.1
> **Prérequis** : POC V2 livré et fusionné dans `main` (plans et comptes rendus dans `Docs/POC_V1/` et `Docs/POC_V2/`), architecture technique V0.5 en place.
> À fournir à l’agent avec : l’architecture technique (V0.5), la note d’intention (V6), le document univers/scénario (v0.8, §4.2 et §5.1 à §5.3), `AGENTS.md`, et les plans et comptes rendus des POC V1 et V2.
> Les mentions « **[code]** » signalent un constat établi par lecture du code du dépôt : l’agent le **re-vérifie** avant d’agir et signale tout écart dans son compte rendu.
> Tous les textes de ce document sont **provisoires**, sauf mention contraire.

## 0. Règles de travail de l’agent (impératives)

1. **Un jalon à la fois**, dans l’ordre. À la fin de chaque jalon : `dotnet build` puis `dotnet test` verts, puis un court message de progression (ce qui est fait, ce qui reste, tout écart). Ne pas passer au jalon suivant si le build ou un test est rouge.
2. **Aucune commande git d’écriture** (règle d’`AGENTS.md`) : l’utilisateur commite lui-même.
3. **Ne pas modifier** : `.kilo/`, `_ContentSources/`, les images de lieux (`assets/rooms/*.png`, y compris les `*_AltState.png`), les fichiers audio, `assets/menu/menuBackground.png`, `assets/texts/help.txt`, le compte rendu du POC V2.
4. **Pas de migration .NET** : le projet reste sur .NET 8.
5. **Périmètre fermé** : ce document est la liste complète du travail. Toute évolution d’architecture non listée au §2, tout besoin imprévu, toute incohérence avec le lore ou l’architecture, tout constat **[code]** inexact : le **signaler** dans le compte rendu, ne pas le traiter. En cas de doute, choisir l’option la plus conservatrice (ne pas changer) et le dire.
6. **Raylib** : vérifier chaque appel (souris, temps, répétition de touche, dessin, collision) dans la cheatsheet et les exemples officiels, pas de mémoire (voir « Références » de l’architecture).
7. **Encodage** : fichiers en UTF-8, **conserver le BOM** des fichiers qui en ont un (par exemple `GameEngine.cs`). Textes joueur : caractères précomposés et apostrophe typographique `’` (architecture §2). Relire les fichiers modifiés après écriture pour vérifier qu’aucun caractère n’est corrompu.
8. **Honnêteté du compte rendu** : ne jamais écrire « vérifié dans le jeu réel » pour ce que l’agent n’a pas observé lui-même. Voir §7 et §8.

## 1. But et périmètre

### 1.1 But
Étendre le jeu sur le **même contenu de lore** que les POC V1 et V2 (chambre, couloir, accueil, rue provisoire) :
- afficher la version du jeu dans le menu ;
- faire évoluer l’image d’une pièce par zones selon l’état des objets ;
- varier et pimenter les réponses aux saisies incomprises et aux grossièretés ;
- retravailler les textes d’ouverture de la chambre et ajouter une description détaillée ;
- ajouter un fondu au lancement d’une nouvelle partie ;
- rendre le menu et le visualiseur de texte utilisables à la souris ;
- corriger les défauts relevés après le POC V2.

### 1.2 Hors périmètre
- Aucun nouveau lieu, objet de lore, puzzle ni élément d’histoire. La rue reste provisoire.
- Pas de déclenchement du visualiseur depuis le moteur, pas de molette de souris, pas de pénalité pour grossièreté, pas de support Linux/macOS, pas de fondu pour « Continuer », pas de fondu sonore.
- Pas de description détaillée pour le couloir ni l’accueil (la propriété existe pour toutes les pièces, seule la chambre la renseigne).
- Pas de changement de la sauvegarde (schéma, contenu) : les zones d’image se déduisent de l’état déjà sauvegardé.

### 1.3 Critère de fin
Jalons J0 à J8 terminés, scénario de référence V3 (§6) rejoué autant que l’agent le peut (§7), build et tests au vert, documentation et compte rendu écrits (§8).

## 2. Évolutions d’architecture autorisées (liste fermée)

**Moteur (`Releve.Engine`)**
1. `RoomBase` reçoit trois membres **virtuels** : `IntroMessages(GameState)`, `DetailedDescription(GameState)`, `ImagePatches` (§3, J3 et J5).
2. `GameEngine.RoomMessages` : ordre « messages d’introduction, description, `OnEnter` » ; `Look` sans cible utilise la description détaillée ; le constructeur reçoit un `Random?` optionnel.
3. `GameEngine.Execute` : contrôle des grossièretés **avant** l’analyse ; réponse d’incompréhension tirée dans une liste pour `UnknownVerb` (J4).
4. `RoomView` reçoit une propriété `ActivePatches` (J5).
5. `CommandParser` : trois changements (direction seule, verbes `vas/allez/avance/avancer` seuls, mots désignant la pièce pour `Look`) (J2, J3).
6. Nouveaux fichiers du moteur : `Parsing/ProfanityFilter.cs`, `Core/FlavourText.cs`, `Core/ReplyPicker.cs` ; `Vocabulary.RoomWords` (J3, J4).
7. `Read` sur `Display` et `BedroomMachines` : retiré du moteur, déplacé dans les `Handle` des pièces (J2).
8. `Use` sans instrument : réponse conditionnée à l’inventaire (J2).

**Interface (`Releve.Game`)**
9. Nouveaux fichiers : `GameInfo.cs`, `Ui/ScreenFade.cs`.
10. `MenuScreen` : survol et clic ; version affichée (J6, J7).
11. `TextViewer` : corrections, indicateurs cliquables (J6, J7).
12. `RoomViewPanel` : dessin des zones d’image (J6).
13. `GameApp` : fondu, et correctif de fermeture de l’aide (J6, J7).
14. `ConsolePanel` et `ArrowsPanel` : corrections listées en J6.

**Règle d’interaction (amende l’architecture §2 et §9.9, voir note d’intention V6 §2)** : tout reste faisable au clavier ; la souris n’est admise que pour quatre exceptions, chacune doublée d’un équivalent clavier :
1. boutons haut/bas de défilement de la console (Page↑ / Page↓) ;
2. bouton « Fermer » du visualiseur (Échap ou Entrée) ;
3. entrées du menu : le survol sélectionne, un clic gauche valide (flèches Haut/Bas et Entrée) ;
4. indicateurs de défilement du visualiseur : un clic défile de **3 lignes** (Page↑/Page↓, ↑/↓, Début/Fin).

Le curseur est visible. Les flèches de déplacement restent des indicateurs, jamais des boutons.

## 3. Jalons

### J0 — Relecture sans modification
1. Lire `AGENTS.md`, l’architecture V0.5 en entier, la note d’intention V6 et ce document.
2. Lancer `dotnet build` et `dotnet test` : noter le nombre de tests de départ (attendu : 103).
3. Confirmer ou infirmer chacun des constats **[code]** de ce document (liste récapitulée au §9).
4. Lister les fichiers à relire avant intervention : `RoomViewPanel.cs`, `ConsolePanel.cs`, `ArrowsPanel.cs`, `HospitalLobby.cs`, `TechCorridor.cs`, `BedroomObjects.cs`, `TextNormalizer.cs`, `Messages.cs`, `Vocabulary.cs`, `ParseResult.cs`, `TestWorld.cs`.
5. Repérer le comportement du **tiret** dans `TextNormalizer` (« lèche-cul » devient-il « leche cul » ou « lechecul » ?). Le noter pour J4.
6. **Ne rien modifier.** Envoyer un court message de constats.

### J1 — Textes et encodage
1. **Apostrophes droites** : remplacer par `’` dans **tous** les textes joueur du moteur et de la couche Game. Cas connus **[code]** : trois constantes « ouvre » dans `HospitalLobby`, trois dans `TechCorridor`, `DescribeBin` (`BedroomObjects`), le message `"Texte d'aide introuvable."` de `GameApp.OpenHelp`. Faire une recherche exhaustive (`"[^"]*'[^"]*"`) dans `src/Releve.Engine/Content/`, `Core/Messages.cs`, `src/Releve.Game/` et traiter tout résultat qui est un texte affiché au joueur (pas les littéraux `char` comme `' '`, pas les commentaires).
2. **Doublons** : dans `HospitalLobby` et `TechCorridor`, les réponses à « ouvre » qui répètent le message de sortie bloquée (porte vitrée non déverrouillée, porte de l’escalier verrouillée, porte du local technique, ascenseur) **réutilisent la constante** du message de sortie bloquée correspondant, au lieu d’un texte dupliqué. Textes inchangés.
3. **Titre du panneau des flèches** : `"Deplacements possibles : "` devient `"Déplacements possibles : "` (`ArrowsPanel`).
4. **Caractères corrompus** : dans `GameEngine.cs`, le commentaire `(Â§6.6)` devient `(§6.6)`. Conserver le BOM. Relire le fichier après modification : si `Â§` réapparaît, c’est que l’outil d’écriture a mal géré l’encodage.
5. **Lecteur de l’escalier** : dans `TechCorridor`, `Use(Badge, StairsReader)` sur un lecteur **déjà déverrouillé** répond : « Le lecteur de l’escalier est déjà déverrouillé. » (au lieu de « Rien ne se passe. »). Le lecteur de la chambre garde « Le lecteur est déjà déverrouillé. ».
6. **Test automatique d’encodage** : nouveau fichier `EncodingTests.cs` (§5.1).

### J2 — Analyseur et comportements
1. **Direction seule** (`CommandParser.Parse`). **[code]** Aujourd’hui, `DirectionAt(words, 0)` fait de toute saisie dont le premier mot est une direction un déplacement. Nouveau comportement : une direction ou son abréviation n’est un déplacement **que si elle est le seul mot** de la saisie (« est », « n »). Une saisie plus longue qui commence par une direction (« est badge », « n’ouvre pas la porte » qui devient « n ouvre porte ») n’est plus un déplacement : le premier mot est traité comme un verbe, donc inconnu (`UnknownVerb`). « va est », « va vers l’est », « aller a nord » ne changent pas (traités par `ParseGo`).
2. **Verbes de déplacement seuls** (`ParseGo`). **[code]** Seuls « va » et « aller » seuls renvoient `MissingObject`. Étendre à `va, vas, aller, allez, avance, avancer`. Les autres verbes de la classe `Go` (sors, monte, descends, entre, rentre, retourne et leurs formes) restent inchangés (`Go` sans direction, « Vous ne pouvez pas aller par là. » si aucun mot de sortie ne correspond).
3. **`Use` sans instrument** (`GameEngine.DefaultBehaviour`). **[code]** Aujourd’hui, « Précisez sur quoi l’utiliser. » est répondu pour toute cible. Nouveau comportement : cette réponse **uniquement si la cible est dans l’inventaire** ; sinon (« utilise lit ») : « Rien ne se passe. ».
4. **`Read` découplé** (**[code]** `GameEngine.DefaultBehaviour` teste `ObjectId.Display` et `ObjectId.BedroomMachines`) :
   - supprimer ce test du moteur : `Read` avec cible, non géré par la pièce, répond « Il n’y a rien à lire ici. » ;
   - dans `HospitalLobby.Handle` : `Read` sur `Display` renvoie le texte de `Describe` de l’afficheur ; dans `Bedroom.Handle` : `Read` sur `BedroomMachines` renvoie celui de `Describe` des machines. Pour ne pas dupliquer le texte, le rendre accessible aux deux endroits (par exemple une constante publique du fichier d’objets, utilisée par `Describe` et par `Handle`). Texte inchangé ;
   - ces `Handle` ne modifient pas l’état : `CommandResult.Say`, pas `Changed`.
5. Tests : §5.2.

### J3 — Chambre : message d’accueil, descriptions, « regarde pièce »
1. **Message d’introduction** (`RoomBase`) : ajouter `public virtual IEnumerable<string> IntroMessages(GameState s) => Array.Empty<string>();`. Dans `GameEngine.RoomMessages`, l’ordre devient : `IntroMessages`, puis `Description`, puis `OnEnter`. Pour toutes les pièces sauf la chambre, rien ne change (le résultat de `IntroMessages` est vide).
2. **Description détaillée** (`RoomBase`) : ajouter `public virtual string? DetailedDescription(GameState s) => null;`. Dans `GameEngine.DefaultBehaviour`, `Look` **sans cible** renvoie `DetailedDescription(State)` s’il n’est ni nul ni vide, sinon `Description(State)`.
3. **Chambre** (`Bedroom.cs`), textes en constantes en tête de classe :
   - `IntroMessages` renvoie **une seule fois** le message d’accueil, en utilisant l’événement existant `GameEvent.WakeAnnouncementDone` (même logique que l’ancien `OnEnter` : si l’événement est présent, ne rien renvoyer ; sinon l’ajouter puis renvoyer le message). `OnEnter` ne renvoie plus rien (supprimer la surcharge ou la laisser vide). Ne pas renommer l’événement.
   - Les trois textes sont donnés au §4.1. La chemise d’hôpital n’est mentionnée **qu’une fois**, dans le message d’accueil : le mot « blouse » ne doit plus apparaître dans aucun texte joueur.
   - `DetailedDescription` : le texte dépend de la présence du sac poubelle (même test que `DescribeBin`, **[code]** à relire dans `BedroomObjects`) : sac dans la chambre, phrase « une corbeille et son sac vide » ; sinon « une corbeille ».
4. **Mots désignant la pièce** (`Vocabulary`, `CommandParser`) :
   - `Vocabulary.RoomWords` : `piece, salle, endroit, lieu, autour, alentours, alentour, ici, environnement, partout, moi` (formes normalisées).
   - Dans `Parse`, pour l’action `Look` uniquement, après le retrait de la préposition de tête et **avant** la résolution des noms : si, en ignorant les prépositions, tous les mots restants sont dans `RoomWords` et qu’**au moins un** n’est pas « moi », le résultat est `Look` **sans cible**. Exemples : « regarde pièce », « regarde dans la pièce », « observe autour de moi » (normalisé « observe autour moi »), « regarde ici ».
   - Ne pas inclure « chambre » (mot de sortie du couloir). Aucun alias d’objet ne doit contenir un mot de `RoomWords` (test, §5.3).
5. Tests : §5.3.

### J4 — Réponses d’incompréhension et grossièretés
**Principe** : seule l’erreur `UnknownVerb` change. Les erreurs `MissingObject`, `UnknownObject` et `Ambiguous` restent **fixes** (ce sont des indications de jeu). La réponse à une grossièreté est prioritaire sur toute autre interprétation : la commande n’est **pas exécutée** (même si elle serait valide). Pas de pénalité, pas de compteur, rien n’est sauvegardé.

1. **`Core/FlavourText.cs`** (classe statique) : listes `UnknownNeutral`, `UnknownHumour`, `ProfanityReplies` (contenu exact au §4.2) et constante `HumourProbability = 0.25`.
2. **`Core/ReplyPicker.cs`** : classe portant le `Random` et, pour chaque liste, l’indice du dernier message renvoyé. `Pick(list)` tire un indice au hasard ; si la liste a plus d’un élément et que l’indice est le même que le dernier pour cette liste, tirer à nouveau. Jamais deux fois de suite le même message dans une même liste. L’état du `ReplyPicker` n’est **pas** sauvegardé.
3. **`GameEngine`** : constructeur `GameEngine(WorldModel world, GameState state, Random? random = null)` (les appels existants restent valides ; `null` crée un `Random` ordinaire). Un `ReplyPicker` est créé à partir de ce `Random`.
4. **`Parsing/ProfanityFilter.cs`** : classe statique avec la liste des mots et des expressions (§4.3, déjà normalisés) et `bool Contains(IReadOnlyList<string> normalizedWords)` : vrai si un mot est dans la liste des mots, **ou** si une expression apparaît comme **suite contiguë de mots**. Comparaison sur des **mots entiers**, jamais sur des fragments.
5. **`GameEngine.Execute`**, en tête : `var words = TextNormalizer.Normalize(input);` si `words.Count > 0` et `ProfanityFilter.Contains(words)`, renvoyer `CommandResult.Say(reply)` avec un message de `ProfanityReplies` ; ne **pas** appeler l’analyseur. Sinon, après l’analyse, si l’erreur est `UnknownVerb` : tirer `UnknownHumour` avec la probabilité `HumourProbability`, sinon `UnknownNeutral`. Les autres erreurs continuent d’utiliser `parse.ErrorMessage`. `CommandParser` et `ParseResult` ne changent pas.
6. Vérifier le point du tiret noté en J0 : si « lèche-cul » est normalisé en une seule forme, ne garder dans `ProfanityFilter` que la forme obtenue en pratique, après test.
7. Tests : §5.4. Les tests existants qui comparent le message exact de `UnknownVerb` dans les tests **du moteur** sont mis à jour pour vérifier l’appartenance à `UnknownNeutral` ∪ `UnknownHumour` (avec un `Random` à graine fixe). Les tests de l’analyseur seul (`ParserTests`) ne changent pas.

### J5 — Zones d’image selon l’état (moteur)
**Principe** : chaque pièce a au plus un fichier d’image alternative `assets/rooms/<ImageKey>_AltState.png`. Une **zone** (rectangle en pixels de l’image source) est remplacée par la même zone de l’image alternative **tant que sa condition est vraie**. L’affichage est **déduit de l’état** du jeu (aucune sauvegarde supplémentaire, retour à l’image de base si la condition redevient fausse). Toutes les images (base et alternative) font **1774 × 887** pixels.

1. **Domaine** (`Core/Domain.cs`) :
   ```csharp
   // Rectangle en pixels de l'image source : [X1, X2) x [Y1, Y2), X2 et Y2 exclus.
   public sealed record ImagePatch(int X1, int Y1, int X2, int Y2, Func<GameState, bool> IsActive);
   public readonly record struct ImageRect(int X1, int Y1, int X2, int Y2);
   ```
2. **`RoomBase`** : `public virtual IReadOnlyList<ImagePatch> ImagePatches => Array.Empty<ImagePatch>();`. Les pièces sans zone ne changent pas.
3. **`RoomView`** : ajouter dans le corps du record la propriété `public IReadOnlyList<ImageRect> ActivePatches { get; init; } = Array.Empty<ImageRect>();` (les constructions existantes de `RoomView` restent valides). `GameEngine.GetView` la renseigne : zones dont `IsActive(State)` est vrai, dans l’ordre de déclaration.
4. **Zones déclarées.** Pour chaque pièce, les coordonnées sont des **constantes nommées, regroupées en tête de classe** (une quadruple X1, Y1, X2, Y2 par zone, avec un nom parlant : par exemple `WardrobeX1`, `StairsDoorX1`), pour être facilement retrouvées et modifiées par l’utilisateur. Les conditions sont les suivantes.

   **Chambre (`Bedroom`)**

   | Zone | Condition d’activation | X1 | Y1 | X2 | Y2 |
   |---|---|---|---|---|---|
   | Armoire ouverte | `BedroomWardrobe` est `Open` | 705 | 140 | 1065 | 565 |
   | Corbeille (sac absent) | `GarbageBag` n’est plus dans la chambre | 1215 | 405 | 1340 | 540 |
   | Lecteur de badge | `BedroomReader` est `Unlocked` | 1670 | 232 | 1730 | 325 |

   **Couloir technique (`TechCorridor`)**

   | Zone | Condition d’activation | X1 | Y1 | X2 | Y2 |
   |---|---|---|---|---|---|
   | Porte de l’escalier ouverte, panneau de sortie et lecteur associé | `StairsReader` est `Unlocked` | 796 | 205 | 990 | 566 |
   | Reflet de la porte ouverte sur le sol | `StairsReader` est `Unlocked` | 830 | 566 | 975 | 775 |

   **Accueil de l’hôpital (`HospitalLobby`)**

   | Zone | Condition d’activation | X1 | Y1 | X2 | Y2 |
   |---|---|---|---|---|---|
   | Porte vitrée de sortie ouverte | `Terminal` est `Activated` | 88 | 150 | 272 | 668 |

   **Toutes ces coordonnées sont provisoires** : estimées à l’œil par Claude sur les images 1774 × 887, à valider ou remplacer par les mesures de l’utilisateur. L’agent les recopie telles quelles, sans les modifier ni les « corriger » à partir des images.
5. **Éléments volontairement sans zone** : dans l’image alternative de l’accueil, la couleur des boutons de l’ascenseur diffère : c’est une erreur de génération, à ignorer (aucune zone). Dans le couloir, les différences du mur et de la porte de gauche sont hors des zones et restent invisibles : ne rien en faire. La rue provisoire n’a ni image ni zone.
6. **Tests** : §5.5, dont la lecture de l’en-tête PNG (largeur et hauteur) pour contrôler les fichiers.

### J6 — Interface : corrections, version, rendu des zones
1. **Version affichée** : classe `GameInfo` (couche Game, fichier `src/Releve.Game/GameInfo.cs`) avec `public const string Version = "v0.3";`, source unique de la version. Dans `MenuScreen.Draw`, afficher cette chaîne **en bas à droite de l’écran de menu, uniquement**, avec la police existante de 20 px (aucune troisième taille), couleur `Palette.DimText`, ombre légère (le même décalage de 1 pixel en noir semi-transparent que le titre, mais plus discret), marge `Layout.Padding` par rapport aux bords. Règle de numérotation à consigner dans l’architecture §2 (Conventions) : « chaque POC n est la version v0.n ».
2. **Rendu des zones** (`RoomViewPanel`, **fichier à relire en J0**) : après avoir dessiné l’image de base étirée sur `Layout.ViewBounds` et **avant** le cadre, pour chaque rectangle de `_view.ActivePatches` : charger (avec le même cache que l’image de base) la texture `<ImageKey>_AltState.png` ; si elle est absente, **ignorer silencieusement** les zones. Sinon dessiner la zone avec `DrawTexturePro` : source = le rectangle `(X1, Y1, X2 − X1, Y2 − Y1)` de la texture alternative ; destination = le même rectangle mis à l’échelle de `ViewBounds`, avec **arrondi séparé de chaque bord** (gauche = round(vx + X1 × échelleX), droit = round(vx + X2 × échelleX), largeur = droit − gauche ; idem en hauteur ; échelle = largeur ou hauteur de `ViewBounds` divisée par largeur ou hauteur de la texture de base). `GameApp.Draw` passe les zones au panneau.
3. **Console** (`ConsolePanel`, **à relire en J0**) :
   - l’invite `> ` de la ligne de saisie prend la couleur de saisie du joueur (`Palette.ConsoleInput`) au lieu de `Palette.Prompt` ;
   - `RecallHistory` (rappel de l’historique par ↑/↓) **ne réagit pas lorsque Ctrl est enfoncé** (Ctrl+↑/↓ sert au défilement de l’inventaire) ;
   - le défilement vers le haut s’arrête lorsque la **première ligne** de l’historique est la première ligne visible (et non quand il ne reste qu’une ligne visible) ;
   - un mot plus large que la zone d’affichage est **coupé** au retour à la ligne (même algorithme qu’au point 4 pour le visualiseur) ;
   - supprimer le champ inutilisé `_scrollHoldTimer`.
4. **Flèches** (`ArrowsPanel`, **à relire en J0**) : pour les rangées **Nord** et **Sud**, l’ensemble « flèche + libellé » est centré horizontalement dans le panneau (aujourd’hui seule la flèche l’est) ; supprimer le paramètre `centred` devenu inutile (toujours vrai).
5. **Visualiseur de texte** (`TextViewer`) :
   - **fermeture à la souris** : `Update()` renvoie **vrai aussi** quand la fermeture vient du bouton « Fermer » (aujourd’hui `UpdateCloseButton` appelle `Close()` mais `Update()` renvoie faux). Ainsi `GameApp` remet `_menuEntered` à vrai et la sélection du menu reste sur « Aide » ;
   - **défilement maximal** : `_scroll` ne dépasse pas `max(0, nombreDeLignes − lignesVisibles)` (pour Page↓, ↓ et Fin), et l’indicateur « plus bas » se base sur cette même limite ;
   - **mots trop larges** : dans `WrapText`, un mot dont la largeur dépasse `maxWidth` est coupé en morceaux (ajouter les caractères un par un à la ligne courante ; dès que la largeur est dépassée, terminer la ligne et poursuivre sur la suivante) ;
   - **répétition** : ↑ et ↓ se répètent lorsque la touche est maintenue (`IsKeyPressed` ou `IsKeyPressedRepeat`) ;
   - **largeur de texte** : `WrapText` reçoit `rectangle.Width − 2 × Layout.Padding − 40` (colonne réservée aux indicateurs de 40 px de large, utilisée au J7).
6. Aucun test automatique pour cette couche (§7) : build au vert et tests moteur inchangés.

### J7 — Souris et fondu
1. **Menu** (`MenuScreen`) :
   - factoriser le calcul du rectangle d’une entrée dans une méthode (`EntryRect(index)`) **identique au rectangle de surbrillance actuel** : x = centre − largeur du libellé / 2, y = 300 + 60 × index, rectangle = `(x − Padding, y − Padding / 2, largeur + 2 × Padding, Fonts.Size + Padding)`. `DrawEntry` et `Update` l’utilisent tous deux ;
   - dans `Update`, en plus du clavier (inchangé) : pour chaque entrée **activée** (l’entrée « Continuer » est ignorée quand `canContinue` est faux) dont le rectangle contient la souris (`GetMousePosition`) : si la souris a **bougé** depuis la dernière image (`GetMouseDelta` non nul), la sélection prend cette entrée (sinon la souris immobile écraserait chaque déplacement au clavier) ; sur **appui du bouton gauche** (`IsMouseButtonPressed`), la sélection prend l’entrée et le choix correspondant est renvoyé comme avec Entrée ;
   - `GameApp` ne change pas pour le reste : il reçoit le même `MenuChoice`.
2. **Indicateurs du visualiseur** (`TextViewer`) :
   - zone de clic de **30 × 30 pixels** centrée sur chaque indicateur (le triangle fait 10 pixels) ;
   - l’indicateur du haut n’existe (dessin et clic) que si `_scroll > 0` ; celui du bas que s’il reste du texte (même condition que le dessin) ;
   - sur **appui du bouton gauche** dans la zone : `_scroll` défile de **3 lignes** (haut : `max(0, _scroll − 3)` ; bas : `min(maxScroll, _scroll + 3)`) ; **pas de répétition à clic maintenu** ;
   - survol : le triangle est dessiné en `Palette.Prompt` au lieu de `Palette.Text` ;
   - **anti-clic fantôme** : le visualiseur ne réagit qu’à un appui (`IsMouseButtonPressed`) survenu **après** son ouverture ; le relâchement du clic qui l’a ouvert (depuis le menu) ne doit déclencher ni « Fermer » ni un indicateur. La logique actuelle de « Fermer » (appui puis relâchement sur le bouton) le garantit déjà ; la conserver telle quelle et appliquer la même règle aux indicateurs ;
   - le visualiseur ne traite aucune entrée souris ni clavier dans la frame où il est ouvert (comportement actuel à conserver).
3. **Fondu** :
   - nouvelle classe `Ui/ScreenFade` : états `Idle`, `Out`, `In` ; durées constantes `OutDuration = 0.6f` et `InDuration = 0.8f` (secondes) ; méthodes `StartOut()`, `StartIn()`, `FadeEvent Update(float dt)` (avec `enum FadeEvent { None, OutFinished, InFinished }`), `Draw()` ; propriété `IsActive`. `Draw` dessine par-dessus tout l’écran un rectangle noir plein d’opacité = `t / OutDuration` (sortie) ou `1 − t / InDuration` (entrée). **Après `StartIn()`, le premier `Update` ignore son `dt`** (le chargement de l’image de la pièce provoque un à-coup qui consommerait le fondu). L’opacité est convertie en octet (`(byte)Math.Clamp(...)`) pour `Color` ;
   - dans `GameApp.Run`, calculer `var dt = MathF.Min(Raylib.GetFrameTime(), 0.05f);` ; **tant que le fondu est actif** : ne mettre à jour ni le menu, ni le jeu, ni le visualiseur ; vider la file de caractères (`GetCharPressed`) à chaque image ; appeler `_fade.Update(dt)` ;
   - `UpdateMenu`, cas `NewGame` : appeler `_fade.StartOut()` **sans** appeler `StartNewGame()` ni mettre `_playing` à vrai ; le menu reste affiché pendant la sortie ;
   - quand `Update` renvoie `OutFinished` : appeler `StartNewGame()`, mettre `_playing` à vrai, puis `_fade.StartIn()` ;
   - à la fin de `Draw`, après le visualiseur : `_fade.Draw()` ;
   - cas `Continue` et `Help` inchangés (aucun fondu) ;
   - la musique de menu continue pendant la sortie ; la piste de jeu démarre dans `StartNewGame`, écran noir ; aucun fondu de volume.
4. Aucun test automatique pour cette couche (§7).

### J8 — Documentation et compte rendu
Voir §8.

## 4. Textes (provisoires, à copier tels quels)

### 4.1 Chambre (`Bedroom.cs`)

Message d’accueil (une seule fois, avant la description) :
```text
Alors que vous ouvrez doucement les yeux, une voix de synthèse annonce : « Réveil terminé, constantes stables. » En vous examinant, vous constatez que vous portez une chemise d’hôpital, la tenue standard des malades alités.
```

Description (à chaque entrée dans la pièce) :
```text
La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu’une armoire.
```

Description détaillée (« observe », « regarde » seul, « regarde pièce »…), sac présent :
```text
La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu’une armoire. Dans un coin se trouve une corbeille et son sac vide. Une porte contrôlée par un lecteur de badge constitue la seule sortie.
```

Description détaillée, sac absent de la chambre :
```text
La pièce est propre, éclairée et sans fenêtre. Du matériel médical autonome est présent, ainsi qu’une armoire. Dans un coin se trouve une corbeille. Une porte contrôlée par un lecteur de badge constitue la seule sortie.
```

### 4.2 Listes de réponses (`Core/FlavourText.cs`)

`UnknownNeutral` (8) :
```text
Commande non comprise.
Je ne saisis pas.
Pas compris.
Je ne comprends pas cette commande.
Hein ? Pardon ?
Ça ne me dit rien.
Essayez de formuler ça autrement.
Cette commande m’est inconnue.
```

`UnknownHumour` (6) :
```text
Vous avez un problème ? Vous voulez qu’on en parle ?
Mais que me dites-vous là…
On se calme, hein ? Essayez de formuler ça autrement.
Mes circuits ont beau chercher, rien ne vient.
Désolé, mon vocabulaire a des limites… et vous venez de les trouver.
Intéressant. Incompréhensible, mais intéressant.
```

`ProfanityReplies` (10) :
```text
Surveillez votre langage !
Si votre maman savait ça…
Tsk, tsk, tsk… que c’est vulgaire.
Jean-Marie Bigard, sors de ce corps !
Hou là là, quel vilain langage.
Voilà qui n’est pas très distingué.
Allons, allons. Un peu de tenue !
Je suis un programme bien élevé : je ne répondrai pas sur ce ton.
Cette remarque sera transmise à qui de droit.
Il y a peut-être des enfants qui jouent. Un peu de retenue !
```
Le caractère « … » est le caractère unique U+2026 (présent dans la police), pas trois points.

### 4.3 Grossièretés (`Parsing/ProfanityFilter.cs`)

Les formes sont écrites **déjà normalisées** (minuscules, sans accents, « œ » devenu « oe »).

Mots isolés :
```text
merde merdes merdique merdiques merdeux merdeuse merdier
emmerde emmerdes emmerder emmerdant emmerdante emmerdeur emmerdeuse
putain putains pute putes putasse putasses
con cons conne connes connard connards connasse connasses connerie conneries conard conasse
encule encules enculee enculees enculer enculeur
enfoire enfoires enfoiree
salaud salauds salope salopes saloperie saloperies salopard salopards
bordel bordels
foutre
chier chie chies chiant chiante chiants chiantes chiasse chieur chieuse
couille couilles couillon couillons burne burnes
bite bites zob
branler branle branleur branleuse branlette branlettes
nique niques niquer
baise baises baiser
pisse pisser
pouffiasse poufiasse petasse petasses garce garces
pourriture crevure raclure
abruti abrutie abrutis cretin cretine cretins debile debiles idiot idiote idiots imbecile imbeciles
trouduc
fdp ntm
```

Expressions (suites de mots) :
```text
va te faire foutre
va te faire enculer
va te faire voir
va chier
fait chier
faire chier
ta gueule
fils pute
fille pute
tete noeud
trou cul
leche cul
lechecul
ras cul
```

**Mots volontairement absents** (sens légitime dans le jeu ou le monde, ne pas les ajouter) : `cul` seul (« cul-de-sac »), `bander`, `fumier`, `ordure`, `bouse`, `batard`, `pouffe`, `chatte`, `gueule` seul, `pine`. Les insultes discriminatoires sont hors périmètre.

## 5. Tests

Les tests existants restent verts, à l’exception de ceux dont le comportement change volontairement (liste en 5.6). Ne pas supprimer un test pour le faire passer : le modifier et le signaler.

### 5.1 `EncodingTests.cs` (nouveau)
- **Scan des données** : pour le monde livré (`GameWorld.Create()`) : titre et description de chaque pièce (état initial), `DetailedDescription` non nul, message bloqué de chaque sortie, nom et `Describe` (état initial) de chaque objet, les listes de `FlavourText`, les constantes de `Messages`. Aucun texte ne contient `'` (U+0027) ni de caractère dans la plage U+0300 à U+036F. Aucun texte ne contient le mot « blouse » (insensible à la casse).
- **Scan des sources** : pour les fichiers `.cs` de `src/Releve.Engine/Content/`, `Core/Messages.cs` et `Core/FlavourText.cs` : repérer les littéraux de chaîne par expression régulière (`"(?:[^"\\]|\\.)*"`), en ignorant les lignes dont le premier caractère non blanc est `/` (commentaires), et vérifier qu’aucun ne contient `'`. Le test retrouve le dossier `src` en remontant depuis `AppContext.BaseDirectory` ; s’il est introuvable, le test **échoue** avec un message clair.

### 5.2 Comportements (J2) : dans `EngineBehaviourTests.cs` et `ParserTests.cs`
- « est badge » et « n ouvre porte » ne sont plus des déplacements (erreur `UnknownVerb` à l’analyse) ; « est », « n », « va est » le sont toujours.
- « vas », « allez », « avance », « avancer » seuls : `MissingObject` (« Précisez l’objet de la commande. »). « sors » dans le couloir : inchangé (« Vous ne pouvez pas aller par là. »).
- `Use` : « utilise badge » (badge dans l’inventaire) : « Précisez sur quoi l’utiliser. » ; « utilise lit » : « Rien ne se passe. ».
- `Read` : « lis afficheur » (accueil) et « lis machines » (chambre) donnent le même texte que `Look` ; « lis porte » : « Il n’y a rien à lire ici. » ; le moteur ne contient plus de test sur `ObjectId.Display` ni `ObjectId.BedroomMachines` (vérification par relecture du code).
- Lecteur de l’escalier : deuxième passage du badge : « Le lecteur de l’escalier est déjà déverrouillé. ».

### 5.3 Chambre (J3) : nouveau fichier `BedroomV3Tests.cs`
- `Start()` sur une nouvelle partie renvoie, dans l’ordre : le message d’accueil, la description, rien d’autre ; l’événement `WakeAnnouncementDone` est enregistré.
- `Start()` sur un état où l’événement est déjà présent (rechargement) renvoie **uniquement** la description (pas le message d’accueil).
- Entrer dans une autre pièce puis revenir dans la chambre : seule la description s’affiche.
- « observe », « regarde », « regarde pièce », « regarde dans la pièce », « regarde autour de moi », « regarde ici » : description détaillée avec « et son sac vide » ; après « prends sac » : la version sans sac.
- Dans le couloir, « observe » renvoie la même chose que la description du couloir (pas de description détaillée).
- « regarde chambre » dans le couloir : « Vous ne voyez pas cela ici. » (« chambre » n’est pas un mot de pièce).
- `WorldConsistencyTests` : aucun alias d’objet ne contient un mot de `Vocabulary.RoomWords`.

### 5.4 Réponses (J4) : nouveau fichier `ReplyTests.cs`
- Chaque liste de `FlavourText` est non vide et sans doublon.
- Avec un `Random` à graine fixe, 500 saisies incomprises : chaque réponse appartient à `UnknownNeutral` ∪ `UnknownHumour`, les deux listes sont utilisées, et deux réponses consécutives ne sont jamais identiques. Même graine : mêmes réponses.
- Chaque mot et chaque expression de `ProfanityFilter` est détecté ; « MERDE !! » et « enculé » sont détectés ; « contre », « conduit », « salopette », « culotte » ne le sont pas.
- « prends le putain de badge bordel » : réponse de `ProfanityReplies` ; **l’état du jeu n’a pas changé** (le badge n’est pas pris, aucune sauvegarde déclenchée : `StateChanged` faux).
- « va te faire foutre » : réponse de `ProfanityReplies` (et non « Vous ne pouvez pas aller par là. »).
- Les erreurs `MissingObject`, `UnknownObject` et `Ambiguous` renvoient toujours leurs messages fixes.
- `WorldConsistencyTests` : aucun alias d’objet ne contient un mot ou une expression de `ProfanityFilter`.

### 5.5 Zones d’image (J5) : nouveau fichier `ImagePatchTests.cs`
- **Monde de test** : ajouter une zone de test dans une pièce de `TestWorld` ; `GetView().ActivePatches` est vide tant que la condition est fausse, contient le rectangle quand elle devient vraie, redevient vide quand elle redevient fausse.
- **Monde livré**, pour chaque pièce déclarant au moins une zone : le fichier `<ImageKey>.png` et `<ImageKey>_AltState.png` existent dans `src/Releve.Game/assets/rooms/` ; leurs dimensions (lues dans l’en-tête PNG : signature de 8 octets, puis largeur sur 4 octets big-endian à l’offset 16 et hauteur à l’offset 20) sont **identiques** entre elles ; chaque rectangle vérifie `0 ≤ X1 < X2 ≤ largeur` et `0 ≤ Y1 < Y2 ≤ hauteur` ; deux zones d’une même pièce ne se chevauchent pas. Le dossier est retrouvé en remontant depuis `AppContext.BaseDirectory` ; introuvable : le test échoue.
- **Chambre** : l’état initial n’a aucune zone active ; « ouvre armoire » active la zone de l’armoire ; « prends sac » active celle de la corbeille ; passer le badge sur le lecteur active celle du lecteur ; « ferme armoire » désactive la zone de l’armoire.
- **Couloir** : à l’état initial, aucune zone active ; après déverrouillage de l’escalier (« utilise badge sur lecteur escalier » avec le badge, dans le couloir), les deux zones du couloir sont actives.
- **Accueil** : aucune zone active à l’état initial ; après « passe le badge sur la borne » (`Terminal` passe à `Activated`), la zone de la porte vitrée est active.
- **Sauvegarde puis rechargement** : mêmes zones actives dans chacune des trois pièces.

### 5.6 Tests existants à adapter volontairement
- Le test du scénario de référence de `PocContentTests` (ordre : message d’accueil, description) et ceux qui citent l’ancien texte de la chambre ou l’ancienne annonce.
- Tout test qui compare le texte exact de `UnknownVerb` côté moteur (voir J4, point 7).
- Le test qui couvre « est badge » (voir 5.2).
- Les compteurs de `WorldConsistencyTests` (4 pièces, 22 objets) ne changent pas.

## 6. Scénario de référence V3 (pour l’utilisateur ; l’agent en rejoue ce qu’il peut par les tests)

1. Menu : « v0.3 » discret en bas à droite. « Nouvelle partie » : fondu au noir, puis fondu d’ouverture sur l’interface ; message d’accueil puis description courte.
2. « observe » : description détaillée avec le sac ; « regarde pièce », « regarde autour de moi » : idem.
3. « ouvre armoire » : l’armoire s’ouvre **dans l’image**. « prends sac » : le sac disparaît de la corbeille **dans l’image** ; « observe » ne cite plus le sac. « prends badge », « utilise badge sur lecteur » : le voyant du lecteur passe au vert **dans l’image**.
3 bis. Couloir : « utilise badge sur lecteur escalier » : la porte du fond s’ouvre **dans l’image**, le panneau et le lecteur passent au vert, un reflet apparaît au sol. Accueil : « passe le badge sur la borne » : la porte vitrée de sortie s’ouvre **dans l’image**.
4. Quitter (« quitter »), relancer, « Continuer » : l’armoire, la corbeille et le lecteur sont dans le même état ; pas de fondu ; pas de message d’accueil.
5. « sefkljfsdlmsfjlmfj » plusieurs fois : réponses variées, parfois humoristiques, jamais la même deux fois de suite.
6. « merde », « prends le putain de badge bordel », « va te faire foutre » : réprobation, rien n’est exécuté.
7. « vas » : « Précisez l’objet de la commande. » ; « utilise lit » : « Rien ne se passe. » ; « n’ouvre pas la porte » : réponse d’incompréhension.
8. Couloir : « observe » = description courte ; deuxième passage du badge sur le lecteur de l’escalier : « Le lecteur de l’escalier est déjà déverrouillé. ».
9. Accueil : « lis afficheur » : la date.
10. Menu à la souris : survol d’une entrée = sélection ; clic sur « Continuer » (si sauvegarde), « Nouvelle partie », « Aide » fonctionnent. « Aide » : clic sur les indicateurs fait défiler de 3 lignes ; clic sur « Fermer » ramène au menu avec la sélection **sur « Aide »**.
11. Console : Ctrl+↑/↓ ne rappelle plus l’historique ; l’invite `> ` a la couleur de la saisie ; le défilement s’arrête sur la première ligne de l’historique ; titre « Déplacements possibles : » accentué ; libellés Nord/Sud centrés avec leur flèche.

## 7. Ce que l’agent peut et ne peut pas vérifier

L’agent **n’a pas** de moyen d’observer l’écran ni d’envoyer des touches ou des clics. Il vérifie donc ce qui relève du build, des tests et de la relecture du code ; tout ce qui est visuel ou interactif **attend l’utilisateur**.

| Élément | Par les tests | Par relecture du code | Validation humaine |
|---|---|---|---|
| Moteur : analyseur, réponses, grossièretés, chambre, `Read`, `Use`, zones actives, encodage | oui | oui | non |
| Fichiers d’images (existence, dimensions, rectangles dans l’image) | oui | — | non |
| Rendu des zones d’image (raccords, découpe, position) | non | oui (arrondis, ordre de dessin) | **oui** |
| Menu : survol, clic, version affichée | non | oui | **oui** |
| Visualiseur : clic sur indicateurs, « Fermer » à la souris, sélection du menu conservée | non | oui | **oui** |
| Fondu : durées, fluidité, saisie bloquée | non | oui | **oui** |
| Console, flèches : couleurs, centrage, défilement, mots coupés | non | oui | **oui** |
| Répétition de Retour arrière et de touche maintenue | non | oui | **oui** |

## 8. Documentation et compte rendu

### 8.1 Architecture (V0.5 vers **V0.6**)
Relire le document **en entier** et mettre à jour **toutes** les sections impactées (le POC V2 en avait oublié). Au minimum :
- en-tête et « État de cette version » ; §1 (avancement, nombre de tests) ; §2 (règle d’interaction à quatre exceptions, règle de numérotation « POC n = v0.n » dans les conventions) ; §3 (tests) ;
- §4.1 (arborescence : nouveaux fichiers moteur et Game) ; §4.3 (cycle d’une commande : contrôle des grossièretés avant l’analyse) ;
- §5.6 (`RoomBase` : `IntroMessages`, `DetailedDescription`, `ImagePatches`) ; §5.7 (`RoomView.ActivePatches`, `GameEngine` : constructeur) ; ajout des types `ImagePatch` et `ImageRect` ;
- §6.3 à §6.7 (direction seule, verbes seuls, mots de pièce, réponses d’incompréhension) ; §7.1 à §7.4 (répartiteur, `Look` détaillé, `Use`, `Read`, ordre introduction / description / entrée) ;
- §9.1 (boucle : fondu), §9.2 et §9.3 (zones d’image, console, flèches), §9.7 (menu : souris, version), §9.8 (visualiseur : corrections, indicateurs), §9.9 (règle d’interaction) ;
- §10 (étendre le jeu : zones d’image, message d’introduction, description détaillée, fichier alternatif) ; §11 (tests : nouveaux fichiers, nombre) ; §12.3 (jalon POC V3) et §12.4 ; §14 ; §15 (retirer les écarts corrigés, ajouter ceux qui restent) ; §16 (outillage : souris et fondu non vérifiables par l’agent).
- Inscrire en **tête de la section modifiée** ce qui change, pas seulement en fin de document. Vérifier la cohérence des numéros de section cités.

### 8.2 Autres documents
- `AGENTS.md` : contrat d’assets (`assets/rooms/<ImageKey>_AltState.png`, mêmes dimensions que l’image de base), mention du POC V3, règle de numérotation de la version, limites de vérification de l’agent.
- `README.md` : liste des assets, version affichée.
- `agentTools/README.md` : mentionner que la souris et le fondu ne sont pas automatisables ; **et** signaler que la recette de rejeu du compte rendu du POC V2 appelle `Wait-ReleveGameWindow`, absente de la liste des fonctions du module (ne pas corriger sans l’avoir testée).

### 8.3 Compte rendu `Docs/POC_V3/CompteRenduPOC_03.md`
Sections obligatoires :
1. **Résultat** (jalons terminés, nombre de tests avant et après).
2. **Travail réalisé**, jalon par jalon (fichiers modifiés).
3. **Constats [code]** : pour chacun, confirmé ou infirmé, avec le fichier consulté.
4. **Vérifié par les tests** (liste des fichiers de tests et ce qu’ils couvrent).
5. **Vérifié par relecture du code seulement.**
6. **Attend la validation humaine** : reprendre le tableau du §7, avec ce qui est précisément à regarder (une phrase par point).
7. **Écarts et besoins signalés** (tout ce qui est hors liste du §2, toute incohérence).
8. **Zones d’image non fournies** : rappeler que le couloir et l’accueil n’ont aucune zone.
9. **Points à valider** (§9).
La section « vérifié dans le jeu réel » **ne doit être présente que si l’agent a réellement observé le jeu** ; sinon, l’écrire explicitement : « non vérifié visuellement par l’agent ».

## 9. Points à valider par l’utilisateur
1. Coordonnées des zones d’image des trois pièces (toutes provisoires), et conservation ou suppression de la zone du reflet au sol du couloir.
2. Textes du message d’accueil, des descriptions de la chambre, des deux listes d’incompréhension et des réponses aux grossièretés.
3. Liste des grossièretés (périmètre volontairement modeste) ; probabilité d’humour (0,25).
4. Durées du fondu (0,6 s et 0,8 s) ; position et discrétion du « v0.3 ».
5. Taille des zones de clic (menu, indicateurs de 30 × 30 pixels) ; défilement de 3 lignes par clic.
6. Raccords visibles des zones d’image (tramage du fond des images générées).
7. Constats **[code]** à confirmer par J0 : C1 à C8 (ordre `description` puis `OnEnter`, `Look` sans cible, direction en premier mot, verbes seuls, `Read` et `Use`, fermeture à la souris de l’aide, absence de souris dans le menu, apostrophes droites, `Â§6.6`).

## 10. Pistes pour la suite (hors périmètre, à titre de mémoire)
Pénalité amusante pour les grossièretés (à la manière d’Oxphar ou du Passager du temps), injures discriminatoires (réponse distincte), descriptions détaillées du couloir et de l’accueil, déclenchement du visualiseur depuis le moteur (lettre des parents, crédits), vrai texte d’aide, molette de souris, variantes d’image à plus de deux états, fondu sonore, support Linux/macOS.