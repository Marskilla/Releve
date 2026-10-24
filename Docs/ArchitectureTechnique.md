# Jeu "Relève" — Architecture technique

> Document de référence — version 0.6
> Valable pour toute la durée du projet. Il décrit la structure du code, pas le contenu du jeu.
> À fournir à l’agent de développement avec la note d’intention (V6) et le mockup bitmap de l’interface. Le document univers/scénario n’est pas nécessaire pour ce qui est purement technique.
> Les documents ne portent plus de numéro de version dans leur nom de fichier : la version figure dans l’en-tête de chacun.

> **État de cette version.** La V0.1 décrivait l’architecture cible, la V0.2 le reflet du code livré à l’issue de l’initialisation, la V0.3 les changements du PoC V1 (contenu réel, tests de contenu, outillage `agentTools/`). La V0.4 n’avait repris que certaines sections pour le PoC V2. La V0.5 reprenait l’ensemble du document pour le rendre cohérent avec le code livré à l’issue du PoC V2. **La V0.6 intègre le PoC V3** : message d’accueil, descriptions détaillées, réponses variées, filtre grossièretés, zones d’image selon l’état, raccourcis clavier de déplacement, scroll console enrichi, « regarde pièce » ; **128 tests** passent. Les écarts connus sont listés au §15. Le plan et le compte rendu de chaque PoC sont dans `Docs/POC_V1/`, `Docs/POC_V2/` et `Docs/POC_V3/`.

## 1. But et portée

Ce document définit l’architecture technique du jeu d’aventure textuel et graphique « Relève » : plateforme, découpage du code, modèle de domaine, analyseur de commandes, exécution, sauvegarde et couche d’affichage. Il sert de référence pour tout développement ultérieur.

Il ne contient pas le contenu du jeu (lieux, objets, textes). Celui-ci est décrit dans des documents séparés qui s’appuient sur ce document.

**Avancement au moment de cette version.** Les trois jalons d’initialisation du §12.3, le contenu du PoC V1 (quatre lieux, 22 objets, chambre de réveil comme départ) et les évolutions du PoC V2 (vocabulaire et mots de sortie, console colorée avec boutons de défilement, flèches en croix, image du lieu étirée, menu avec image de fond, visualiseur de texte et page d’aide, sauvegarde après chaque changement d’état) sont livrés. **Le PoC V3 ajoute** : message d’accueil unique, descriptions détaillées selon l’état, réponses variées (neutres et humoristiques) aux commandes incomprises, filtre de grossièretés avec réponses dédiées, zones d’image actives selon l’état du jeu (superposition `*_AltState.png`), raccourcis clavier de déplacement (flèches directionnelles), « regarde pièce » (mots de pièce), scroll console enrichi (Home/End/PageUp/PageDown/Up/Down avec répétition). `dotnet build` est vert et les **128 tests** du moteur passent. Un dossier d’automatisation hors build (`agentTools/`, §16) permet de piloter la fenêtre réelle du jeu pour les vérifications, selon les capacités de l’agent utilisé.

## 2. Principes directeurs

- **Petit développement « à l’ancienne »** : le projet est un seul jeu, pas un moteur multi-jeux.
- **Contenu en classes C#** : lieux, objets, textes, alias, sorties sont écrits dans le code. Aucun fichier JSON/YAML de paramétrage du contenu. (Le JSON n’est utilisé que pour la sauvegarde. Les seuls fichiers de contenu lus au runtime sont l’image de fond du menu, les images de lieux, le texte d’aide et l’audio.)
- **Moteur isolé** : le projet du moteur ne dépend ni de raylib ni d’aucune bibliothèque d’affichage ou de son.
- **Pas de MVVM ni de framework UI** : l’interface est un rendu raylib qui lit l’état du moteur à chaque image.
- **Pas de sur-élaboration** : ne rien ajouter qui ne soit pas décrit dans les documents fournis.
- **En cas de contradiction ou d’ambiguïté** entre documents : signaler le point à l’utilisateur au lieu de trancher seul. La note d’intention fait référence pour l’interface et le comportement du jeu, sous réserve de la règle d’interaction ci-dessous. Deux contradictions ont été tranchées de cette façon et sont signalées aux §9.6 et §14.
- **Règle d’interaction (amende la note d’intention V5, §2).** Tout doit rester faisable au clavier. La souris n’est admise que pour des exceptions explicites, toutes doublées d’un équivalent clavier : (1) deux boutons cliquables (haut, bas) pour faire défiler l’historique de la console (équivalent : Page↑ / Page↓) ; (2) le bouton « Fermer » du visualiseur de texte (équivalent : Échap ou Entrée). Le curseur de la souris est visible. Les flèches de déplacement restent des indicateurs, jamais des boutons.

### Conventions

- Identifiants de code en anglais, textes affichés au joueur en français, commentaires brefs en français.
- Textes joueur : jamais de pensées, souvenirs ni émotions attribués au joueur ; formulations non genrées (pas de participe passé accordé au joueur).
- Dans chaque classe de pièce, les textes sont des constantes regroupées en tête de classe, séparées de la logique.
- **Encodage des textes joueur** : caractères **précomposés** et apostrophe typographique `’` obligatoires. raylib dessine chaque codepoint indépendamment : un accent décomposé (`e` + U+0301) s’affiche en charabia. Vérifier par recherche de `[\u0300-\u036f]` après toute modification de texte. Vérifier aussi l’absence d’apostrophe droite `'` dans les constantes de texte joueur (voir §15, écart connu).
- Une sortie impossible est expliquée par un message qui décrit ce qui bloque, **sans dire comment débloquer** (note d’intention §2).
- **Règle de numérotation** : PoC n = version v0.n de ce document (PoC V3 → v0.6). La version figure en en-tête, pas dans le nom de fichier.
- **Règle de dépôt** : l’agent n’exécute aucune commande git d’écriture (règle d’`AGENTS.md`) ; l’utilisateur commite lui-même.

## 3. Choix techniques

| Sujet | Décision |
|---|---|
| Plateforme | .NET 8, C# 12, `Nullable` activé. Fin de support de .NET 8 prévue le 10/11/2026 ; migration non planifiée (choix assumé par l’utilisateur, il ne faut pas y revenir sans son accord). |
| Affichage | Bibliothèque **Raylib-cs** (`Raylib-cs`), version **8.1.0**, fenêtre fixe. Aucune autre bibliothèque d’affichage. |
| Cible | Windows x64, publication autonome (runtime copié à côté de l’exe). |
| Portabilité | L’architecture doit permettre Linux/macOS sans refonte du moteur. Aucun build Linux/macOS n’est livré à ce stade. |
| Éditeur | VS Code et ligne de commande `dotnet`. Aucun concepteur visuel requis. |
| Tests | xUnit sur le moteur (**128 tests**). |
| Sauvegarde | JSON chiffré (AES-GCM), un seul fichier, dossier Documents de l’utilisateur. |
| Versions de paquets | Gestion centralisée dans `Directory.Packages.props` ; aucune version dans un `.csproj`. Raylib-cs 8.1.0, Microsoft.NET.Test.Sdk 17.14.1, xunit 2.9.3, xunit.runner.visualstudio 2.8.2. |
| RuntimeIdentifier | `win-x64` **fixé** dans `src/Releve.Game/Releve.Game.csproj` : la bibliothèque native `raylib.dll` est copiée à côté de l’exe dans tous les modes de build. |

Commandes de référence :

```text
dotnet build
dotnet test
dotnet run --project src/Releve.Game
dotnet publish src/Releve.Game -c Release -r win-x64 --self-contained
```

Conséquence du `RuntimeIdentifier` fixe : la sortie du projet Game se trouve sous `bin/<Configuration>/net8.0/**win-x64**/`, et non sous `net8.0/`. Les configurations de lancement (`.vscode/launch.json`) dépendent de ce chemin.

Les dossiers d’assets sont copiés dans le répertoire de sortie (`<None Include="assets\**\*" CopyToOutputDirectory>`). **Vérifié après publication** : `raylib.dll`, les assets (dont `menu/` et `texts/`, vérifiés par l’utilisateur au PoC V2) et la police sont bien à côté de `Releve.Game.exe` (§14, point 6).

## 4. Architecture générale

### 4.1 Projets

```text
Releve/
  Directory.Build.props        (TargetFramework net8.0, Nullable, ImplicitUsings, LangVersion 12)
  Directory.Packages.props     (versions des paquets centralisées)
  Releve.sln
  AGENTS.md                    règles de travail du dépôt pour les agents
  src/
    Releve.Engine/             bibliothèque de classes, AUCUNE dépendance à raylib
      GlobalUsings.cs          alias WorldModel (voir note ci-dessous)
      GameEngine.cs            Execute / Start / GetView
      Core/
        Domain.cs              GameAction, Direction, ObjectState, ExitState, LocationKind,
                                Command, Location, ObjectDef, Exit, CommandResult, InventoryItem, RoomView,
                                ImagePatch, ImageRect
        GameState.cs           état mutable, seul élément sauvegardé
        Messages.cs            textes neutres du moteur (§6.7, §7.3)
        FlavourText.cs         listes réponses variées (UnknownNeutral, UnknownHumour, ProfanityReplies) (§7.5)
        ReplyPicker.cs         sélecteur sans répétition consécutive par liste (§7.5)
      Parsing/
        TextNormalizer.cs      normalisation (§6.2)
        Vocabulary.cs          tables verbes / directions, mots de fermeture, **mots de pièce** (§6.3, §6.7)
        CommandParser.cs       analyse, mots de sortie, **mots de pièce**, résolution des noms (§6.4-§6.6, §6.7)
        ParseResult.cs         ParseError, ParseResult
        ProfanityFilter.cs     filtre grossièretés (mots isolés + expressions) (§6.8)
      World/
        Ids.cs                 RoomId, ObjectId, GameEvent, extension Directions.Opposite
        World.cs               RoomBase et World (point de construction unique)
      Content/
        GameWorld.cs           assemblage du monde livré (§12.2)
        Rooms/                 Bedroom, TechCorridor, HospitalLobby, Street
        Objects/               BedroomObjects, TechCorridorObjects, HospitalLobbyObjects
      Persistence/
        SaveData.cs            ISaveStore, SaveData, SaveLocation
        SaveSerializer.cs      JSON (enums par nom)
        SaveCrypto.cs          AES-256-GCM
        EncryptedFileSaveStore.cs  fichier unique, écriture atomique
    Releve.Game/               exécutable, dépend de Releve.Engine et de Raylib-cs
      Program.cs               point d'entrée
      GameApp.cs               boucle principale, états menu / jeu / visualiseur (§9.1)
      Ui/
        Layout.cs              constantes de disposition (§9.2)
        Palette.cs             couleurs de l'interface
        Fonts.cs               police TTF, deux tailles, codepoints (§9.5)
        Assets.cs              localisation du dossier assets
        RoomViewPanel.cs       vue du lieu, rectangle de remplacement, **zones AltState** (§9.3)
        ArrowsPanel.cs         croix des déplacements
        InventoryPanel.cs      inventaire à libellés
        ConsolePanel.cs        historique typé, saisie, **défilement clavier enrichi (Home/End/PageUp/PageDown/Up/Down), boutons souris** (§9.3)
        MenuScreen.cs          menu à trois entrées, image de fond
        TextViewer.cs          visualiseur de texte modal réutilisable (§9.8)
      Audio/
        MusicPlayer.cs         bandes son en boucle (§9.6)
      assets/                  rooms/ (base + *_AltState.png), fonts/, audio/, menu/, texts/
  tests/
    Releve.Engine.Tests/
      TestWorld/TestWorld.cs   monde de test indépendant du contenu (§11)
      TestSupport.cs           fabrication d'un moteur prêt à jouer
      WorldConsistencyTests.cs, NormalizationTests.cs, ParserTests.cs, ExitWordsTests.cs,
      EngineBehaviourTests.cs, PocContentTests.cs, SaveTests.cs,
      **BedroomV3Tests.cs, ReplyTests.cs, ImagePatchTests.cs, EncodingTests.cs**
  Docs/                        documents de référence ; POC_V1/, POC_V2/, **POC_V3/** : plans et comptes rendus
  agentTools/                  automatisation hors build (§16)
  .kilo/, _ContentSources/     configuration de l'agent et sources de contenu : ne pas modifier
```

> **Collision de noms à connaître** : la classe `World` et l’espace de noms `Releve.Engine.World` portent le même nom. Dans les fichiers qui font `using Releve.Engine.World;`, l’identifiant `World` est ambigu ; un alias global est donc déclaré dans `GlobalUsings.cs` : `WorldModel = Releve.Engine.World.World`. Tout nouveau fichier manipulant le monde utilise `WorldModel` comme type de paramètre (comme `GameEngine` et `GameWorld`).

### 4.2 Règle de dépendance

`Releve.Game` → `Releve.Engine`, jamais l’inverse. Le moteur ne contient ni dessin, ni lecture de touches, ni son. Il reçoit du texte, renvoie du texte et un état à afficher. Le projet de tests ne référence que le moteur : la couche Game n’a pas de test automatique (§11).

### 4.3 Cycle d’une commande

```text
Saisie (Game) ──► GameEngine.Execute(texte)
                       │
                       ├─ Contrôle grossièretés (ProfanityFilter) ─► réponse ProfanityReplies si détecté
                       ├─ CommandParser.Parse(texte, objets accessibles, mots de sortie de la pièce)
                       │                                   ─► Command ou erreur
                       ├─ Go ? ──► sans direction : « Vous ne pouvez pas aller par là. »
                       │           avec direction : déplacement (sorties conditionnées)
                       ├─ Vérification de l'instrument (Use) ─► refus éventuel
                       ├─ RoomBase.Handle(command, état) ─► résultat propre à la pièce, ou null
                       └─ Comportements par défaut ───► résultat générique
                       ▼
                   CommandResult (messages) + GameState modifié
Game affiche les messages dans la console, sauvegarde si StateChanged, puis relit engine.GetView().
```

## 5. Modèle de domaine (projet Engine)

### 5.1 Énumérations

Les identifiants sont des `enum` (pas de chaînes) : le compilateur détecte les fautes de frappe. **Dans la sauvegarde, les enums sont écrits par leur nom**, jamais par leur valeur numérique.

```csharp
public enum GameAction { Look, Take, Drop, Open, Close, Use, Talk, Read, Go, Inventory }
public enum Direction { North, South, East, West }
public enum ObjectState { None, Closed, Open, Locked, Unlocked, Activated }
public enum ExitState { Absent, Blocked, Open }   // Absent = rien affiché ; Blocked = flèche creuse ; Open = flèche pleine
public enum LocationKind { Room, Inventory, Nowhere }
```

Les enums de contenu sont enrichis par les documents de contenu, **et également par le monde de test** : les membres `TestRoom*`, `Test*` et `TestAnnouncePlayed` n’existent que pour `TestWorld` et ne doivent jamais apparaître dans le monde réel (§11). Contenu livré à ce jour (les membres de test, inchangés, ne sont pas relistés ici) :

```csharp
public enum RoomId
{
    Bedroom, TechCorridor, HospitalLobby, Street,      // monde livré (PoC V1)
    TestRoomStart, TestRoomVault, TestRoomWall         // monde de test uniquement
}

public enum ObjectId
{
    // Chambre de réveil
    BedroomBed, BedroomMachines, BedroomWardrobe, ChildClothes, Badge,
    BedroomReader, BedroomDoor, BedroomBin, GarbageBag,
    // Couloir technique
    StairsDoor, StairsReader, TechDoor, TechReader,
    // Accueil de l'hôpital
    Android, Terminal, Display, Wheelchairs, GlassDoor, Plants,
    Elevator, ElevatorReader, ElevatorButton,
    // Monde de test uniquement
    TestKey, TestCabinet, TestChest, TestBook, TestAccountBook, TestHiddenNote
}

public enum GameEvent { WakeAnnouncementDone, TestAnnouncePlayed }
```

Le fichier `Ids.cs` contient aussi l’extension `Directions.Opposite`, utilisée par les tests de cohérence.

### 5.2 Commande normalisée

```csharp
// Convention pour Use avec deux compléments : le PREMIER nom est l'instrument, le SECOND est la cible.
// "utilise badge sur lecteur" -> Instrument = badge, Target = lecteur.
public sealed record Command(
    GameAction Action,
    ObjectId? Target = null,
    ObjectId? Instrument = null,
    Direction? Direction = null);
```

Une commande `Go` produite par l’analyseur ne porte jamais de cible objet : elle a une direction, ou aucune (§6.5).

### 5.3 Emplacement et état

```csharp
public readonly record struct Location(LocationKind Kind, RoomId? Room = null)
{
    public static Location InRoom(RoomId r) => new(LocationKind.Room, r);
    public static Location Inventory => new(LocationKind.Inventory);
    public static Location Nowhere => new(LocationKind.Nowhere);
}

public sealed class GameState   // mutable, seul élément sauvegardé
{
    public RoomId CurrentRoom { get; set; }
    public Dictionary<ObjectId, Location> Locations { get; } = new();
    public Dictionary<ObjectId, ObjectState> States { get; } = new();
    public HashSet<GameEvent> Events { get; } = new();

    public ObjectState StateOf(ObjectId id) => States.GetValueOrDefault(id, ObjectState.None);
    public bool IsInInventory(ObjectId id) => Locations.GetValueOrDefault(id).Kind == LocationKind.Inventory;
}
```

Un objet n’appartient pas à une pièce : il a un **emplacement**, qui change au fil du jeu (un objet pris passe dans l’inventaire).

### 5.4 Objets

Un objet est défini une seule fois (définition immuable). Son emplacement et son état courants vivent dans `GameState`.

```csharp
public sealed record ObjectDef(
    ObjectId Id,
    string Name,                          // libellé affiché (inventaire, listes d'ambiguïté)
    string[] Aliases,                     // formes que le joueur peut saisir (voir §6.2)
    bool Portable,
    Location InitialLocation,
    ObjectState InitialState,
    Func<GameState, string> Describe,     // texte de « regarde X », peut dépendre de l'état
    Func<GameState, bool>? VisibleWhen = null);   // null = visible dès qu'il est présent
```

### 5.5 Sorties

Chaque sortie appartient à la pièce de **départ** : l’aller et le retour sont deux sorties distinctes. Il n’y a pas d’objet « porte » partagé entre deux pièces. Une sortie peut être conditionnée par **l’état d’un objet de la pièce** (lecteur de badge, serrure).

```csharp
public sealed record Exit(
    Direction Dir,
    RoomId? To,                             // null = sortie sans destination (fausse piste) : toujours bloquée
    Func<GameState, bool>? IsOpen = null,   // null = toujours ouverte (ignoré si To est null)
    string? BlockedMessage = null,
    bool OneWay = false,                    // retour volontairement absent côté destination
    IReadOnlyList<string>? Words = null);   // mots de sortie normalisés (§6.5) ; null = aucun
```

`Words` permet de franchir la sortie sans donner de direction cardinale (« sors », « va dans la rue », « monte l’escalier »). Les mots doivent être fournis **déjà normalisés** (§6.2) par le contenu : le moteur ne les normalise pas, `WorldConsistencyTests` le vérifie. Ils sont non vides, uniques dans une pièce et ne contiennent aucune direction cardinale. `GameEngine.ExitWords()` construit à chaque commande la table mot → direction de la pièce courante.

Les trois états d’affichage sont **calculés**, jamais stockés (`GameEngine.GetView` parcourt les quatre directions à chaque appel) : pas de sortie dans la direction → `Absent` ; sortie sans destination, ou condition fausse → `Blocked` ; sinon `Open`.

### 5.6 Pièces

Une classe par pièce, **sans état propre** : toutes les méthodes reçoivent l’état en paramètre. Les comportements spécifiques (par exemple « passer le badge sur le lecteur », « ouvre porte ») vivent dans `Handle` de la pièce où se trouve la cible.

```csharp
public abstract class RoomBase
{
    public abstract RoomId Id { get; }
    public abstract string Title { get; }
    public abstract string ImageKey(GameState s);                   // clé d'image, variante possible selon l'état
    public abstract string Description(GameState s);                // texte affiché à l'entrée
    public virtual IEnumerable<string> IntroMessages(GameState s) => Array.Empty<string>(); // messages d'introduction (uniques, avant Description)
    public virtual string? DetailedDescription(GameState s) => null; // description détaillée pour « regarde » sans cible
    public virtual IReadOnlyList<ImagePatch> ImagePatches => Array.Empty<ImagePatch>();      // zones d'image actives selon l'état
    public virtual IEnumerable<string> OnEnter(GameState s) => Array.Empty<string>();        // messages d'entrée supplémentaires (après Description)
    public abstract IReadOnlyList<Exit> Exits { get; }
    public virtual CommandResult? Handle(Command c, GameState s) => null;   // null = non géré : comportements par défaut
}
```

Un `Handle` qui modifie l’état de la partie doit renvoyer `CommandResult.Changed(...)` : c’est ce qui déclenche la sauvegarde automatique (§8).

### 5.7 Résultat, vue et monde

```csharp
public sealed record CommandResult(IReadOnlyList<string> Messages, bool StateChanged = false)
{
    public static CommandResult Say(params string[] m) => new(m);
    public static CommandResult Changed(params string[] m) => new(m, true);
}

public sealed record InventoryItem(ObjectId Id, string Name);

/// <summary>Zone d'image : rectangle source [X1,X2) x [Y1,Y2) et condition d'activation.</summary>
public sealed record ImagePatch(int X1, int Y1, int X2, int Y2, Func<GameState, bool> IsActive);

/// <summary>Rectangle d'image (coordonnées source).</summary>
public readonly record struct ImageRect(int X1, int Y1, int X2, int Y2);

public sealed record RoomView(
    RoomId Room,
    string Title,
    string ImageKey,
    IReadOnlyDictionary<Direction, ExitState> Exits,
    IReadOnlyList<InventoryItem> Inventory,
    IReadOnlyList<ImageRect> ActivePatches)
{
    // Constructeur de compatibilité pour les appels existants sans ActivePatches
    public RoomView(RoomId room, string title, string imageKey,
        IReadOnlyDictionary<Direction, ExitState> exits, IReadOnlyList<InventoryItem> inventory)
        : this(room, title, imageKey, exits, inventory, Array.Empty<ImageRect>())
    {
    }
}

// Alias WorldModel dans GlobalUsings.cs (voir §4.1).
public sealed class World   // point de construction unique : assemble toutes les pièces et tous les objets
{
    public World(RoomId startRoom, IReadOnlyDictionary<RoomId, RoomBase> rooms,
                 IReadOnlyDictionary<ObjectId, ObjectDef> objects);
    public RoomId StartRoom { get; }
    public IReadOnlyDictionary<RoomId, RoomBase> Rooms { get; }
    public IReadOnlyDictionary<ObjectId, ObjectDef> Objects { get; }
    public GameState CreateInitialState();   // CurrentRoom = StartRoom ; emplacement et état initiaux de chaque objet
}

public sealed class GameEngine
{
    public GameEngine(WorldModel world, GameState state, Random? random = null);
    public GameState State { get; }
    public IReadOnlyList<string> Start();    // à appeler au démarrage : IntroMessages, Description, OnEnter de la pièce courante
    public CommandResult Execute(string input);
    public RoomView GetView();
}
```

## 6. Analyseur de commandes

### 6.1 Principe

Vocabulaire contrôlé, deux étages : (1) **normalisation et reconnaissance** du texte, (2) **résolution des noms** dans la portée courante. L’analyseur ne cherche pas à comprendre le langage naturel.

### 6.2 Normalisation (identique pour la saisie, les alias et les mots de sortie)

```text
Normaliser(texte):
  1. minuscules (culture invariante), œ -> oe, æ -> ae (avant décomposition)
  2. suppression des accents (décomposition Unicode FormD, rejet des marques non espacées)
  3. apostrophes (' et ’) remplacées par un espace  ("l'armoire" -> "l armoire")
  4. ponctuation, symboles et espaces multiples supprimés/réduits
  5. découpage en mots
  6. suppression des articles : le la les l un une des du de d
  7. prépositions ramenées à une forme canonique : sur, avec, a (à, au, aux), dans, vers, contre
```

Les alias des objets sont normalisés par la même fonction au chargement : « lecteur de badge » devient « lecteur badge ». « clé » et « clef » sont deux alias du même objet. Les mots de sortie, eux, sont écrits normalisés dans le contenu (« hopital », pas « hôpital »).

### 6.3 Verbes reconnus

Le verbe est le premier mot après normalisation. Cette table est celle de `Vocabulary.Verbs` ; elle s’étend au fil du projet.

| GameAction | Formes acceptées (normalisées) |
|---|---|
| Look | regarde, regarder, examine, examiner, observe, observer, inspecte, inspecter |
| Take | prends, prend, prendre, prenez, prennez, ramasse, ramasser, saisis, saisir |
| Drop | pose, poser, depose, deposer, lache, lacher, jette, jeter |
| Open | ouvre, ouvrir, ouvrez |
| Close | ferme, fermer, fermez |
| Use | utilise, utiliser, utilisez, passe, passer, applique, appliquer, appuie, appuyer, pousse, pousser, actionne, actionner |
| Talk | parle, parler, discute, discuter, interroge, interroger, demande, demander |
| Read | lis, lire, lisez |
| Go | va, vas, aller, allez, avance, avancer, sors, sortir, monte, monter, descends, descendre, entre, entrer, rentre, rentrer, retourne, retourner |
| Inventory | inventaire, inv, i |

Directions : `nord`, `sud`, `est`, `ouest` (abréviations `n`, `s`, `e`, `o`). Toute commande dont le **premier mot** est une direction (ou une abréviation) est un déplacement, quel que soit le reste de la phrase (« est badge » va à l’est ; voir §15 pour les conséquences sur les élisions). Une direction précédée de `va` / `va vers` / `aller a` équivaut à `Go`.

La fermeture du jeu est interceptée par la couche Game avant d’appeler le moteur. Trois formes sont reconnues, normalisées (`Vocabulary.QuitWords`) : `quitter`, `quitte`, `quittes`. La commande n’est **pas** affichée en écho dans la console : `GameApp.Submit` détecte le mot et demande la fermeture avant tout écho.

### 6.4 Structures de phrase

```text
<verbe>                       regarde, inventaire, lis
<direction>                   est
<verbe> <nom>                 prends badge
<verbe> <prép> <nom>          parle a androide, appuie sur bouton   (la préposition est ignorée)
<verbe> <nom1> <prép> <nom2>  utilise badge sur lecteur             (nom1 = instrument, nom2 = cible)
<mot de sortie>               sors, monte                           (verbe de déplacement ou mot de la pièce)
<verbe Go> [prép] <mot de sortie>   va dans la rue, monte l'escalier
```

### 6.5 Pseudo-code

```text
Parse(input, scope, exitWords = null):   // scope = objets accessibles (§6.6) ; exitWords = mot -> direction (pièce courante)
  mots = Normaliser(input)
  si mots vide -> Erreur(Empty)

  // 1. direction seule : testée AVANT la table des verbes
  si mots[0] est une direction -> Succès(Go, direction)

  verbe = TableVerbes[mots[0]]  ; si absent -> Erreur(UnknownVerb)
  reste = mots[1..]

  // 2. verbes de la classe Go : jamais de résolution d'objet
  si verbe est de la classe Go -> return ParseGo(verbe, reste, exitWords)

  si reste vide:
      si verbe in {Look, Inventory, Read} -> Succès(verbe)
      sinon -> Erreur(MissingObject)       // "Prendre quoi ?"

  si reste[0] est une préposition -> la retirer (si rien ne reste : MissingObject)

  // J3.5 : pour Look seulement, si tous les mots restants (hors prépositions) sont dans RoomWords
  // et qu'au moins un n'est pas "moi" -> Look sans cible.
  si verbe == Look:
      nonPrep = mots de reste qui ne sont pas préposition
      si nonPrep non vide et tous dans RoomWords et au moins un != "moi":
          -> Succès(Look)

  groupe1 = mots de reste jusqu'à la prochaine préposition
  groupe2 = mots après cette préposition (peut être vide)

  cible1 = Résoudre(groupe1, scope)        // peut échouer : UnknownObject ou Ambiguous
  si groupe2 non vide:
      cible2 = Résoudre(groupe2, scope)
      -> Succès(verbe, Instrument = cible1, Target = cible2)
  sinon:
      -> Succès(verbe, Target = cible1)

ParseGo(verbe, reste, exitWords):
  reste = reste sans préposition initiale
  si reste est exactement une direction -> Succès(Go, direction)
  candidats = [verbe, si c'est un mot de sortie de la pièce] + mots de reste (prépositions ignorées)
  si candidats non vide et tous sont des mots de sortie de la pièce:
      si ils désignent une seule direction -> Succès(Go, direction)
  si reste vide:
      si verbe in {va, vas, aller, allez, avance, avancer} -> Erreur(MissingObject)
  -> Succès(Go, sans direction)    // « va porte », « sors » sans sortie correspondante : refusé par le moteur

Résoudre(groupe, scope):
  texte = groupe joint par des espaces
  candidats = objets de scope dont un alias normalisé est EXACTEMENT égal à texte
  si aucun:
      candidats = objets de scope dont un alias CONTIENT tous les mots du groupe
  si 0 candidat  -> Erreur(UnknownObject)
  si 1 candidat  -> cet objet
  si > 1         -> Erreur(Ambiguous, noms des candidats)
```

### 6.8 Filtre de grossièretés (PoC V3)

`ProfanityFilter.Contains(normalizedWords)` est appelé **avant** l'analyseur dans `GameEngine.Execute`. Il vérifie :
- **Mots isolés** (83 entrées) : chaque mot normalisé de la saisie est testé contre un `HashSet`.
- **Expressions** (14 suites contiguës de mots) : recherche de fenêtre glissante sur la liste des mots normalisés.

Si détecté : `Execute` renvoie immédiatement une réponse tirée de `FlavourText.ProfanityReplies` (via `ReplyPicker`), sans analyser la commande, sans changer l'état (`StateChanged = false`). La liste contient volontairement `lechecul` (forme normalisée de « lèche-cul » avec trait d'union supprimé par `TextNormalizer`).

Mots **absents volontairement** (sens légitime dans le jeu ou le monde, ne pas les ajouter) : `cul` seul (« cul-de-sac »), `bander`, `fumier`, `ordure`, `bouse`, `bâtard`, `pouffe`, `chatte`, `gueule` seul, `pine`. Les insultes discriminatoires sont hors périmètre.

### 6.7 Erreurs et messages

| Erreur | Message (neutre) |
|---|---|
| Empty | (rien : la saisie vide est ignorée) |
| UnknownVerb | « Cette commande n’est pas comprise. » |
| MissingObject | « Précisez l’objet de la commande. » |
| UnknownObject | « Vous ne voyez pas cela ici. » |
| Ambiguous | « Précisez : » suivi des noms des candidats joints par « ou » (« le lecteur de l’escalier ou le lecteur du local technique »). |

La correction automatique de fautes de frappe n'est pas prévue. Les formes courantes sont couvertes par la table de verbes et les alias.

### 6.7 Erreurs et messages

| Erreur | Message |
|---|---|
| Empty | (rien : la saisie vide est ignorée) |
| UnknownVerb | Réponse tirée au hasard de `FlavourText.UnknownNeutral` (75 %) ou `FlavourText.UnknownHumour` (25 %), **jamais deux fois le même message consécutif** pour une même liste (`ReplyPicker`). |
| MissingObject | « Précisez l’objet de la commande. » |
| UnknownObject | « Vous ne voyez pas cela ici. » |
| Ambiguous | « Précisez : » suivi des noms des candidats joints par « ou » (« le lecteur de l’escalier ou le lecteur du local technique »). |

La correction automatique de fautes de frappe n'est pas prévue. Les formes courantes sont couvertes par la table de verbes et les alias.

## 7. Exécution des commandes

### 7.1 Répartiteur

```text
Execute(input):
  mots = Normaliser(input)
  si mots non vide et ProfanityFilter.Contains(mots):
      return ProfanityReplies (via ReplyPicker), StateChanged = false

  r = Parse(input, Scope(), ExitWords())
  si erreur == Empty    -> aucun message (saisie vide ignorée)
  si autre erreur       -> message correspondant (§6.7)

  c = commande
  si c.Action == Go:
      si c.Direction est nulle -> « Vous ne pouvez pas aller par là. »   (Messages.CannotGo)
      sinon return Move(c.Direction)

  si c.Instrument != null et !état.IsInInventory(c.Instrument):
      return « Vous ne l’avez pas sur vous. »

  // 1. comportement propre à la pièce
  res = pièceCourante.Handle(c, état)
  si res != null -> return res

  // 2. comportements par défaut
  return DefaultBehaviour(c)
```

### 7.2 Déplacement

```text
Move(dir):
  sortie = pièceCourante.Exits.Find(dir)
  si absente                      -> « Vous ne pouvez pas aller par là. »
  si To est null ou IsOpen faux   -> sortie.BlockedMessage, ou « Vous ne pouvez pas aller par là. » à défaut
  sinon                           -> état.CurrentRoom = sortie.To
                                     return Changed(Description de la nouvelle pièce + OnEnter(état))
```

Une tentative vers une sortie `Blocked` n’est pas exécutée et renvoie le message explicatif. Un déplacement réussi signale `StateChanged`.

### 7.3 Comportements par défaut

Messages exacts, tels qu’implémentés dans `Core/Messages.cs` :

| Action | Comportement |
|---|---|
| Look sans cible | Réaffiche la description de la pièce. |
| Look avec cible | Affiche `Describe(état)` de l’objet. |
| Read sans cible | « Il n’y a rien à lire ici. » (`Messages.NothingToRead`). |
| Read avec cible | Sur `Display` et `BedroomMachines`, même texte que `Look` (`Describe`). Sur tout autre objet : « Il n’y a rien à lire ici. » |
| Take | Si l’objet est portable et présent dans la pièce courante : il passe dans l’inventaire (« Pris. »). Sinon : « Vous ne pouvez pas prendre cela. » |
| Drop | Si l’objet est dans l’inventaire : il passe dans la pièce courante (« Posé. »). Sinon : « Vous ne portez pas cela. » |
| Open | Si l’état courant est exactement `Closed`, passe à `Open` (« Vous ouvrez <nom>. ») ; sinon « Rien ne se passe. » |
| Close | Si l’état courant est exactement `Open`, passe à `Closed` (« Vous fermez <nom>. ») ; sinon « Rien ne se passe. » |
| Use | Sans instrument : « Précisez sur quoi l’utiliser. » (`Messages.UseWithoutInstrument`) — pour toute cible sans instrument non gérée par la pièce. Avec instrument non géré : « Rien ne se passe. » |
| Talk | « Rien ne se passe. » |
| Inventory | Liste les noms des objets de l’inventaire, ou « Vous ne portez rien. » Un complément éventuel est ignoré. |
| Go sans direction | « Vous ne pouvez pas aller par là. » (`Messages.CannotGo`) — aucune exception n’est levée (corrigé au PoC V2, A5). |

Points de détail à connaître :

- Les formes « Vous ouvrez … » / « Vous fermez … » sont **fixes** : elles ne reprennent pas le verbe tapé par le joueur, et ne changent d’état que si l’objet est exactement dans l’état opposé. `Take`, `Drop`, `Open`, `Close` et le déplacement signalent `StateChanged`.
- Le vérificateur d’instrument (§7.1) s’applique dès que la commande porte un `Instrument`, quelle que soit l’action : un `Take` rédigé « prends caisse avec levier » exige donc le levier dans l’inventaire.
- Le traitement de `Read` sur `Display` et `BedroomMachines` est codé dans `GameEngine.DefaultBehaviour` avec les identifiants de ces objets (couplage du moteur au contenu, §15), et non dans les `Handle` des pièces.
- Les réponses à « ouvre » sur les portes et l’ascenseur (porte vitrée, porte de l’escalier, porte du local technique, ascenseur) sont dans les `Handle` des pièces : elles dépendent de l’état des lecteurs et de la borne.

### 7.4 Entrée dans une pièce

`Start()` et chaque déplacement renvoient la description de la pièce, puis les messages de `OnEnter`. Les événements uniques (annonce, scène) utilisent `GameState.Events` pour ne se produire qu’une fois. Au chargement d’une sauvegarde, `Start()` réaffiche la description de la pièce sans rejouer les événements déjà notés. `OnEnter` modifie `Events` pendant `Start()` sans signaler `StateChanged` : cette modification est sauvegardée à la prochaine commande qui change l’état, ou à la fermeture.

## 8. Sauvegarde

- **Déclenchement** : **(1)** à la fermeture de la fenêtre, et seulement si une partie est en cours (nouvelle ou chargée), dans un bloc `finally` pour couvrir aussi une sortie sur exception (l’exception n’est pas avalée) ; **(2)** après toute commande dont le résultat signale `StateChanged` (donc aussi tout déplacement), via `GameApp.Submit`, uniquement si une partie est en cours. Une seule sauvegarde. Le chemin « quitter » → écriture du fichier a été vérifié dans le jeu réel pendant les PoC V1 et V2.
- **Contenu** : pièce courante, emplacements des objets, états des objets, événements. **Aucune définition** (textes, images, sorties) n’est sauvegardée, ni l’historique de la console.
- **Version de schéma** : `SchemaVersion`, valeur courante **1**. Si la version est inconnue ou le fichier illisible, la sauvegarde est ignorée proprement (« Continuer » grisé).
- **Format avant chiffrement** (enums écrits par nom, `System.Text.Json` avec `JsonStringEnumConverter`, JSON compact sans indentation) :

```json
{
  "schemaVersion": 1,
  "currentRoom": "TechCorridor",
  "locations": { "Badge": { "kind": "Inventory" }, "BedroomWardrobe": { "kind": "Room", "room": "Bedroom" } },
  "states": { "BedroomReader": "Unlocked", "StairsReader": "Unlocked", "Terminal": "Activated" },
  "events": [ "WakeAnnouncementDone" ]
}
```

- **Chiffrement** : AES-256-GCM (`System.Security.Cryptography.AesGcm`), nonce aléatoire de 12 octets et tag de 16 octets. Disposition du fichier : `[nonce][tag][contenu chiffré]`. La clé est le SHA-256 d’un secret constant embarqué dans l’application : il s’agit d’une **obfuscation contre l’édition manuelle**, pas d’une protection. Aucune exception ne traverse la couche de sauvegarde : un contenu altéré ou une mauvaise clé rendent `null`.
- **Écriture atomique** : écriture dans `<fichier>.tmp`, puis `File.Move(..., overwrite: true)`. Le dossier cible est créé au besoin.
- **Emplacement** : dossier Documents de l’utilisateur (`Environment.SpecialFolder.MyDocuments`), sous-dossier `Releve`, fichier `save.dat`. Sur la machine de développement, `Documents` est redirigé vers `D:\MesDocuments`. Choix à revoir pour Linux/macOS.
- **Tests** : `EncryptedFileSaveStore` accepte un chemin injecté, ce qui permet aux tests d’utiliser un fichier temporaire.

```csharp
public interface ISaveStore
{
    bool Exists();
    GameState? Load();                 // null si absent, illisible ou version inconnue
    void Save(GameState state);
}
```

## 9. Couche d’affichage (projet Game, raylib)

### 9.1 Principe

Une boucle simple : lire les entrées, mettre à jour, dessiner, sans liaison de données. Les noms précis des fonctions raylib sont à **vérifier dans la cheatsheet et les exemples officiels** (voir Références), pas à deviner.

```text
Initialisation : fenêtre 1280×720 (« Relève »), SetTargetFPS(60), SetExitKey(Null),
                 périphérique audio, polices (deux tailles), panneaux, menu, image de fond du menu (optionnelle)

Tant que la fenêtre n'est pas fermée et que « quitter » n'a pas été saisi:
    si le visualiseur de texte est ouvert:
        vider la file de caractères (GetCharPressed) ; mettre à jour le visualiseur seul
        (ni le menu, ni la console, ni l'inventaire ne reçoivent d'entrée)
    sinon si une partie est en cours: lire la console, exécuter la commande, relire GetView()
    sinon:                            afficher et mettre à jour le menu
    dessin (fond, scène — vue du lieu, flèches, inventaire, console — ou menu ; puis le visualiseur par-dessus s'il est ouvert)
    musique: mise à jour du flux

À la fermeture (bloc finally) : sauvegarde si une partie est en cours, puis libération des textures
(dont le fond du menu), des polices et des flux audio.
```

Détails vérifiés :

- `SetExitKey(KeyboardKey.Null)` : la touche Échap ne ferme plus la fenêtre. La fermeture passe par le bouton de la fenêtre ou par la commande « quitter ». Échap sert à fermer le visualiseur de texte.
- La saisie lit les caractères en Unicode (`GetCharPressed`), pas les touches physiques : les accents sont saisis quelle que soit la disposition du clavier.
- Les touches de validation (Entrée) sont lues avec `IsKeyPressed`, **déclenché sur front** : voir §16 pour la conséquence sur l’automatisation. Retour arrière, Suppr, ←/→ utilisent `IsKeyPressedRepeat` (répétition à touche maintenue).
- Retour arrière : la constante est **`KeyboardKey.Backspace`**. `KeyboardKey.Back` est la touche « retour » d’Android (code 4) et ne se déclenche jamais sur un clavier de bureau (cause du défaut du PoC V1, corrigé au PoC V2).
- « quitter » n’est pas affiché en écho dans la console (§6.3).
- Les textures et polices sont chargées après `InitWindow`.
- `GameApp.Submit` : écho de la commande (type *saisie*), exécution, ajout des réponses (type *réponse*), sauvegarde si `StateChanged`, puis relecture de la vue.

### 9.2 Disposition (1280×720, valeurs de `Ui/Layout.cs`)

| Zone | Constante | Valeur |
|---|---|---|
| Fenêtre | `ScreenWidth` × `ScreenHeight` | 1280 × 720 |
| Zone haute | `TopHeight` | 480 (les deux tiers) |
| Vue du lieu | `ViewWidth` × `ViewHeight` | 960 × 480 (trois quarts de la largeur), marges `Padding` = 10 |
| Colonne latérale | `SidebarX` / `SidebarWidth` | 960 / 320 |
| Déplacements | `ArrowsHeight` | 132 (hauteur réduite au strict nécessaire) |
| Inventaire | `InventoryBounds` | x=960, y=132, 320 × 348 |
| Console | `ConsoleBounds` | x=0, y=480, 1280 × 240 (un tiers, toute la largeur) |

L’image est **étirée pour remplir exactement `ViewBounds`** (940 × 460 utiles ; proportions non conservées : les images de lieux sont en ratio 2:1, d’où une déformation horizontale d’environ 2 %, acceptée). Le cadre (`DrawRectangleLinesEx`) est redessiné après l’image. Si l’image d’un lieu est introuvable, un **rectangle de remplacement** porte le titre de la pièce : le jeu fonctionne sans aucune illustration.

La console calcule à chaque image le nombre de lignes visibles et ancre l’historique en bas, juste au-dessus de la ligne de saisie. Une colonne de boutons de défilement (~28 px) à l’extrémité droite de l’aire de l’historique réduit d’autant la largeur de retour à la ligne.

### 9.3 Composants

- **Vue du lieu** : texture chargée depuis `assets/rooms/<ImageKey>.png`, mise en cache par clé, **étirée pour remplir exactement `ViewBounds`** avec `DrawTexturePro`. Le cadre est redessiné après l’image. Si l’image est introuvable, rectangle de remplacement.
- **Flèches** (`ArrowsPanel`) : disposition en **croix** centrée horizontalement sous le titre « Déplacements possibles : », sur trois rangées de 34 px (`RowHeight`) : Nord en haut, Ouest à gauche (libellé puis flèche) et Est à droite (flèche puis libellé) au milieu, Sud en bas. Les flèches (18 px, `ArrowSize`) sont des **têtes de flèche seules, sans queue** : l’utilisateur a demandé cette simplification après que l’agent de code n’arrivait pas à dessiner correctement fût et pointe ensemble. `Absent` : rien ; `Blocked` : pointe **creuse** (`DrawTriangleLines`), gris sombre (`Palette.ArrowBlocked`), libellé atténué ; `Open` : pointe **pleine** (`DrawTriangle`), couleur vive (`Palette.ArrowOpen`), libellé normal. La distinction ne repose pas que sur la couleur. Ce sont des **indicateurs**, pas des boutons : aucune interaction à la souris. **Règle raylib** : `DrawTriangle` exige des sommets en ordre **anti-horaire à l’écran** ; dans l’autre sens, le triangle n’est pas dessiné (cause du défaut du PoC V1, où seul le fût et la pointe Est apparaissaient). La grille de dessin (constantes de `ArrowsPanel.cs`) est reproduite par la mesure pixelique de `agentTools/` (§16).
- **Inventaire** : liste verticale de libellés précédés d’une icône de remplacement (carré de 16 px), titre « Inventaire : », défilement au clavier (Ctrl+↑/↓).
- **Console** (`ConsolePanel`) : historique avec retour à la ligne automatique (largeur mesurée avec la police, réduite de la colonne des boutons de défilement), ancré en bas, défilement vertical. **Deux types de ligne**, mémorisés par ligne d’historique : *saisie du joueur* (écho « > commande », couleur `Palette.ConsoleInput`) et *réponse du jeu* (couleur `Palette.ConsoleResponse`) ; les lignes issues du retour à la ligne d’un même message gardent son type (`Append(message, isInput)`). Ligne de saisie avec invite `> ` (couleur `Palette.Prompt`) et texte saisi en couleur de saisie, curseur clignotant (2 clignotements par seconde), décalée vers la gauche quand le curseur dépasse le bord droit. Édition : Retour arrière et Suppr avec répétition à touche maintenue (`IsKeyPressedRepeat`), flèches gauche/droite, début/fin. Flèches haut/bas : rappel de l’historique des commandes saisies. **Deux boutons cliquables** (haut, bas), dessinés comme formes géométriques dans une colonne de 28 px à l’extrémité droite de l’aire de l’historique : un clic fait défiler de **3 lignes** ; **pas de répétition à clic maintenu** (supprimée à la demande de l’utilisateur : défilement trop rapide) ; un bouton est grisé quand le défilement est impossible dans son sens. Page↑ / Page↓ restent actifs. Tout nouveau message ramène l’historique en bas. Les boutons ne réagissent pas quand le visualiseur de texte est ouvert.
- **Police** : `Ui/Fonts.cs`, taille 20 (corps, interligne 26) et seconde taille (~64) pour le titre du menu (`MeasureTitleWidth`, `DrawTitle`), chargée depuis `assets/fonts/DejaVuSans.ttf` avec une liste explicite de codepoints. Repli sur la police par défaut de raylib si le fichier est absent ou invalide. **Le jeu de codepoints ne contient pas ▲ ▼ ◄ ►** : tout symbole d’interface est dessiné en géométrie.
- **Assets** : `Ui/Assets.cs` résout le dossier `assets` à côté de l’exe, sinon en remontant depuis le répertoire courant (lancement depuis les sources). Fournit aussi les chemins `MenuBackground` et `HelpText`.
- **Palette** : `Ui/Palette.cs`, fond bleu-gris sombre, panneaux, bordures, texte, texte atténué, invite, rectangle de remplacement, couleurs des flèches (`ArrowOpen`, `ArrowBlocked`) et des lignes de console (`ConsoleInput`, `ConsoleResponse`). Valeurs validées à l’écran par l’utilisateur.

### 9.4 Touches de défilement

- Page précédente / Page suivante : défilement de l’historique de la console (et du visualiseur de texte, §9.8).
- Ctrl + Haut / Ctrl + Bas : défilement de l’inventaire.

Le Ctrl est testé avec `IsKeyDown`, les flèches avec `IsKeyPressed`. Un conflit possible avec le rappel de l’historique de la console (la flèche Haut y rappelle l’historique) est signalé au §15.

### 9.5 Police et accents (point critique)

Le jeu est en français : ne pas se reposer sur la police par défaut de raylib. Une police TTF libre de droits est chargée depuis `assets/fonts` avec la liste explicite des codepoints :

- ASCII imprimable (32 à 126) ;
- Latin-1 Supplement et Latin Extended-A (0x00A0 à 0x017F) : accents français, `œ Œ`, guillemets `«` `»` ;
- ponctuation générale : 0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2026.

Police retenue : **DejaVu Sans 2.37**, licence incluse dans `assets/fonts/DejaVuSans-LICENSE.txt`. **Validé à l’écran** : é è ê ë à â ç ù û ô î ï œ « » ’ … s’affichent et se saisissent (326 glyphes chargés, aucun manquant). La saisie lit les caractères en Unicode, pas les touches physiques (§9.1). Un caractère hors de cette liste (par exemple dans `help.txt`) s’afficherait comme un « ? ».

### 9.6 Audio

Deux bandes son, conformément à la note d'intention §10 (la V0.1 n'en prévoyait qu'une : la contradiction est tranchée en faveur de la note) :

| Piste | Fichiers cherchés | Moment de lecture |
|---|---|---|
| `menu` | `assets/audio/menu.ogg` puis `menu.mp3` | à l'écran du menu (et pendant l'aide) |
| `game` | `assets/audio/game.ogg` puis `game.mp3` | pendant la partie |

Chaque flux est chargé si présent, mis en boucle (`Looping = true`) et mis à jour à chaque image (`UpdateMusicStream`). Changer de piste arrête la précédente. **La bascule vers `game.ogg` se fait dès la sortie du menu** (choix « Continuer » ou « Nouvelle partie ») via `MusicPlayer.Play(MusicPlayer.GameTrack)` dans `GameApp.StartNewGame` et `ContinueGame` : « Aide » ne change pas la musique. Si le périphérique audio est indisponible (`InitAudioDevice` échoue) ou si les fichiers sont absents, le jeu continue **sans son**, sans erreur bloquante. Les flux sont arrêtés et déchargés à la fermeture.

### 9.7 Menu

Au lancement : **trois entrées**, dans l’ordre « Continuer » (grisé si aucune sauvegarde valide), « Nouvelle partie », « Aide » (toujours accessible). Navigation cyclique au clavier (haut/bas, Entrée). La sélection est placée par défaut sur « Continuer » quand une sauvegarde existe, sinon sur « Nouvelle partie ». Valider une entrée grisée ne fait rien. `MenuScreen` renvoie un choix typé (`Continue`, `NewGame`, `Help`).

- **Fond d'écran** : `assets/menu/menuBackground.png` étiré pour remplir toute la fenêtre (proportions non conservées), dessiné avant le titre et les entrées. Fichier absent : fond uni (`Palette.Background`), sans erreur.
- **Titre** : « Relève » en grand (seconde taille de police, `Fonts.Title`), centré en haut, avec ombre portée pour la lisibilité.
- **« Aide »** ouvre le **visualiseur de texte** (§9.8) en plein écran, titre « Aide », avec le contenu de `assets/texts/help.txt` (UTF-8, lu par `GameApp.OpenHelp`). Fichier absent ou illisible : le visualiseur affiche « Texte d'aide introuvable. ».

Implémentation : l'existence et la validité de la sauvegarde sont évaluées **une seule fois au démarrage** (`_canContinue` dans `GameApp`), et `MenuScreen.Reset` n'est appelé qu'à l'entrée dans l'état menu (drapeau `_menuEntered`, remis à faux au choix d’une entrée). `GameApp.UpdateMenu` n'appelle plus `StartNewGame` par défaut : « Aide » n'entre pas en jeu, ne change pas la musique et ne modifie pas `_playing`. À la fermeture du visualiseur **au clavier** (`Update()` renvoie vrai), `_menuEntered` repasse à vrai et la sélection reste sur « Aide » ; à la fermeture **à la souris**, ce n’est pas le cas (défaut connu, §15).

### 9.8 Visualiseur de texte (`Ui/TextViewer.cs`)

Composant réutilisable de la couche Game, **superposé** à l’écran courant : aide aujourd’hui, lettre des parents, crédits ou autre texte plus tard. Le déclenchement depuis le moteur (par exemple « lis lettre ») n’existe pas : il demanderait un signal du moteur vers l’interface, hors périmètre à ce jour.

- **API** : `Open(rectangle, titre, texte)`, `IsOpen`, `Update()` (renvoie vrai à la fermeture au clavier), `Draw()`. Le rectangle est un paramètre : plein écran pour l’aide ; une fenêtre partielle est prévue pour un usage en jeu (non déclenchable à ce jour, vérifiée uniquement par construction).
- **Opaque et modal** : il remplit son rectangle d’une couleur pleine et masque ce qu’il recouvre. Tant qu’il est ouvert, `GameApp` n’appelle ni la mise à jour du menu, ni celle du jeu (donc ni la console ni l’inventaire) ; la scène sous-jacente est dessinée puis recouverte ; la file de caractères est vidée à chaque image pour qu’aucune saisie ne fuie dans la console à la fermeture. La musique continue.
- **Texte** : retours CRLF normalisés en LF ; retour à la ligne automatique à la largeur du rectangle (mesurée avec `Fonts.MeasureWidth`) ; lignes vides conservées.
- **Défilement** : Page↑ / Page↓ (une page moins une ligne), ↑ / ↓ (une ligne), Début / Fin. Indicateurs « texte au-dessus / en dessous » dessinés en triangles (sommets anti-horaires, §9.3).
- **Fermeture** : bouton « Fermer » cliquable (appui et relâchement sur le bouton, survol visible) et équivalent clavier Échap ou Entrée. Le composant n’a pas de garde explicite pour la frame de création : l’Entrée qui ouvre l’aide depuis le menu ne la ferme pas parce que `IsKeyPressed` ne se déclenche que sur le front et que `GameApp` n’appelle `Update()` qu’à partir de l’image suivante.
- Limites connues : voir §15.

### 9.9 Règle d’interaction

Voir le §2. En résumé : clavier partout ; souris pour les boutons ▲/▼ de la console et le bouton « Fermer » du visualiseur ; curseur visible ; flèches de déplacement non cliquables.

## 10. Étendre le jeu

Pour ajouter un lieu ou un objet :

1. Ajouter les membres nécessaires aux enums `RoomId` / `ObjectId` / `GameEvent`.
2. Créer la classe de la pièce dans `Content/Rooms/` (dérivée de `RoomBase`) : titre, description, sorties (avec `Words` si des mots de sortie s’imposent), `OnEnter` éventuel, `Handle` pour les interactions propres au lieu. Textes en constantes en tête de classe.
3. Déclarer les objets (`ObjectDef`) dans `Content/Objects/`, avec alias, emplacement initial, état initial, description. Convention suivie par le contenu livré : un fichier d’objets par pièce (`BedroomObjects`, `TechCorridorObjects`, …), agrégé par `GameWorld`.
4. Enregistrer la pièce et les objets dans `World`. Si c’est la pièce de départ, positionner `StartRoom`.
5. Ajouter ou mettre à jour les tests de cohérence (§11) et, pour tout comportement propre à une pièce, un test de scénario.

Contraintes à respecter, vérifiées par les tests ou par les conventions :

- Les interactions qui concernent deux pièces ne partagent pas d’état « porte » : la condition d’une sortie lit l’état d’un objet de la pièce de départ.
- Les alias ne doivent pas entrer en collision involontaire entre objets présents dans la même pièce (§6.6).
- Les mots de sortie sont écrits normalisés, non vides, uniques dans la pièce, sans direction cardinale (§5.5). Un mot de sortie n’est jamais masqué par un alias d’objet pour un verbe de déplacement.
- Tout `Handle` qui modifie l’état doit renvoyer `Changed` (§5.6), sinon la sauvegarde automatique ne se déclenche pas.
- `ImageKey` doit correspondre à un fichier `assets/rooms/<clé>.png`, sinon le rectangle de remplacement s’affiche : comportement attendu, silencieux, ce n’est pas une erreur. La casse doit correspondre exactement (portabilité Linux).
- Tout texte affiché respecte la convention d’encodage du §2 (caractères précomposés, apostrophe typographique).

## 11. Stratégie de tests (projet `Releve.Engine.Tests`)

Les tests remplacent la validation de schéma qu’offrirait un fichier de données. **103 tests**, tous verts (83 à l’issue du PoC V1, 20 ajoutés au PoC V2).

| Fichier | Ce qu’il couvre |
|---|---|
| `WorldConsistencyTests.cs` | Cohérence du monde livré et du monde de test : chaque pièce a une classe, chaque objet une définition, chaque destination existe, chaque sortie a son retour sauf `OneWay`, sortie sans destination toujours bloquée avec message obligatoire, pas de collision d’alias dans une même pièce, mots de sortie non vides, uniques par pièce et sans direction cardinale. Vérifie aussi les compteurs du monde livré (4 pièces, 22 objets) et son départ (`Bedroom`). |
| `NormalizationTests.cs` | Accents, articles, apostrophes, ponctuation, prépositions canoniques, alias normalisés comme la saisie. |
| `ParserTests.cs` | Variantes de verbes (dont les verbes ajoutés au PoC V2), directions seules et précédées de « va », Look/Inventory/Read sans complément, ambiguïté (liste des candidats), objet invisible inconnu, verbe inconnu. |
| `ExitWordsTests.cs` | Mots de sortie : priorité sur les alias d’objets pour les verbes de déplacement, alias d’objets seuls pour les autres verbes (« regarde escalier » = `Look`), « sors » sans sortie correspondante, une couverture par ligne du tableau des mots de sortie (7 cas). |
| `EngineBehaviourTests.cs` | Comportements par défaut du moteur (§7.3), dont `Read`, `Use` sans instrument et `Go` sans direction sans exception (« va porte », « aller lit »). |
| `PocContentTests.cs` | Contenu du PoC : sorties sans destination toujours bloquées, retours existants, ambiguïtés du couloir (« lecteur », « porte »), armoire fermée puis badge pris, fiche patient affichée à l’identique, scénario de référence complet du réveil à la rue provisoire, corbeille selon l’emplacement du sac, réponses à « ouvre », sauvegarde puis rechargement (pièce, inventaire, états, événements ; annonce de réveil non rejouée). |
| `SaveTests.cs` | Aller-retour chiffré à l’identique, clés et noms d’enums du JSON, fichier corrompu ignoré, version de schéma inconnue ignorée. |
| `TestWorld/TestWorld.cs` | Monde de test indépendant du contenu réel : pièces, objets dont un objet invisible (`VisibleWhen`), sortie à sens unique, sortie sans destination. |

Les tests génériques (analyseur, états de sortie, sauvegarde, comportements) s’appuient sur `TestWorld`, pas sur le monde livré. Les tests de contenu (`PocContentTests`, `WorldConsistencyTests`) s’appuient au contraire sur `GameWorld.Create()` : c’est le seul endroit qui valide le monde réel de bout en bout. Ajouter ou retirer un membre de `RoomId` / `ObjectId` / `GameEvent` impose de mettre à jour les listes de test de `WorldConsistencyTests` et le monde de test : les tests échouent bruyamment plutôt que de valider des données.

**Hors tests automatiques** : la couche Game (console, flèches, menu, visualiseur, sauvegarde automatique) n’est couverte par aucun test unitaire. Elle est vérifiée dans le jeu réel : par `agentTools/` quand l’agent sait l’utiliser (PoC V1), sinon par des validations humaines (PoC V2, où l’agent de code n’avait pas de sondes ni de captures ; Retour arrière, ordre des sommets des flèches et copie des assets ont été testés par l’utilisateur).

## 12. Initialisation du projet

### 12.1 Livrables

La structure du §4.1 et tout ce qui est décrit dans ce document sont implémentés : modèle de domaine, analyseur, répartiteur, sauvegarde chiffrée, couche d’affichage raylib complète (disposition, vue du lieu, flèches, inventaire, console, police, audio, menu à trois entrées, visualiseur de texte), tests. Le contenu du PoC est livré (§12.2) et validé dans le jeu réel.

### 12.2 Monde livré

Le monde d’amorçage technique des premières versions (`SandboxA`, `SandboxB`, `SandboxObjects`, `BootstrapWorld`) a été **supprimé** à l’arrivée du contenu réel. Il est remplacé par le monde du PoC, assemblé dans `Content/GameWorld.cs` :

- `Bedroom` (pièce de départ), `TechCorridor`, `HospitalLobby`, `Street`, reliées Est / Ouest / Nord / Sud. Deux sorties sans destination (Est du couloir, Nord de l’accueil) servent de fausses pistes. Les sorties praticables portent des mots de sortie (§6.5).
- 22 objets, dont deux portables (badge, sac poubelle), deux objets à visibilité conditionnée (vêtements d’enfant et badge, visibles seulement armoire ouverte) et quatre lecteurs verrouillés au départ.
- La rue est **provisoire** : aucune valeur de lore, aucune image.

Le détail du contenu (noms, alias, textes, comportements) appartient aux documents d’implémentation des PoC, pas à ce document.

### 12.3 Jalons

| Jalon | Contenu | État |
|---|---|---|
| 1 | Projet Engine et tests, avec le monde d’amorçage (§5 à §8, §10, §11), sans interface. | Livré |
| 2 | Fenêtre raylib, disposition, console avec saisie et accents, vue de remplacement. | Livré |
| 3 | Flèches, inventaire, sauvegarde, menu minimal, musique. | Livré |
| 4 | Contenu du PoC V1 : enums, pièces, objets, tests de contenu, suppression du monde d’amorçage, vérification dans le jeu réel. | Livré |
| 5 | PoC V2 : vocabulaire et mots de sortie, `Read`, corrections moteur, console typée et boutons, flèches en croix, image étirée, menu à trois entrées et fond, visualiseur de texte et aide, sauvegarde automatique. | Livré |

### 12.4 Critères d’acceptation

Vérifiés sur le build livré :

- Build et tests au vert à chaque jalon (103 tests).
- Les accents s’affichent et se saisissent correctement dans la console.
- Les trois états de flèche sont visibles et se mettent à jour après chaque action.
- L’inventaire reflète les objets pris, avec défilement au clavier ; la console défile (clavier et boutons) et conserve l’historique.
- Quitter puis « Continuer » restaure la partie à l’identique ; un arrêt brutal après un changement d’état la restaure aussi.
- Le jeu fonctionne sans fichier audio ni images.
- L’exe publié en autonome démarre sur une machine sans SDK .NET, avec la bibliothèque native raylib à côté de l’exe.

Critères propres aux PoC (détail dans les comptes rendus de `Docs/POC_V1/` et `Docs/POC_V2/`) :

- Le scénario de référence se déroule de bout en bout, commande par commande ; « va porte » ne plante plus.
- Les trois états de flèche sont corrects dans les quatre lieux.
- Les textes accentués s’affichent correctement, fiche patient et guillemets compris.
- Le monde d’amorçage a entièrement disparu du code, des objets, des tests et des enums de contenu.

## 13. Consignes de réalisation pour l’agent

1. Travailler par jalons, en gardant build et tests au vert.
2. Ne pas ajouter de fonctionnalité, de projet ni de dépendance non mentionnés. Aucune commande git d’écriture (`AGENTS.md`).
3. Vérifier chaque appel raylib dans la cheatsheet et les exemples officiels plutôt que de s’appuyer sur la mémoire. Contraintes du binding 8.1.0 relevées : `IsFontValid` / `IsTextureValid` (il n’existe ni `IsFontReady` ni `IsTextureReady`), `char.ConvertFromUtf32` (attention à la casse : « tf32 », pas « TF32 »), `music.Looping` qui est un champ de structure et non une méthode, **`KeyboardKey.Backspace`** et non `KeyboardKey.Back`, et **`DrawTriangle` en ordre anti-horaire** à l’écran (sinon rien n’est dessiné ; `DrawTriangleLines` n’y est pas sensible).
4. Signaler explicitement tout écart ou ambiguïté plutôt que choisir silencieusement.
5. À la fin, fournir un court récapitulatif : ce qui fonctionne, ce qui reste approximatif, les points à valider, en distinguant ce qui est vérifié dans le jeu réel, ce qui ne l’est que par les tests et ce qui attend une validation humaine.

## 14. Points à valider

1. **Résolution cible** : 1280×720 retenue et implémentée (`Layout.cs`). Images de lieux : trois sur quatre fournies (`Bedroom`, `HospitalLobby`, `TechCorridor`) ; la rue provisoire est sans image (rectangle de remplacement, comportement prévu §9.2). Format définitif à préciser avec les lieux suivants (ratio 2:1 conseillé : voir la déformation au §9.2).
2. **Police TTF** : DejaVu Sans 2.37 retenue, licence incluse.
3. **Touches de défilement** : Page↑/Page↓ pour la console, Ctrl+↑/↓ pour l’inventaire (conflit possible, §15).
4. **Dossier de sauvegarde** : `Documents/Releve/save.dat` retenu. Fréquence : à la fermeture et après chaque commande qui change l’état. Le dossier de données de l’application reste plus conventionnel et est le point à revoir en priorité, notamment pour Linux/macOS.
5. **Messages neutres d’erreur** : implémentés tels quels (§6.7 et §7.3).
6. **Bibliothèque native raylib** : vérifiée présente à côté de l’exe après `dotnet publish`.
7. **Page d’aide pleine page** : implémentée au PoC V2 (§9.8) avec un texte de remplissage (lorem ipsum) : le vrai texte d’aide reste à rédiger.
8. **Migration .NET** : non planifiée (décision de l’utilisateur).

## 15. Écarts connus et points ouverts

### 15.1 Défauts recensés puis corrigés

1. **Navigation du menu neutralisée.** `MenuScreen.Reset` n’est plus appelé qu’à l’entrée dans l’état menu (drapeau `_menuEntered`).
2. **Fichier de sauvegarde relu en permanence.** Le résultat de `CanContinue()` est mémorisé au démarrage (`_canContinue`).
3. **Images de lieux non référencées.** Les trois images de `assets/rooms/` sont des images vivantes du monde (clés `Bedroom`, `HospitalLobby`, `TechCorridor`) ; seule la rue est sans image, par conception.
4. **Touche Retour arrière** : cause = `KeyboardKey.Back` (touche Android) au lieu de `KeyboardKey.Backspace` ; corrigé, avec répétition à touche maintenue pour Retour arrière et Suppr. Testé par l’utilisateur au clavier réel ; la répétition n’est pas validable par `agentTools/` (§16).
5. **Flèches** : cause = ordre des sommets horaire de `DrawTriangle` pour Nord, Sud et Ouest (triangle non dessiné, il ne restait que les fûts). Corrigé ; de plus, l’utilisateur a demandé de ne garder que les têtes (sans fût), que l’agent n’arrivait pas à dessiner proprement. Testé par l’utilisateur.
6. **Plantage « va <objet> »** : `GameEngine.Execute` appelait `Move(command.Direction!.Value)` pour toute commande `Go` ; une commande `Go` sans direction (« va porte ») levait une exception non interceptée. Corrigé au PoC V2 (A5) : message « Vous ne pouvez pas aller par là. », avec tests.
7. **Sauvegarde** : la sauvegarde ne se faisait qu’à la sortie normale. Désormais après chaque commande à `StateChanged` et dans un `finally` à la fermeture.
8. **Bascule musique menu/jeu** : régression corrigée — `game.ogg` est joué en quittant le menu.
9. **Image du lieu étirée** à toute la zone utile (B4) ; **page d’aide** (C4) ; **visualiseur** : CRLF normalisé avant le retour à la ligne, indicateur du bas repositionné au-dessus du bouton « Fermer ».
10. **Écart d’alias « prends badge »** : accepté, diagnostic corrigé (§6.6). L’explication précédente (« un objet invisible reste partiellement identifiable ») était inexacte.

### 15.2 Décisions de l’utilisateur

- **Boutons de défilement de la console** : un clic = 3 lignes, pas de répétition à clic maintenu (défilement trop rapide au test) ; boutons grisés quand le défilement est impossible.
- **Flèches** : têtes seules, sans fût (§9.3).
- **Image du lieu** : étirée, déformation d’environ 2 % acceptée.
- **Sauvegarde** : après chaque commande qui change l’état.

### 15.3 Écarts connus à l’issue du PoC V2 (relevés à la lecture du code, non corrigés)

À traiter dans une version ultérieure ; ils ne bloquent pas le jeu.

**Interface**
1. **Fermeture de l’aide à la souris** : `TextViewer.Update()` renvoie faux à la fermeture par clic ; `GameApp` ne remet donc pas `_menuEntered` à vrai et `MenuScreen.Reset` s’exécute : la sélection revient sur « Continuer » ou « Nouvelle partie » au lieu de rester sur « Aide ». Au clavier, la sélection est conservée.
2. **Titre du panneau des flèches** : la constante contient « Deplacements possibles : » (sans accent) ; régression probable de la réécriture du panneau (l’accent était affiché au PoC V1).
3. **Conflit possible Ctrl+↑/↓** : `ConsolePanel.RecallHistory` ne teste pas Ctrl ; Ctrl+↑/↓ (défilement de l’inventaire) rappelle aussi l’historique des commandes (à confirmer en jeu). L’architecture V0.4 affirmait qu’il n’y avait aucun conflit.
4. **Console et visualiseur** : le défilement peut remonter jusqu’à ne laisser qu’une seule ligne visible (le maximum est la dernière ligne, pas la dernière page) ; les mots plus larges que la zone ne sont pas coupés (la condition de retour à la ligne laisse dépasser un mot isolé) ; dans le visualiseur, ↑/↓ n’ont pas de répétition à touche maintenue et le texte n’est pas réduit de la largeur des indicateurs.
5. **Invite de la ligne de saisie** : le « > » garde `Palette.Prompt` au lieu de la couleur de saisie.
6. **Flèches** : pour Nord et Sud, seule la flèche est centrée et le libellé est décalé à droite ; paramètre `centred` toujours vrai (code mort) ; champ `_scrollHoldTimer` inutilisé dans `ConsolePanel`.
7. **Icônes d’inventaire** : toujours des carrés de remplacement, en attente des assets définitifs.

**Moteur et contenu**
8. **Apostrophes droites** dans les textes ajoutés au PoC V2 : trois constantes « ouvre » dans `HospitalLobby`, trois dans `TechCorridor`, `DescribeBin`, et le message d’aide introuvable de `GameApp`. La convention du §2 impose `’` ; le contrôle `[\u0300-\u036f]` ne le détecte pas. Ces textes dupliquent aussi presque à l’identique les messages de sortie bloquée (typographie différente).
9. **Lecteurs** : repasser le badge sur le lecteur de l’escalier déjà déverrouillé répond « Rien ne se passe. », alors que celui de la chambre répond « Le lecteur est déjà déverrouillé. ».
10. **Couplage moteur / contenu** : `Read` sur `Display` et `BedroomMachines` est codé en dur dans `GameEngine.DefaultBehaviour` (identifiants du contenu), au lieu des `Handle` des pièces.
11. **« Précisez sur quoi l’utiliser. »** s’applique à toute cible sans instrument non gérée (« utilise lit »), pas seulement à un objet de l’inventaire.
12. **Analyseur** : toute commande dont le premier mot est une direction ou une abréviation est un déplacement, élisions comprises (« n’ouvre pas la porte » → « n » → Nord) ; « vas », « allez », « avance » et « avancer » seuls répondent « Vous ne pouvez pas aller par là. » alors que « va » et « aller » seuls répondent « Précisez l’objet de la commande. ».
13. **Caractères corrompus** dans un commentaire de `GameEngine.cs` (« Â§6.6 » pour « §6.6 ») : signalés, non corrigés.

## 16. Outillage d’automatisation (hors build)

Le dossier `agentTools/` contient l’automatisation utilisée pour vérifier le jeu réel. Il ne fait **pas partie du build** : aucun `.cs` n’y appartient, `dotnet build` et `dotnet test` ne le voient pas.

| Fichier | Rôle |
|---|---|
| `ReleveGameTools.psm1` | Module PowerShell : focus clavier (`Set-ReleveGameFocus` / `Wait-ReleveGameFocus`), envoi de commandes (`Send-ReleveGameCommands`), frappes Entrée maintenues (`Send-ReleveGameEnter`), vidage de la ligne de saisie (`Clear-ReleveGameInput`), captures (`Save-ReleveGameWindowShot`), mesure des états de flèche (`Get-ReleveArrowStates`). |
| `Run-Hidden.ps1` | Exécute un script dans un processus sans fenêtre. |
| `Send-Commands.ps1` | Envoie un fichier de commandes, avec validation d’entrées de menu. |
| `scenarios/` | Scénarios réutilisables (référence du PoC, quitter-et-sauvegarder). |
| `README.md` | Recette et pièges recensés. |

Contraintes vérifiées, à respecter pour toute évolution :

1. **L’injection doit partir d’un processus sans fenêtre** : un terminal PowerShell visible vole le premier plan et le jeu ne reçoit rien.
2. **Les caractères passent par `SendKeys`** : `SendInput` (`wVk` ou `KEYEVENTF_UNICODE`) et `PostMessage(WM_CHAR)` n’atteignent pas la saisie du jeu. Un caractère à la fois : la file de raylib est limitée à 16 codepoints par image (`MAX_CHAR_PRESSED_QUEUE`).
3. **Les touches d’état (Entrée, Retour arrière) doivent être maintenues** avec `keybd_event` (~60-80 ms). `IsKeyPressed` détecte un front : un appui-relâché instantané qui tombe entre deux images 60 Hz est silencieusement perdu, et deux commandes se retrouvent collées sur la même ligne.
4. **Les fichiers de commandes sont lus en UTF-8** : `Get-Content` lit en ANSI par défaut sous PowerShell 5.1, ce qui corrompt les accents.
5. **La capture `PrintWindow` sert de référence** pour les mesures pixeliques (client 1280×720, décalage de +8 en x et +39 en y par la bordure). Les zones de `Get-ReleveArrowStates` sont déduites des constantes de `Ui/ArrowsPanel.cs` et `Ui/Layout.cs` ; elles ont été recalibrées pour la disposition en croix du PoC V2 et doivent être recalibrées si la mise en page du panneau change.
6. **La répétition de Retour arrière et de Suppr** est implémentée dans le moteur d’entrée (`IsKeyPressedRepeat`) mais **ne peut être validée par `agentTools/` comme par un vrai clavier** : validation humaine. Les clics souris (boutons de défilement, « Fermer ») sont dans le même cas.
7. **Disponibilité selon l’agent** : l’agent utilisé au PoC V2 (Nemotron) n’a pas pu utiliser sondes, captures ni injection ; l’outillage reste valable pour un agent qui sait l’utiliser.
8. **À re-vérifier** : la recette de rejeu du compte rendu du PoC V2 n’utilise pas `Run-Hidden.ps1` et appelle `Wait-ReleveGameWindow`, qui n’est pas dans la liste ci-dessus ; ne pas s’y fier avant de l’avoir testée au regard de la contrainte 1.

## Références

- Raylib-cs sur NuGet : <https://www.nuget.org/packages/Raylib-cs/8.1.0>
- Cheatsheet raylib (liste des fonctions) : <https://www.raylib.com/cheatsheet/cheatsheet.html>
- Exemples officiels raylib : <https://github.com/raysan5/raylib/tree/master/examples>
- Politique de support .NET : <https://dotnet.microsoft.com/platform/support/policy/dotnet-core>