# Jeu "Relève" — Architecture technique

> Document de référence — version 0.7
> Valable pour toute la durée du projet. Il décrit la structure du code, pas le contenu du jeu.
> À fournir à l’agent de développement avec la note d’intention (V6) et le mockup bitmap de l’interface. Le document univers/scénario n’est pas nécessaire pour ce qui est purement technique.
> Les documents ne portent plus de numéro de version dans leur nom de fichier : la version figure dans l’en-tête de chacun.

> **État de cette version.** La V0.1 décrivait l’architecture cible, la V0.2 le reflet du code livré à l’issue de l’initialisation, la V0.3 les changements du PoC V1, la V0.4 et la V0.5 ceux du PoC V2, la V0.6 (rédigée par l’agent de code) ceux du PoC V3, de façon incomplète et incohérente. **La V0.7 est une réécriture complète, reflet du code du PoC V3 tel que lu sur la branche `develop`.** Le jeu compte quatre lieux, 22 objets et **128 tests** du moteur, tous verts. Les écarts connus (exigences du plan V3 non réalisées, défauts de touches) sont au §15.3 : ils sont la base du lot de corrections du PoC V4.
>
> **Fiabilité.** Relus sur le code pour cette version : §4.3, §5.6, §5.7, §6, §7, §9.1, §9.3 (vue du lieu, flèches, inventaire, console), §9.4, §9.7 à §9.10. Repris de la V0.6 sans relecture, car non touchés par le POC V3 : §3, §8, §9.2, §9.5, §9.6, §12, §16 et les détails de contenu.

## 1. But et portée

Ce document définit l’architecture technique du jeu d’aventure textuel et graphique « Relève » : plateforme, découpage du code, modèle de domaine, analyseur de commandes, exécution, sauvegarde et couche d’affichage. Il sert de référence pour tout développement ultérieur.

Il ne contient pas le contenu du jeu (lieux, objets, textes). Celui-ci est décrit dans des documents séparés qui s’appuient sur ce document.

**Avancement.** Les POC V1 (contenu : quatre lieux, 22 objets), V2 (vocabulaire et mots de sortie, console colorée, flèches en croix, image étirée, menu à trois entrées, visualiseur de texte, sauvegarde automatique) et V3 (message d’accueil unique, descriptions détaillées, réponses variées aux saisies incomprises, filtre de grossièretés, zones d’image selon l’état, menu et visualiseur à la souris, fondu au lancement, raccourcis fléchés, version affichée) sont livrés. Un dossier d’automatisation hors build (`agentTools/`, §16) existe, mais l’agent de code utilisé ne peut pas s’en servir.

## 2. Principes directeurs

- **Petit développement « à l’ancienne »** : le projet est un seul jeu, pas un moteur multi-jeux.
- **Contenu en classes C#** : lieux, objets, textes, alias, sorties, zones d’image sont écrits dans le code. Aucun fichier JSON/YAML de paramétrage du contenu. (Le JSON n’est utilisé que pour la sauvegarde. Les seuls fichiers lus au runtime sont l’image de fond du menu, les images de lieux et leurs variantes, le texte d’aide et l’audio.)
- **Moteur isolé** : le projet du moteur ne dépend ni de raylib ni d’aucune bibliothèque d’affichage ou de son.
- **Pas de MVVM ni de framework UI** : l’interface est un rendu raylib qui lit l’état du moteur à chaque image.
- **Pas de sur-élaboration** : ne rien ajouter qui ne soit pas décrit dans les documents fournis.
- **En cas de contradiction ou d’ambiguïté** entre documents : signaler le point à l’utilisateur au lieu de trancher seul. La note d’intention fait référence pour l’interface et le comportement du jeu.
- **Règle d’interaction (note d’intention V6, §2).** Tout est faisable au clavier. La souris n’est admise que pour quatre exceptions, chacune doublée d’un équivalent clavier :
  1. les deux boutons de défilement de la console (équivalent : Page↑ / Page↓) ;
  2. le bouton « Fermer » du visualiseur de texte (équivalent : Échap ou Entrée) ;
  3. les entrées du menu : le survol sélectionne, un clic gauche valide (équivalent : flèches Haut / Bas et Entrée) ;
  4. les indicateurs de défilement du visualiseur : un clic défile de 3 lignes (équivalent : Page↑ / Page↓, ↑ / ↓, Début / Fin).

  Le curseur de la souris est visible. Les flèches de la zone de déplacement restent des indicateurs, jamais des boutons.

### Conventions

- Identifiants de code en anglais, textes affichés au joueur en français, commentaires brefs en français.
- Textes joueur : jamais de pensées, souvenirs ni émotions attribués au joueur ; formulations non genrées (pas de participe passé accordé au joueur).
- Dans chaque classe de pièce, les textes sont des constantes regroupées en tête de classe, séparées de la logique. Les coordonnées des zones d’image y sont aussi des constantes nommées.
- **Encodage des textes joueur** : caractères **précomposés** et apostrophe typographique `’` obligatoires. raylib dessine chaque codepoint indépendamment : un accent décomposé (`e` + U+0301) s’affiche en charabia. `EncodingTests` contrôle les textes du moteur (apostrophe droite, accents décomposés, mot « blouse »). Les textes de la couche Game ne sont pas couverts : les relire à la main.
- Une sortie impossible est expliquée par un message qui décrit ce qui bloque, **sans dire comment débloquer** (note d’intention §2).
- **Numérotation** : le **jeu** porte la version v0.n du POC n (`GameInfo.Version`, actuellement `v0.3`). Ce document a sa propre numérotation, indiquée en en-tête.
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

Les dossiers d’assets sont copiés dans le répertoire de sortie (`<None Include="assets\**\*" CopyToOutputDirectory>`). Vérifié après publication : `raylib.dll`, les assets (dont `menu/` et `texts/`) et la police sont bien à côté de `Releve.Game.exe`.

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
                                Command, Location, ObjectDef, Exit, CommandResult, InventoryItem,
                                RoomView, ImagePatch, ImageRect
        GameState.cs           état mutable, seul élément sauvegardé
        Messages.cs            textes neutres du moteur (§6.7, §7.3)
        FlavourText.cs         listes UnknownNeutral, UnknownHumour, ProfanityReplies (§6.7, §6.8)
        ReplyPicker.cs         tirage sans répétition consécutive par liste (§6.7)
      Parsing/
        TextNormalizer.cs      normalisation (§6.2)
        Vocabulary.cs          tables verbes / directions, mots de fermeture, mots de pièce (§6.3)
        CommandParser.cs       analyse, mots de sortie, mots de pièce, résolution des noms (§6.4 à §6.6)
        ParseResult.cs         ParseError, ParseResult
        ProfanityFilter.cs     filtre de grossièretés (§6.8)
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
      GameInfo.cs              version affichée du jeu (constante Version)
      GameApp.cs               boucle principale, états menu / jeu / visualiseur / fondu (§9.1)
      Ui/
        Layout.cs              constantes de disposition (§9.2)
        Palette.cs             couleurs de l'interface
        Fonts.cs               police TTF, deux tailles, codepoints (§9.5)
        Assets.cs              localisation du dossier assets
        RoomViewPanel.cs       vue du lieu, rectangle de remplacement, zones d’image (§9.3)
        ArrowsPanel.cs         croix des déplacements
        InventoryPanel.cs      inventaire à libellés
        ConsolePanel.cs        historique typé, saisie, défilement, boutons souris (§9.3)
        MenuScreen.cs          menu à trois entrées, image de fond, souris, version
        TextViewer.cs          visualiseur de texte modal réutilisable (§9.9)
        ScreenFade.cs          fondu au noir (§9.8)
      Audio/
        MusicPlayer.cs         bandes son en boucle (§9.6)
      assets/                  rooms/ (base + *_AltState.png), fonts/, audio/, menu/, texts/
  tests/
    Releve.Engine.Tests/
      TestWorld/TestWorld.cs   monde de test indépendant du contenu (§11)
      TestSupport.cs           fabrication d'un moteur prêt à jouer
      WorldConsistencyTests.cs, NormalizationTests.cs, ParserTests.cs, ExitWordsTests.cs,
      EngineBehaviourTests.cs, PocContentTests.cs, SaveTests.cs,
      BedroomV3Tests.cs, ReplyTests.cs, ImagePatchTests.cs, EncodingTests.cs
  Docs/                        documents de référence ; POC_V1/, POC_V2/, POC_V3/ : plans et comptes rendus
  agentTools/                  automatisation hors build (§16)
  .kilo/, _ContentSources/     configuration de l'agent et sources de contenu : ne pas modifier
```

> **Collision de noms à connaître** : la classe `World` et l’espace de noms `Releve.Engine.World` portent le même nom. Dans les fichiers qui font `using Releve.Engine.World;`, l’identifiant `World` est ambigu ; un alias global est donc déclaré dans `GlobalUsings.cs` : `WorldModel = Releve.Engine.World.World`. Tout nouveau fichier manipulant le monde utilise `WorldModel` comme type de paramètre.

### 4.2 Règle de dépendance

`Releve.Game` → `Releve.Engine`, jamais l’inverse. Le moteur ne contient ni dessin, ni lecture de touches, ni son. Il reçoit du texte, renvoie du texte et un état à afficher. Le projet de tests ne référence que le moteur : la couche Game n’a pas de test automatique (§11).

### 4.3 Cycle d’une commande

```text
Saisie (clavier) ou raccourci fléché (§9.4)
        │
        ▼
GameApp.Submit(texte)
   ├─ « quitter » ? ─► fermeture, sans écho
   ├─ écho de la commande dans la console (type saisie) ; mémorisation dans l’historique
   ▼
GameEngine.Execute(texte)
   ├─ grossièreté (ProfanityFilter sur le texte normalisé) ─► réponse ProfanityReplies, rien n’est exécuté
   ├─ CommandParser.Parse(texte, objets accessibles, mots de sortie de la pièce)
   │      ─► Command, ou erreur (UnknownVerb : réponse tirée dans une liste ; autres erreurs : message fixe)
   ├─ Go ? ──► sans direction : « Vous ne pouvez pas aller par là. » ; sinon déplacement (§7.2)
   ├─ vérification de l’instrument (Use, Take…) ─► refus éventuel
   ├─ RoomBase.Handle(command, état) ─► résultat propre à la pièce, ou null
   └─ comportements par défaut ─► résultat générique
   ▼
CommandResult (messages, StateChanged)
   ▼
GameApp : ajoute les messages (type réponse), sauvegarde si StateChanged, relit engine.GetView()
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

Les enums de contenu sont enrichis par les documents de contenu, **et également par le monde de test** : les membres `TestRoom*`, `Test*` et `TestAnnouncePlayed` n’existent que pour `TestWorld` et ne doivent jamais apparaître dans le monde réel (§11).

```csharp
public enum RoomId
{
    Bedroom, TechCorridor, HospitalLobby, Street,      // monde livré
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

`GameEvent.WakeAnnouncementDone` garde son nom historique : il marque désormais « message d’accueil joué ». Le fichier `Ids.cs` contient aussi l’extension `Directions.Opposite`, utilisée par les tests de cohérence.

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

`Words` permet de franchir la sortie sans donner de direction cardinale (« sors », « va dans la rue », « monte l’escalier »). Les mots sont fournis **déjà normalisés** (§6.2) par le contenu : le moteur ne les normalise pas, `WorldConsistencyTests` le vérifie. Ils sont non vides, uniques dans une pièce et ne contiennent aucune direction cardinale. `GameEngine.ExitWords()` construit à chaque commande la table mot → direction de la pièce courante.

Les trois états d’affichage sont **calculés**, jamais stockés (`GameEngine.GetView` parcourt les quatre directions à chaque appel) : pas de sortie dans la direction → `Absent` ; sortie sans destination, ou condition fausse → `Blocked` ; sinon `Open`.

### 5.6 Pièces

Une classe par pièce, **sans état propre** : toutes les méthodes reçoivent l’état en paramètre. Les comportements spécifiques (« passer le badge sur le lecteur », « ouvre porte », « lis afficheur ») vivent dans `Handle` de la pièce où se trouve la cible.

```csharp
public abstract class RoomBase
{
    public abstract RoomId Id { get; }
    public abstract string Title { get; }
    public abstract string ImageKey(GameState s);                   // clé d'image
    public abstract string Description(GameState s);                // description courte, affichée à chaque entrée
    public virtual IEnumerable<string> IntroMessages(GameState s) => Array.Empty<string>();  // avant la description ; messages uniques (événement)
    public virtual string? DetailedDescription(GameState s) => null; // « regarde » sans cible ; null = on utilise Description
    public virtual IReadOnlyList<ImagePatch> ImagePatches => Array.Empty<ImagePatch>();      // zones d'image selon l'état
    public virtual IEnumerable<string> OnEnter(GameState s) => Array.Empty<string>();        // messages après la description
    public abstract IReadOnlyList<Exit> Exits { get; }
    public virtual CommandResult? Handle(Command c, GameState s) => null;   // null = non géré : comportements par défaut
}
```

Un `Handle` qui modifie l’état de la partie doit renvoyer `CommandResult.Changed(...)` : c’est ce qui déclenche la sauvegarde automatique (§8). Un `Handle` qui ne fait que renseigner (par exemple `Read`) renvoie `CommandResult.Say(...)`.

### 5.7 Résultat, vue et monde

```csharp
public sealed record CommandResult(IReadOnlyList<string> Messages, bool StateChanged = false)
{
    public static CommandResult Say(params string[] m) => new(m);
    public static CommandResult Changed(params string[] m) => new(m, true);
}

public sealed record InventoryItem(ObjectId Id, string Name);

/// Zone d'image : rectangle source [X1,X2) x [Y1,Y2) en pixels de l'image, et condition d'activation.
public sealed record ImagePatch(int X1, int Y1, int X2, int Y2, Func<GameState, bool> IsActive);

/// Rectangle d'image (coordonnées source).
public readonly record struct ImageRect(int X1, int Y1, int X2, int Y2);

public sealed record RoomView(
    RoomId Room,
    string Title,
    string ImageKey,
    IReadOnlyDictionary<Direction, ExitState> Exits,
    IReadOnlyList<InventoryItem> Inventory,
    IReadOnlyList<ImageRect> ActivePatches)
{
    // Constructeur de compatibilité pour les appels sans ActivePatches
    public RoomView(RoomId room, string title, string imageKey,
        IReadOnlyDictionary<Direction, ExitState> exits, IReadOnlyList<InventoryItem> inventory)
        : this(room, title, imageKey, exits, inventory, Array.Empty<ImageRect>()) { }
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
    public GameEngine(WorldModel world, GameState state, Random? random = null);   // random : graine des tirages (§6.7), tests
    public GameState State { get; }
    public IReadOnlyList<string> Start();    // IntroMessages, Description, OnEnter de la pièce courante
    public CommandResult Execute(string input);
    public RoomView GetView();               // sorties, inventaire, zones d'image actives
}
```

`GetView` renseigne `ActivePatches` : les zones de la pièce courante dont `IsActive(State)` est vrai, dans l’ordre de déclaration. Les zones se **déduisent de l’état** : rien n’est sauvegardé en plus, et la zone disparaît si la condition redevient fausse.

## 6. Analyseur de commandes

### 6.1 Principe

Vocabulaire contrôlé, deux étages : (1) **normalisation et reconnaissance** du texte, (2) **résolution des noms** dans la portée courante. L’analyseur ne cherche pas à comprendre le langage naturel.

### 6.2 Normalisation (identique pour la saisie, les alias, les mots de sortie et la liste de grossièretés)

```text
Normaliser(texte):
  1. minuscules (culture invariante), œ -> oe, æ -> ae (avant décomposition)
  2. suppression des accents (décomposition Unicode FormD, rejet des marques non espacées)
  3. apostrophes (' et ’) remplacées par un espace  ("l'armoire" -> "l armoire")
  4. ponctuation, symboles et espaces multiples supprimés/réduits (le tiret est supprimé : « lèche-cul » donne « lechecul », vérifié au POC V3)
  5. découpage en mots
  6. suppression des articles : le la les l un une des du de d
  7. prépositions ramenées à une forme canonique : sur, avec, a (à, au, aux), dans, vers, contre
```

Les alias des objets sont normalisés par la même fonction au chargement : « lecteur de badge » devient « lecteur badge ». « clé » et « clef » sont deux alias du même objet. Les mots de sortie sont écrits normalisés dans le contenu (« hopital », pas « hôpital »).

### 6.3 Verbes reconnus, directions, mots de pièce

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

**Directions** : `nord`, `sud`, `est`, `ouest` (abréviations `n`, `s`, `e`, `o`). Une direction n’est un déplacement **que si elle est l’unique mot de la saisie** (« est », « n »). Une saisie plus longue qui commence par une direction (« est badge », « n’ouvre pas la porte » qui devient « n ouvre porte ») n’est plus un déplacement : le premier mot est alors cherché dans la table des verbes, il est inconnu, d’où une réponse d’incompréhension. Une direction précédée de `va` / `va vers` / `aller a` reste un déplacement (§6.5).

**Mots de pièce** (`Vocabulary.RoomWords`) : `piece, salle, endroit, lieu, autour, alentours, alentour, ici, environnement, partout, moi`. Ils permettent « regarde pièce », « regarde dans la pièce », « observe autour de moi » (normalisé « observe autour moi »), « regarde ici » : voir §6.5. « chambre » n’en fait volontairement pas partie (c’est un mot de sortie du couloir). Aucun alias d’objet ne contient un mot de pièce (test de cohérence).

**Fermeture du jeu** : interceptée par la couche Game avant d’appeler le moteur. Trois formes, normalisées (`Vocabulary.QuitWords`) : `quitter`, `quitte`, `quittes`. La commande n’est **pas** affichée en écho.

### 6.4 Structures de phrase

```text
<verbe>                       regarde, observe, inventaire, lis
<direction>                   est                                   (seule)
<verbe> <nom>                 prends badge
<verbe> <prép> <nom>          parle a androide, appuie sur bouton   (la préposition est ignorée)
<verbe> <nom1> <prép> <nom2>  utilise badge sur lecteur             (nom1 = instrument, nom2 = cible)
<verbe Look> <mots de pièce>  regarde pièce, observe autour de moi  (Look sans cible)
<mot de sortie>               sors, monte                           (verbe de déplacement ou mot de la pièce)
<verbe Go> [prép] <mot de sortie>   va dans la rue, monte l'escalier
```

### 6.5 Pseudo-code

```text
Parse(input, scope, exitWords = null):   // scope = objets accessibles (§6.6) ; exitWords = mot -> direction (pièce courante)
  mots = Normaliser(input)
  si mots vide -> Erreur(Empty)

  // 1. direction seule : déplacement SEULEMENT si c'est l'unique mot
  si mots[0] est une direction et mots.Count == 1 -> Succès(Go, direction)

  verbe = TableVerbes[mots[0]]  ; si absent -> Erreur(UnknownVerb)     // « est badge » tombe ici
  reste = mots[1..]

  // 2. verbes de la classe Go : jamais de résolution d'objet
  si verbe est de la classe Go -> return ParseGo(reste, mots[0], exitWords)

  si reste vide:
      si verbe in {Look, Inventory, Read} -> Succès(verbe)
      sinon -> Erreur(MissingObject)       // "Prendre quoi ?"

  si reste[0] est une préposition -> la retirer (si rien ne reste : MissingObject)

  // 3. Look seulement : tous les mots restants (hors prépositions) sont des mots de pièce
  //    et au moins un n'est pas "moi" -> Look sans cible
  si verbe == Look: ... -> Succès(Look)

  groupe1 = mots de reste jusqu'à la prochaine préposition
  groupe2 = mots après cette préposition (peut être vide)

  cible1 = Résoudre(groupe1, scope)        // peut échouer : UnknownObject ou Ambiguous
  si groupe2 non vide:
      cible2 = Résoudre(groupe2, scope)
      -> Succès(verbe, Instrument = cible1, Target = cible2)
  sinon:
      -> Succès(verbe, Target = cible1)

ParseGo(reste, verbe, exitWords):
  afterVerb = reste sans préposition initiale
  si afterVerb est exactement une direction -> Succès(Go, direction)     // « va vers est »
  candidats = [verbe, s'il est un mot de sortie de la pièce] + mots de reste (prépositions ignorées)
  si candidats non vide et tous sont des mots de sortie de la pièce
      et désignent une seule direction -> Succès(Go, direction)
  si reste vide:
      si verbe in {va, vas, aller, allez, avance, avancer} -> Erreur(MissingObject)
  -> Succès(Go, sans direction)    // « va porte », « sors » sans sortie correspondante : refusé par le moteur
```

Pour un verbe de la classe `Go`, les mots de sortie sont **prioritaires** sur les alias d’objets (« va rue »). Pour tout autre verbe, seuls les alias d’objets comptent (« regarde escalier » reste un `Look`).

### 6.6 Portée et résolution des noms

**Portée** : `GameEngine.Scope()` renvoie les objets dont l’emplacement est la pièce courante ou l’inventaire **et** qui sont visibles (`VisibleWhen` nul ou vrai). Un objet caché (badge dans l’armoire fermée) n’est pas dans la portée.

**Résolution** (`Resolve`) : le groupe de mots devient un texte unique.
1. Si l’alias normalisé d’un objet de la portée est **exactement égal** à ce texte : cet objet.
2. Sinon, on garde les objets dont au moins un alias **contient tous les mots** du groupe (comparaison de mots entiers).
3. Zéro candidat : `UnknownObject`. Un candidat : cet objet. Plusieurs : `Ambiguous`, avec les noms des candidats.

**Conséquence assumée (décision A1 du POC V2)** : « prends badge » avant « ouvre armoire » ne répond pas « inconnu ». Le badge caché n’est pas dans la portée, mais le mot « badge » est un mot de l’alias « lecteur badge » du lecteur, visible dans la pièce : l’analyseur résout donc le lecteur, que le moteur refuse de prendre (« Vous ne pouvez pas prendre cela. »). Cette réponse ne révèle rien. Test : `Badge_Is_Reachable_Only_After_Opening_The_Wardrobe`.

### 6.7 Erreurs et messages

| Erreur | Réponse du moteur |
|---|---|
| Empty | (rien : la saisie vide est ignorée) |
| UnknownVerb | Réponse **tirée dans une liste** : avec la probabilité `FlavourText.HumourProbability` (0,25) dans `UnknownHumour` (6 messages), sinon dans `UnknownNeutral` (8 messages). `ReplyPicker` garantit que deux réponses consécutives d’une même liste ne sont jamais identiques. |
| MissingObject | « Précisez l’objet de la commande. » |
| UnknownObject | « Vous ne voyez pas cela ici. » |
| Ambiguous | « Précisez : » suivi des noms des candidats joints par « ou » (« le lecteur de l’escalier ou le lecteur du local technique »). |

Seule `UnknownVerb` est variable : les trois autres erreurs sont des indications de jeu et restent fixes. `CommandParser` et `ParseResult` ne connaissent pas les listes : `ParseResult.ErrorMessage` garde un message neutre pour `UnknownVerb`, utilisé par les tests de l’analyseur seul ; le moteur ne l’utilise pas. Le `Random` du moteur est injectable (constructeur) pour des tests déterministes. L’état de `ReplyPicker` n’est pas sauvegardé.

La correction automatique de fautes de frappe n’est pas prévue.

### 6.8 Filtre de grossièretés

`ProfanityFilter.Contains(motsNormalisés)` est appelé **avant** l’analyseur dans `GameEngine.Execute`. Il vérifie des **mots entiers** (jamais des fragments : « contre », « salopette » ne sont pas détectés) :
- **mots isolés** (une centaine de formes) dans un ensemble ;
- **expressions** (suites contiguës de mots) par fenêtre glissante.

Les formes sont écrites déjà normalisées. Si une grossièreté est détectée **n’importe où** dans la saisie, même dans une commande valide (« prends le putain de badge bordel »), le moteur répond par un message de `FlavourText.ProfanityReplies` (10 messages, tirage sans répétition consécutive), **n’exécute rien**, ne modifie pas l’état (`StateChanged` faux) et ne compte rien. Pas de pénalité à ce stade.

Mots volontairement **absents** (sens légitime dans le jeu ou le monde) : `cul` seul (« cul-de-sac »), `bander`, `fumier`, `ordure`, `bouse`, `batard`, `pouffe`, `chatte`, `gueule` seul, `pine`. Les insultes discriminatoires sont hors périmètre. Un test vérifie qu’aucun alias d’objet ne contient un mot de la liste.

## 7. Exécution des commandes

### 7.1 Répartiteur

```text
Execute(input):
  mots = Normaliser(input)
  si mots non vide et ProfanityFilter.Contains(mots):
      return Say(ProfanityReplies tiré par ReplyPicker)         // StateChanged = false

  r = Parse(input, Scope(), ExitWords())
  si erreur == Empty        -> aucun message
  si erreur == UnknownVerb  -> tirage UnknownHumour (p = 0,25) ou UnknownNeutral
  si autre erreur           -> message fixe (§6.7)

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
                                     return Changed(messages d'entrée de la nouvelle pièce, §7.4)
```

Une tentative vers une sortie `Blocked` n’est pas exécutée et renvoie le message explicatif. Un déplacement réussi signale `StateChanged`.

### 7.3 Comportements par défaut

Messages exacts, tels qu’implémentés dans `Core/Messages.cs` :

| Action | Comportement |
|---|---|
| Look sans cible | Renvoie `DetailedDescription(état)` de la pièce s’il n’est pas nul, sinon `Description(état)`. (Une chaîne vide n’est pas traitée comme absente : écart, §15.3.) |
| Look avec cible | Affiche `Describe(état)` de l’objet. |
| Read | Sans cible, ou avec une cible non gérée par la pièce : « Il n’y a rien à lire ici. » (`Messages.NothingToRead`). Les cibles lisibles (`Display`, `BedroomMachines`) sont gérées par le `Handle` de leur pièce, avec le même texte que `Look`. Le moteur ne connaît plus ces identifiants. |
| Take | Si l’objet est portable et présent dans la pièce courante : il passe dans l’inventaire (« Pris. »). Sinon : « Vous ne pouvez pas prendre cela. » |
| Drop | Si l’objet est dans l’inventaire : il passe dans la pièce courante (« Posé. »). Sinon : « Vous ne portez pas cela. » |
| Open | Si l’état courant est exactement `Closed`, passe à `Open` (« Vous ouvrez <nom>. ») ; sinon « Rien ne se passe. » |
| Close | Si l’état courant est exactement `Open`, passe à `Closed` (« Vous fermez <nom>. ») ; sinon « Rien ne se passe. » |
| Use | Sans instrument : « Précisez sur quoi l’utiliser. » (`Messages.UseWithoutInstrument`) **seulement si la cible est dans l’inventaire** ; sinon « Rien ne se passe. ». Avec instrument non géré : « Rien ne se passe. » |
| Talk | « Rien ne se passe. » |
| Inventory | Liste les noms des objets de l’inventaire, ou « Vous ne portez rien. » Un complément éventuel est ignoré. |
| Go sans direction | « Vous ne pouvez pas aller par là. » (`Messages.CannotGo`), sans exception. |

Points de détail :
- Les formes « Vous ouvrez … » / « Vous fermez … » sont **fixes** : elles ne reprennent pas le verbe tapé par le joueur. `Take`, `Drop`, `Open`, `Close` et le déplacement signalent `StateChanged`.
- Le vérificateur d’instrument (§7.1) s’applique dès que la commande porte un `Instrument`, quelle que soit l’action.
- Les réponses à « ouvre » sur les portes et l’ascenseur dépendent de l’état des lecteurs et de la borne : elles sont dans les `Handle` des pièces et **réutilisent les constantes des messages de sortie bloquée** quand le texte est identique.

### 7.4 Entrée dans une pièce

`Start()` et chaque déplacement renvoient, **dans cet ordre** : les messages de `IntroMessages`, la `Description`, puis les messages de `OnEnter`. Pour toutes les pièces sauf la chambre, `IntroMessages` est vide. Dans la chambre, `IntroMessages` renvoie **une seule fois** le message d’accueil du jeu (réveil, voix de synthèse, chemise d’hôpital) : il s’appuie sur l’événement `WakeAnnouncementDone` de `GameState.Events` (si l’événement est présent, rien ; sinon il est ajouté puis le message est renvoyé). Au chargement d’une sauvegarde, `Start()` réaffiche la description sans rejouer ce message. Ces modifications de `Events` pendant `Start()` ne signalent pas `StateChanged` : elles sont sauvegardées à la prochaine commande qui change l’état, ou à la fermeture.

## 8. Sauvegarde

- **Déclenchement** : **(1)** à la fermeture de la fenêtre, et seulement si une partie est en cours (nouvelle ou chargée), dans un bloc `finally` pour couvrir aussi une sortie sur exception (l’exception n’est pas avalée) ; **(2)** après toute commande dont le résultat signale `StateChanged` (donc aussi tout déplacement), via `GameApp.Submit`, uniquement si une partie est en cours. Une seule sauvegarde.
- **Contenu** : pièce courante, emplacements des objets, états des objets, événements. **Aucune définition** (textes, images, sorties, zones d’image) n’est sauvegardée, ni l’historique de la console ni l’historique des commandes rappelables.
- **Version de schéma** : `SchemaVersion`, valeur courante **1**. Si la version est inconnue ou le fichier illisible, la sauvegarde est ignorée proprement (« Continuer » grisé).
- **Format avant chiffrement** (enums écrits par nom, `System.Text.Json` avec `JsonStringEnumConverter`, JSON compact) :

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
- **Emplacement** : dossier Documents de l’utilisateur (`Environment.SpecialFolder.MyDocuments`), sous-dossier `Releve`, fichier `save.dat`. Choix à revoir pour Linux/macOS.
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

### 9.1 Principe et boucle

Une boucle simple : lire les entrées, mettre à jour, dessiner, sans liaison de données. Les noms précis des fonctions raylib sont à **vérifier dans la cheatsheet et les exemples officiels** (voir Références), pas à deviner.

```text
Initialisation : fenêtre 1280×720 (« Relève »), SetTargetFPS(60), SetExitKey(Null),
                 périphérique audio, polices (deux tailles), panneaux, menu, image de fond du menu (optionnelle)

Tant que la fenêtre n'est pas fermée et que « quitter » n'a pas été saisi:
    dt = min(temps de l'image, 0,05 s)
    si le fondu est actif:
        vider la file de caractères ; fondu.Update(dt)
        à la fin du fondu de sortie : StartNewGame(), _playing = vrai, fondu.StartIn()
        (ni le menu, ni le jeu, ni le visualiseur ne reçoivent d'entrée)
    sinon si le visualiseur de texte est ouvert:
        vider la file de caractères ; Update() du visualiseur seul ; s'il renvoie vrai (fermeture),
        _menuEntered = vrai (la sélection du menu est conservée)
    sinon si une partie est en cours: UpdateGame (raccourcis fléchés, inventaire, console)
    sinon:                            UpdateMenu (clavier et souris)
    dessin : fond, scène (vue du lieu, flèches, inventaire, console, ou menu), visualiseur par-dessus s'il est ouvert, fondu en dernier
    musique : mise à jour du flux

À la fermeture (bloc finally) : sauvegarde si une partie est en cours, puis libération des textures
(dont le fond du menu et les images alternatives), des polices et des flux audio.
```

Détails :
- `SetExitKey(KeyboardKey.Null)` : Échap ne ferme plus la fenêtre ; il ferme le visualiseur.
- La saisie lit les caractères en Unicode (`GetCharPressed`), pas les touches physiques : les accents sont saisis quelle que soit la disposition du clavier.
- Les touches de validation (Entrée) sont lues avec `IsKeyPressed`, déclenché sur front : voir §16. Retour arrière, Suppr, ←/→ utilisent `IsKeyPressedRepeat`.
- Retour arrière : la constante est **`KeyboardKey.Backspace`** (`KeyboardKey.Back` est la touche Android).
- Les textures et polices sont chargées après `InitWindow`.
- `GameApp.Submit` : « quitter » intercepté sans écho ; écho de la commande (type *saisie*) ; mémorisation dans l’historique des commandes (§9.3, console) ; exécution ; ajout des réponses (type *réponse*) ; sauvegarde si `StateChanged` ; relecture de la vue. Les raccourcis fléchés passent par `Submit` : leur commande est donc affichée en écho et mémorisée.

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

L’image est **étirée pour remplir exactement `ViewBounds`** (940 × 460 utiles ; proportions non conservées : les images sont en 1774 × 887, ratio 2:1, d’où une déformation d’environ 2 %, acceptée). Le cadre est redessiné après l’image. Si l’image d’un lieu est introuvable, un **rectangle de remplacement** porte le titre de la pièce : le jeu fonctionne sans aucune illustration.

### 9.3 Composants

- **Vue du lieu** (`RoomViewPanel.Draw(RoomView)`) : texture chargée depuis `assets/rooms/<ImageKey>.png`, mise en cache par clé, étirée sur `ViewBounds` avec `DrawTexturePro`. **Zones d’image** : si `RoomView.ActivePatches` n’est pas vide, la texture `<ImageKey>_AltState.png` (même clé, suffixe `_AltState`, mêmes dimensions que la base) est chargée à la demande, mise en cache, et chaque rectangle actif est dessiné par-dessus l’image de base (source = le rectangle dans la texture alternative ; destination = le même rectangle mis à l’échelle de `ViewBounds`), **avant** le cadre. Si le fichier alternatif est absent, les zones sont ignorées sans erreur. Limites connues : dimensions 1774 × 887 codées en dur pour le calcul d’échelle, pas d’arrondi séparé de chaque bord (§15.3).
- **Flèches** (`ArrowsPanel`) : disposition en **croix** sous le titre « Déplacements possibles : », trois rangées de 34 px (`RowHeight`), flèches de 18 px (`ArrowSize`). Nord en haut, Ouest à gauche (libellé puis flèche, aligné sur le bord gauche) et Est à droite (flèche puis libellé, aligné sur le bord droit) au milieu, Sud en bas. Les flèches sont des **têtes de flèche seules, sans queue** (décision de l’utilisateur). `Absent` : rien ; `Blocked` : pointe **creuse** (`DrawTriangleLines`), gris sombre (`Palette.ArrowBlocked`), libellé atténué ; `Open` : pointe **pleine** (`DrawTriangle`), couleur vive (`Palette.ArrowOpen`), libellé normal. Indicateurs, pas des boutons. **Règle raylib** : `DrawTriangle` exige des sommets en ordre **anti-horaire à l’écran**, sinon rien n’est dessiné. Écart : pour Nord et Sud, seule la flèche est centrée, le libellé est décalé à droite (§15.3).
- **Inventaire** (`InventoryPanel`) : liste verticale de libellés précédés d’une icône de remplacement (carré de 16 px), titre « Inventaire : ». Défilement **Ctrl+Haut / Ctrl+Bas, sans Shift**, une ligne par appui, sans répétition. Pas de Page↑/Page↓.
- **Console** (`ConsolePanel`) : historique avec retour à la ligne automatique (largeur mesurée avec la police, réduite de la colonne des boutons), ancré en bas, défilement vertical. **Deux types de ligne**, mémorisés par ligne d’historique : *saisie du joueur* (écho « > commande », `Palette.ConsoleInput`) et *réponse du jeu* (`Palette.ConsoleResponse`) ; les lignes issues du retour à la ligne d’un même message gardent son type. Ligne de saisie avec invite `> ` (aujourd’hui en `Palette.Prompt`, voir §15.3) et curseur clignotant, décalée vers la gauche quand le curseur dépasse le bord. Édition : Retour arrière et Suppr avec répétition, ←/→ (déplacement du curseur), Début/Fin (début et fin de ligne). **Historique des commandes** : toutes les commandes envoyées au moteur (saisie et raccourcis fléchés) sont mémorisées (pile, sans doublon consécutif) ; **Shift+Haut / Shift+Bas** rappellent la commande précédente / suivante dans la ligne de saisie, sans la valider. **Défilement de l’historique de la console** : Page↑ / Page↓ (par page), Début / Fin (début et fin de l’historique), et deux **boutons cliquables** (haut, bas) en géométrie dans une colonne de 28 px à droite de l’aire d’historique : un clic fait défiler de **3 lignes**, sans répétition à clic maintenu (décision de l’utilisateur), bouton grisé quand le défilement est impossible. Tout nouveau message ramène l’historique en bas. Les boutons ne réagissent pas quand le visualiseur est ouvert.
- **Police** (`Ui/Fonts.cs`) : taille 20 (corps, interligne 26) et seconde taille (~64) pour le titre du menu, chargée depuis `assets/fonts/DejaVuSans.ttf` avec une liste explicite de codepoints. Repli sur la police par défaut de raylib si le fichier est absent. **Le jeu de codepoints ne contient pas ▲ ▼ ◄ ►** : tout symbole d’interface est dessiné en géométrie.
- **Assets** (`Ui/Assets.cs`) : résout le dossier `assets` à côté de l’exe, sinon en remontant depuis le répertoire courant. Fournit aussi les chemins du fond du menu et du texte d’aide.
- **Palette** (`Ui/Palette.cs`) : couleurs de l’interface, des flèches (`ArrowOpen`, `ArrowBlocked`) et des lignes de console (`ConsoleInput`, `ConsoleResponse`).

### 9.4 Touches

| Touches | Action | Où |
|---|---|---|
| ↑ ↓ ← → **seules** (ni Shift ni Ctrl) | déplacement : commande « nord », « sud », « ouest », « est » envoyée comme une saisie, avec répétition | `GameApp.UpdateGame` |
| Shift + ↑ / ↓ | rappel de la commande précédente / suivante dans la ligne de saisie | `ConsolePanel` |
| Ctrl + ↑ / ↓ (sans Shift) | défilement de l’inventaire, une ligne | `InventoryPanel` |
| Page↑ / Page↓ | défilement par page de l’historique de la console, et du visualiseur | `ConsolePanel`, `TextViewer` |
| Début / Fin | défilement de l’historique de la console **et** déplacement du curseur de la ligne de saisie (double effet, §15.3) ; défilement du visualiseur | `ConsolePanel`, `TextViewer` |
| ← / → | **déplacement du joueur et** déplacement du curseur de la ligne de saisie (double effet, §15.3) | `GameApp`, `ConsolePanel` |
| Échap ou Entrée | ferme le visualiseur | `TextViewer` |

### 9.5 Police et accents (point critique)

Le jeu est en français : ne pas se reposer sur la police par défaut de raylib. Une police TTF libre de droits est chargée depuis `assets/fonts` avec la liste explicite des codepoints :

- ASCII imprimable (32 à 126) ;
- Latin-1 Supplement et Latin Extended-A (0x00A0 à 0x017F) : accents français, `œ Œ`, guillemets `«` `»` ;
- ponctuation générale : 0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2026.

Police retenue : **DejaVu Sans 2.37**, licence incluse dans `assets/fonts/DejaVuSans-LICENSE.txt`. Un caractère hors de cette liste (par exemple dans `help.txt`) s’afficherait comme un « ? ».

### 9.6 Audio

Deux bandes son, conformément à la note d’intention §10 :

| Piste | Fichiers cherchés | Moment de lecture |
|---|---|---|
| `menu` | `assets/audio/menu.ogg` puis `menu.mp3` | à l’écran du menu (et pendant l’aide) |
| `game` | `assets/audio/game.ogg` puis `game.mp3` | pendant la partie |

Chaque flux est chargé si présent, mis en boucle et mis à jour à chaque image. Changer de piste arrête la précédente. **La bascule vers la piste de jeu se fait dans `StartNewGame` et `ContinueGame`** : avec le fondu, la piste de jeu démarre une fois l’écran noir ; la musique du menu continue pendant le fondu de sortie. « Aide » ne change pas la musique. Si le périphérique audio est indisponible ou si les fichiers sont absents, le jeu continue **sans son**, sans erreur bloquante.

### 9.7 Menu

Au lancement : **trois entrées**, dans l’ordre « Continuer » (grisé si aucune sauvegarde valide), « Nouvelle partie », « Aide » (toujours accessible). La sélection est placée par défaut sur « Continuer » quand une sauvegarde existe, sinon sur « Nouvelle partie ». `MenuScreen.Update` renvoie un choix typé (`Continue`, `NewGame`, `Help`).

- **Clavier** : flèches Haut/Bas (navigation cyclique, sans répétition), Entrée. Valider une entrée grisée ne fait rien.
- **Souris** : pour chaque entrée activée (« Continuer » est ignorée quand elle est grisée), si la souris est dans son rectangle (`EntryRect`) : la sélection prend l’entrée **seulement si la souris a bougé** depuis la dernière image (sinon la souris immobile écraserait la navigation au clavier) ; un appui du bouton gauche valide l’entrée. Écart : un clic n’actualise pas la sélection si la souris n’a pas bougé, et `DrawEntry` recalcule le rectangle au lieu d’utiliser `EntryRect` (§15.3).
- **Fond d’écran** : `assets/menu/menuBackground.png` étiré pour remplir toute la fenêtre, dessiné avant le titre et les entrées. Fichier absent : fond uni (`Palette.Background`).
- **Titre** : « Relève » en grand (seconde taille de police), centré en haut, avec ombre portée.
- **Version** : `GameInfo.Version` (« v0.3 »), en bas à droite du menu, police de 20 px, `Palette.DimText`, ombre de 1 px.
- **Nouvelle partie** : démarre le **fondu** (§9.8) ; la partie n’est créée qu’une fois l’écran noir. **Continuer** : pas de fondu. **Aide** : ouvre le visualiseur (§9.9) en plein écran, titre « Aide », avec le contenu de `assets/texts/help.txt` (UTF-8, lu par `GameApp.OpenHelp`) ; fichier absent : « Texte d’aide introuvable. ». À la fermeture du visualiseur (clavier **ou** souris), `_menuEntered` repasse à vrai et la sélection reste sur « Aide ».

L’existence de la sauvegarde est évaluée **une seule fois au démarrage** (`_canContinue`), et `MenuScreen.Reset` n’est appelé qu’à l’entrée dans l’état menu (drapeau `_menuEntered`).

### 9.8 Fondu d’écran (`Ui/ScreenFade.cs`)

- **États** : `Idle`, `Out` (vers le noir), `In` (depuis le noir). Durées constantes : sortie 0,6 s, entrée 0,8 s.
- **Déclenchement** : choix « Nouvelle partie » dans le menu → `StartOut()`. Quand `Update(dt)` renvoie `FadeEvent.OutFinished`, la boucle appelle `StartNewGame()`, passe `_playing` à vrai puis appelle `StartIn()`. `Update` renvoie `FadeEvent.InFinished` à la fin de l’entrée.
- **Pendant le fondu** : aucune mise à jour du menu, du jeu ni du visualiseur ; la file de caractères est vidée. Le fondu est dessiné **par-dessus tout** (rectangle noir d’opacité variable).
- `dt` est plafonné à 0,05 s. Écart : le premier `Update` après `StartIn()` n’ignore pas son `dt` (§15.3).

### 9.9 Visualiseur de texte (`Ui/TextViewer.cs`)

Composant réutilisable, **superposé** à l’écran courant : aide aujourd’hui, lettre des parents, crédits ou autre texte plus tard. Le déclenchement depuis le moteur (par exemple « lis lettre ») n’existe pas : il demanderait un signal du moteur vers l’interface.

- **API** : `Open(rectangle, titre, texte)`, `IsOpen`, `Update()`, `Draw()`. `Update()` renvoie **vrai uniquement à la fermeture**, par clavier (Échap, Entrée) ou par clic sur « Fermer » ; un clic sur un indicateur ne ferme pas le visualiseur. Le rectangle est un paramètre : plein écran pour l’aide ; une fenêtre partielle est prévue (non déclenchable à ce jour).
- **Opaque et modal** : remplit son rectangle d’une couleur pleine. Tant qu’il est ouvert, `GameApp` n’appelle ni la mise à jour du menu ni celle du jeu ; la file de caractères est vidée à chaque image. La musique continue. Il ne traite aucune entrée dans la frame où il est ouvert.
- **Texte** : retours CRLF normalisés en LF ; retour à la ligne automatique à la largeur du rectangle moins deux marges (mesurée avec `Fonts.MeasureWidth`) ; lignes vides conservées. Écarts : un mot plus large que la zone n’est pas coupé ; la largeur n’est pas réduite de la colonne des indicateurs (§15.3).
- **Défilement** : Page↑ / Page↓ (une page moins une ligne), ↑ / ↓ (une ligne, avec répétition), Début / Fin. Le défilement maximal est `max(0, lignes − lignes visibles)`. **Indicateurs** « texte au-dessus / en dessous » dessinés en triangles (sommets anti-horaires), visibles seulement quand le défilement est possible dans le sens. **Souris** : zone de clic de 30 × 30 px par indicateur ; un appui du bouton gauche défile de **3 lignes** ; **pas de répétition** à clic maintenu ; survol : triangle en `Palette.Prompt` ; seul un appui survenu après l’ouverture compte (pas de clic fantôme).
- **Fermeture** : bouton « Fermer » (appui puis relâchement sur le bouton, survol visible) ou Échap / Entrée.

### 9.10 Règle d’interaction

Voir le §2 : clavier partout ; souris pour les quatre exceptions listées ; curseur visible ; flèches de déplacement non cliquables.

## 10. Étendre le jeu

Pour ajouter un lieu ou un objet :

1. Ajouter les membres nécessaires aux enums `RoomId` / `ObjectId` / `GameEvent`.
2. Créer la classe de la pièce dans `Content/Rooms/` (dérivée de `RoomBase`) : titre, description courte, sorties (avec `Words` si des mots de sortie s’imposent), `Handle` pour les interactions propres au lieu. Textes en constantes en tête de classe. Optionnel : `DetailedDescription` (réponse à « regarde » sans cible), `IntroMessages` (message unique avant la description, avec un événement dans `GameState.Events`), `OnEnter`, `ImagePatches`.
3. Déclarer les objets (`ObjectDef`) dans `Content/Objects/`, avec alias, emplacement initial, état initial, description. Convention : un fichier d’objets par pièce, agrégé par `GameWorld`.
4. Enregistrer la pièce et les objets dans `World`. Si c’est la pièce de départ, positionner `StartRoom`.
5. Ajouter ou mettre à jour les tests de cohérence (§11) et, pour tout comportement propre à une pièce, un test de scénario.

Pour ajouter des **zones d’image** à une pièce : fournir `assets/rooms/<ImageKey>_AltState.png`, aux **mêmes dimensions** que l’image de base (un seul fichier alternatif par pièce) ; déclarer dans la classe de la pièce, en constantes nommées, une quadruple X1, Y1, X2, Y2 par objet altéré (rectangle en pixels de l’image source, X2 et Y2 exclus), avec la condition d’activation (fonction de `GameState`). `ImagePatchTests` vérifie l’existence des fichiers, leurs dimensions (lues dans l’en-tête PNG), la validité des rectangles et leur absence de chevauchement.

Contraintes à respecter :
- Les interactions qui concernent deux pièces ne partagent pas d’état « porte » : la condition d’une sortie lit l’état d’un objet de la pièce de départ.
- Les alias ne doivent pas entrer en collision involontaire entre objets de la même pièce (§6.6), ni contenir un mot de `RoomWords` ni un mot du filtre de grossièretés.
- Les mots de sortie sont écrits normalisés, non vides, uniques dans la pièce, sans direction cardinale (§5.5). Un mot de sortie n’est jamais masqué par un alias d’objet pour un verbe de déplacement.
- Tout `Handle` qui modifie l’état doit renvoyer `Changed` (§5.6), sinon la sauvegarde automatique ne se déclenche pas.
- `ImageKey` doit correspondre à un fichier `assets/rooms/<clé>.png`, sinon le rectangle de remplacement s’affiche : comportement attendu, silencieux. La casse doit correspondre exactement (portabilité Linux).
- Tout texte affiché respecte la convention d’encodage du §2.

## 11. Stratégie de tests (projet `Releve.Engine.Tests`)

Les tests remplacent la validation de schéma qu’offrirait un fichier de données. **128 tests**, tous verts (83 à l’issue du PoC V1, 20 ajoutés au PoC V2, 25 au PoC V3).

| Fichier | Ce qu’il couvre |
|---|---|
| `WorldConsistencyTests.cs` | Cohérence du monde livré et du monde de test : chaque pièce a une classe, chaque objet une définition, chaque destination existe, chaque sortie a son retour sauf `OneWay`, sortie sans destination toujours bloquée, pas de collision d’alias dans une pièce, mots de sortie non vides, uniques et sans direction cardinale, aucun alias ne contient un mot de pièce ni une grossièreté ; compteurs du monde livré (4 pièces, 22 objets) et départ (`Bedroom`). |
| `NormalizationTests.cs` | Accents, articles, apostrophes, ponctuation, prépositions canoniques, alias normalisés comme la saisie. |
| `ParserTests.cs` | Variantes de verbes, directions seules (« est » oui, « est badge » non), verbes de déplacement seuls, Look/Inventory/Read sans complément, mots de pièce, ambiguïté, objet invisible inconnu, verbe inconnu. |
| `ExitWordsTests.cs` | Mots de sortie : priorité sur les alias pour les verbes de déplacement, alias seuls pour les autres verbes, « sors » sans sortie correspondante, une couverture par ligne du tableau des mots de sortie. |
| `EngineBehaviourTests.cs` | Comportements par défaut (§7.3) : `Read`, `Use` conditionné à l’inventaire, `Go` sans direction sans exception. |
| `PocContentTests.cs` | Contenu : sorties sans destination, retours existants, ambiguïtés du couloir, armoire puis badge, fiche patient à l’identique, scénario de référence complet, corbeille, réponses à « ouvre », sauvegarde puis rechargement. |
| `SaveTests.cs` | Aller-retour chiffré, clés et noms d’enums du JSON, fichier corrompu ignoré, version de schéma inconnue ignorée. |
| `BedroomV3Tests.cs` | Message d’accueil unique, ordre introduction / description, descriptions détaillées selon le sac, « regarde pièce » et variantes. |
| `ReplyTests.cs` | Listes de réponses, tirage sans répétition, graine fixe, filtre de grossièretés (mots, expressions, faux positifs), priorité de la grossièreté, erreurs fixes. |
| `ImagePatchTests.cs` | Zones actives selon l’état (monde de test et trois pièces livrées), fichiers et dimensions PNG, rectangles, sauvegarde et rechargement. |
| `EncodingTests.cs` | Pas d’apostrophe droite ni d’accent décomposé dans les textes du moteur (données et sources), pas de « blouse ». |
| `TestWorld/TestWorld.cs` | Monde de test indépendant du contenu réel. |

Les tests génériques s’appuient sur `TestWorld`, pas sur le monde livré. Les tests de contenu s’appuient sur `GameWorld.Create()`. Ajouter ou retirer un membre de `RoomId` / `ObjectId` / `GameEvent` impose de mettre à jour les listes de test et le monde de test.

**Hors tests automatiques** : toute la couche Game (console, flèches, menu, visualiseur, fondu, rendu des zones, sauvegarde automatique) n’est couverte par aucun test. Elle n’est vérifiée que par l’utilisateur, en jouant : l’agent de code ne peut ni voir l’écran ni envoyer de touches ou de clics.

## 12. Initialisation du projet

### 12.1 Livrables

La structure du §4.1 et tout ce qui est décrit dans ce document sont implémentés.

### 12.2 Monde livré

`Content/GameWorld.cs` assemble : `Bedroom` (pièce de départ), `TechCorridor`, `HospitalLobby`, `Street`, reliées Est / Ouest / Nord / Sud. Deux sorties sans destination (Est du couloir, Nord de l’accueil) servent de fausses pistes. Les sorties praticables portent des mots de sortie. 22 objets, dont deux portables (badge, sac poubelle) et deux à visibilité conditionnée (vêtements d’enfant et badge, visibles armoire ouverte). La chambre déclare trois zones d’image, le couloir deux, l’accueil une (§10). La rue est **provisoire** : aucune valeur de lore, aucune image.

### 12.3 Jalons

| Jalon | Contenu | État |
|---|---|---|
| 1 | Projet Engine et tests, monde d’amorçage, sans interface. | Livré |
| 2 | Fenêtre raylib, disposition, console avec saisie et accents. | Livré |
| 3 | Flèches, inventaire, sauvegarde, menu minimal, musique. | Livré |
| 4 | POC V1 : contenu réel, suppression du monde d’amorçage. | Livré |
| 5 | POC V2 : vocabulaire, mots de sortie, console typée, flèches en croix, image étirée, menu à trois entrées, visualiseur, aide, sauvegarde automatique. | Livré |
| 6 | POC V3 : message d’accueil, descriptions détaillées, réponses variées, grossièretés, zones d’image, fondu, menu et visualiseur à la souris, raccourcis fléchés, version affichée. | Livré, écarts au §15.3 |

### 12.4 Critères d’acceptation

- Build et tests au vert (128 tests).
- Les accents s’affichent et se saisissent correctement.
- Les trois états de flèche se mettent à jour après chaque action.
- L’inventaire reflète les objets pris ; la console défile et conserve l’historique.
- Quitter puis « Continuer » restaure la partie ; un arrêt brutal après un changement d’état la restaure aussi.
- Les zones d’image reflètent l’état du jeu, y compris après rechargement.
- Le jeu fonctionne sans fichier audio ni images.
- L’exe publié en autonome démarre sans SDK .NET, avec `raylib.dll` à côté.

## 13. Consignes de réalisation pour l’agent

1. Travailler par jalons, en gardant build et tests au vert.
2. Ne pas ajouter de fonctionnalité, de projet ni de dépendance non mentionnés. Aucune commande git d’écriture (`AGENTS.md`).
3. Vérifier chaque appel raylib dans la cheatsheet et les exemples officiels plutôt que de s’appuyer sur la mémoire. Contraintes du binding 8.1.0 relevées : `IsFontValid` / `IsTextureValid` (il n’existe ni `IsFontReady` ni `IsTextureReady`), `char.ConvertFromUtf32`, `music.Looping` (champ de structure), **`KeyboardKey.Backspace`** et non `KeyboardKey.Back`, **`DrawTriangle` en ordre anti-horaire** à l’écran.
4. Signaler explicitement tout écart ou ambiguïté plutôt que choisir silencieusement.
5. À la fin, fournir un récapitulatif qui distingue ce qui est vérifié dans le jeu réel, ce qui ne l’est que par les tests et ce qui attend une validation humaine. Ne jamais déclarer « vérifié dans le jeu réel » ce que l’agent n’a pas observé.

## 14. Points à valider

1. **Résolution cible** : 1280×720. Images de lieux : trois sur quatre fournies, avec leur variante `_AltState` ; la rue provisoire est sans image.
2. **Police TTF** : DejaVu Sans 2.37, licence incluse.
3. **Touches** : voir §9.4 ; deux doubles effets à trancher (§15.3, points 9 et 10).
4. **Dossier de sauvegarde** : `Documents/Releve/save.dat` ; le dossier de données de l’application reste plus conventionnel, notamment pour Linux/macOS.
5. **Page d’aide** : implémentée avec un texte de remplissage (lorem ipsum) ; le vrai texte reste à rédiger.
6. **Migration .NET** : non planifiée (décision de l’utilisateur).
7. **Textes provisoires** : réponses d’incompréhension et de grossièreté, descriptions de la chambre, message d’accueil.

## 15. Écarts connus et points ouverts

### 15.1 Défauts recensés puis corrigés (historique)

Menu : navigation et relecture permanente de la sauvegarde ; Retour arrière (`Backspace`) ; flèches (ordre des sommets) ; plantage « va <objet> » ; sauvegarde sur changement d’état et en `finally` ; bascule de la musique ; image étirée ; écart d’alias « prends badge » (accepté, §6.6) ; apostrophes droites dans les textes du moteur et de `GameApp` ; titre « Déplacements possibles : » accentué ; caractères corrompus de `GameEngine.cs` ; message du lecteur de l’escalier ; `Read` découplé du moteur ; `Use` conditionné à l’inventaire ; direction en premier mot ; verbes `vas/allez/avance/avancer` seuls ; fermeture de l’aide à la souris et sélection conservée ; menu cliquable ; indicateurs du visualiseur cliquables ; fondu qui s’enchaîne ; version non tronquée ; conflit des flèches de déplacement avec le défilement de la console.

### 15.2 Décisions de l’utilisateur

- Boutons de défilement de la console : un clic = 3 lignes, pas de répétition à clic maintenu.
- Flèches de déplacement : têtes seules, sans fût.
- Image du lieu : étirée, déformation d’environ 2 % acceptée.
- Sauvegarde : après chaque commande qui change l’état.
- Raccourcis clavier (§9.4) : ↑↓←→ seules = déplacements ; Shift+↑/↓ = historique ; Ctrl+↑/↓ = inventaire ; Page↑/↓ = console et visualiseur.
- Zone d’image de l’armoire (chambre) : coordonnées corrigées par l’utilisateur après vérification visuelle.

### 15.3 Écarts connus à l’issue du POC V3 (relevés à la lecture du code, non corrigés)

À traiter au POC V4. Ils ne bloquent pas le jeu.

**Exigences du plan V3 non réalisées**
1. `ConsolePanel` : l’invite `> ` de la ligne de saisie est dessinée en `Palette.Prompt` au lieu de `Palette.ConsoleInput`.
2. `ConsolePanel` : le défilement vers le haut va jusqu’à `Count − 1` ; avec Début ou le bouton haut, une seule ligne reste visible. Le maximum doit être `max(0, Count − lignes visibles)`.
3. `ConsolePanel.Wrap` et `TextViewer.WrapText` : un mot plus large que la zone d’affichage n’est pas coupé (la condition `current.Length == 0` l’accepte tel quel).
4. `TextViewer.Open` : `WrapText` reçoit `Width − 2 × Padding` ; la colonne des indicateurs (40 px) n’est pas retirée, une ligne pleine passe sous les indicateurs.
5. `ScreenFade` : le premier `Update` après `StartIn()` n’ignore pas son `dt` (le plafond de 0,05 s limite l’effet).
6. `RoomViewPanel` : pas d’arrondi séparé de chaque bord des zones ; dimensions 1774 × 887 codées en dur au lieu de celles de la texture ; commentaire périmé « en conservant les proportions ».
7. `MenuScreen` : `DrawEntry` ne partage pas `EntryRect` avec `Update` ; un clic ne fixe pas la sélection si la souris n’a pas bougé depuis la dernière image.
8. `ArrowsPanel` : pour Nord et Sud, seule la flèche est centrée (le libellé est décalé à droite) ; le paramètre `centred` est toujours vrai (code mort) ; la constante `CellWidth` est inutilisée.
9. `GameEngine` : `Look` sans cible teste `DetailedDescription` contre `null` seulement ; une chaîne vide est renvoyée telle quelle.

**Défauts de touches (nouveaux, hors plan)**
10. **← / →** déplacent le joueur (`GameApp.UpdateGame`) **et** le curseur de la ligne de saisie (`ConsolePanel.ReadCharacters`) ; ↑ / ↓ déplacent aussi le joueur pendant une saisie en cours. Corriger une faute de frappe dans « ouvre armoire » exécute « ouest ». *Recommandation* : ne déplacer le joueur que si la ligne de saisie est vide. **Décision de l’utilisateur attendue.**
11. **Début / Fin** font défiler la console **et** déplacent le curseur de la ligne de saisie. *Recommandation* : réserver Début / Fin à la ligne de saisie et utiliser Ctrl+Début / Ctrl+Fin pour le défilement. **Décision de l’utilisateur attendue.**

**Mineurs**
12. `TextViewer.IndicatorDownRect` : variable `lastVisible` inutilisée ; zone de clic des indicateurs décalée d’environ 5 px verticalement par rapport au triangle.
13. `InventoryPanel` : commentaire de classe périmé (cite Page↑/Page↓, que le code ne gère pas).
14. Icônes d’inventaire : toujours des carrés de remplacement.
15. Commentaires de code sans accents dans plusieurs fichiers de la couche Game (cosmétique).
16. `ParseResult.ErrorMessage` garde un message neutre pour `UnknownVerb`, jamais utilisé par le moteur (utile aux seuls tests de l’analyseur).

**Non relus pour cette version** : les trois classes de pièces et les fichiers d’objets, `Vocabulary.cs`, `TextNormalizer.cs`, `Messages.cs`. Le moteur est couvert par les 128 tests.

## 16. Outillage d’automatisation (hors build)

Le dossier `agentTools/` contient l’automatisation utilisée pour vérifier le jeu réel au POC V1. Il ne fait **pas partie du build** : aucun `.cs` n’y appartient. L’agent de code actuel (Nemotron) ne sait pas l’utiliser.

| Fichier | Rôle |
|---|---|
| `ReleveGameTools.psm1` | Module PowerShell : focus clavier, envoi de commandes, frappes Entrée maintenues, vidage de la ligne de saisie, captures, mesure des états de flèche. |
| `Run-Hidden.ps1` | Exécute un script dans un processus sans fenêtre. |
| `Send-Commands.ps1` | Envoie un fichier de commandes, avec validation d’entrées de menu. |
| `scenarios/` | Scénarios réutilisables (référence du PoC, quitter-et-sauvegarder). |
| `README.md` | Recette et pièges recensés. |

Contraintes vérifiées :
1. **L’injection doit partir d’un processus sans fenêtre** : un terminal visible vole le premier plan.
2. **Les caractères passent par `SendKeys`**, un caractère à la fois (la file de raylib est limitée à 16 codepoints par image).
3. **Les touches d’état (Entrée, Retour arrière) doivent être maintenues** (~60-80 ms) avec `keybd_event` : `IsKeyPressed` détecte un front.
4. **Les fichiers de commandes sont lus en UTF-8**.
5. **La capture `PrintWindow` sert de référence** pour les mesures pixeliques. Les zones de `Get-ReleveArrowStates` sont déduites des constantes de `ArrowsPanel.cs` et `Layout.cs` et doivent être recalibrées si la mise en page change.
6. **Non automatisables** : la répétition de touche maintenue, les clics souris (boutons de la console, « Fermer », menu, indicateurs du visualiseur) et le fondu (durées, fluidité) : validation humaine.
7. **À re-vérifier** : la recette de rejeu du compte rendu du POC V2 appelle `Wait-ReleveGameWindow`, qui n’est pas dans la liste du module ; ne pas s’y fier avant de l’avoir testée.

## Références

- Raylib-cs sur NuGet : <https://www.nuget.org/packages/Raylib-cs/8.1.0>
- Cheatsheet raylib (liste des fonctions) : <https://www.raylib.com/cheatsheet/cheatsheet.html>
- Exemples officiels raylib : <https://github.com/raysan5/raylib/tree/master/examples>
- Politique de support .NET : <https://dotnet.microsoft.com/platform/support/policy/dotnet-core>