# Jeu "Relève" — Architecture technique

> Document de référence — version 0.4
> Valable pour toute la durée du projet. Il décrit la structure du code, pas le contenu du jeu.
> À fournir à l’agent de développement avec la note d’intention (V5) et le mockup bitmap de l’interface. Le document univers/scénario n’est pas nécessaire à ce stade.

> **État de cette version.** La V0.1 décrivait l’architecture cible, la V0.2 le reflet du code livré à l’issue de l’initialisation. La V0.3 intègre les changements apportés par le PoC (« ImplementationPOCV0.1.md », compte rendu dans « CompteRenduPOCV0.1.md ») : le monde d’amorçage est remplacé par le contenu réel, les tests propres au contenu apparaissent, deux défauts recensés à la version précédente sont corrigés, et un dossier d’automatisation hors build est ajouté (§16). Chaque section a été reconfrontée aux fichiers réels et corrigée lorsqu’elle s’en écartait.

## 1. But et portée

Ce document définit l’architecture technique du jeu d’aventure textuel et graphique « Relève » : plateforme, découpage du code, modèle de domaine, analyseur de commandes, exécution, sauvegarde et couche d’affichage. Il sert à **initialiser le projet** (§12) puis de référence pour tout développement ultérieur.

Il ne contient pas le contenu du jeu (lieux, objets, textes). Celui-ci est décrit dans des documents séparés qui s’appuient sur ce document.

**Avancement au moment de cette version.** Les trois jalons d’initialisation du §12.3 sont livrés, ainsi que le contenu du PoC : quatre lieux, 22 objets, chambre de réveil comme départ, scénario de référence jouable de bout en bout dans le jeu réel. `dotnet build` est vert et les **83 tests** du moteur passent. Le monde d’amorçage technique des premières versions a été supprimé, comme prévu. Un dossier d’automatisation hors build (`agentTools/`, §16) permet de piloter la fenêtre réelle du jeu pour les vérifications.

## 2. Principes directeurs

- **Petit développement « à l’ancienne »** : le projet est un seul jeu, pas un moteur multi-jeux.
- **Contenu en classes C#** : lieux, objets, textes, alias, sorties sont écrits dans le code. Aucun fichier JSON/YAML de paramétrage du contenu. (Le JSON n’est utilisé que pour la sauvegarde.)
- **Moteur isolé** : le projet du moteur ne dépend ni de raylib ni d’aucune bibliothèque d’affichage ou de son.
- **Pas de MVVM ni de framework UI** : l’interface est un rendu raylib qui lit l’état du moteur à chaque image.
- **Pas de sur-élaboration** : ne rien ajouter qui ne soit pas décrit dans les documents fournis.
- **En cas de contradiction ou d’ambiguïté** entre documents : signaler le point à l’utilisateur au lieu de trancher seul. La note d’intention fait référence pour l’interface et le comportement du jeu. Deux contradictions ont été tranchées de cette façon et sont signalées aux §9.6 et §14.

### Conventions

- Identifiants de code en anglais, textes affichés au joueur en français, commentaires brefs en français.
- Textes joueur : jamais de pensées, souvenirs ni émotions attribués au joueur ; formulations non genrées (pas de participe passé accordé au joueur).
- Dans chaque classe de pièce, les textes sont des constantes regroupées en tête de classe, séparées de la logique.
- **Encodage des textes joueur** : caractères **précomposés** et apostrophe typographique `’` obligatoires. raylib dessine chaque codepoint indépendamment : un accent décomposé (`e` + U+0301) s’affiche en charabia. Vérifier par recherche de `[\u0300-\u036f]` après toute modification de texte.
- Une sortie impossible est expliquée par un message qui décrit ce qui bloque, **sans dire comment débloquer** (note d’intention §2).

## 3. Choix techniques

| Sujet | Décision |
|---|---|
| Plateforme | .NET 8, C# 12, `Nullable` activé. Fin de support de .NET 8 prévue le 10/11/2026, migration non planifiée (choix assumé). |
| Affichage | Bibliothèque **Raylib-cs** (`Raylib-cs`), version **8.1.0**, fenêtre fixe. Aucune autre bibliothèque d’affichage. |
| Cible | Windows x64, publication autonome (runtime copié à côté de l’exe). |
| Portabilité | L’architecture doit permettre Linux/macOS sans refonte du moteur. Aucun build Linux/macOS n’est livré à ce stade. |
| Éditeur | VS Code et ligne de commande `dotnet`. Aucun conceptneur visuel requis. |
| Tests | xUnit sur le moteur. |
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

Les dossiers d’assets sont copiés dans le répertoire de sortie (`<None Include="assets\**\*" CopyToOutputDirectory>`). **Vérifié après publication** : `raylib.dll`, les assets et la police sont bien à côté de `Releve.Game.exe` (§14, point 6).

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
                               Command, Location, ObjectDef, Exit, CommandResult, InventoryItem, RoomView
        GameState.cs           état mutable, seul élément sauvegardé
        Messages.cs            textes neutres du moteur (§6.7, §7.3)
      Parsing/
        TextNormalizer.cs      normalisation (§6.2)
        Vocabulary.cs          tables verbes / directions, mots de fermeture (§6.3)
        CommandParser.cs       analyse et résolution des noms (§6.4-§6.6)
        ParseResult.cs         ParseError, ParseResult
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
      GameApp.cs               boucle principale, états menu / jeu (§9.1)
      Ui/
        Layout.cs              constantes de disposition (§9.2)
        Palette.cs             couleurs de l'interface
        Fonts.cs               police TTF et codepoints (§9.5)
        Assets.cs              localisation du dossier assets
        RoomViewPanel.cs       vue du lieu et rectangle de remplacement
        ArrowsPanel.cs         croix des déplacements
        InventoryPanel.cs      inventaire à libellés
        ConsolePanel.cs        historique, saisie, défilement
        MenuScreen.cs          menu minimal
      Audio/
        MusicPlayer.cs         bandes son en boucle (§9.6)
      assets/                  rooms/, fonts/, audio/
  tests/
    Releve.Engine.Tests/
      TestWorld/TestWorld.cs   monde de test indépendant du contenu (§11)
      TestSupport.cs           fabrication d'un moteur prêt à jouer
      WorldConsistencyTests.cs, NormalizationTests.cs, ParserTests.cs,
      EngineBehaviourTests.cs, PocContentTests.cs, SaveTests.cs
  Docs/                        documents de référence et comptes rendus
  agentTools/                  automatisation hors build (§16)
```

> **Collision de noms à connaître** : la classe `World` et l’espace de noms `Releve.Engine.World` portent le même nom. Dans les fichiers qui font `using Releve.Engine.World;`, l’identifiant `World` est ambigu ; un alias global est donc déclaré dans `GlobalUsings.cs` : `WorldModel = Releve.Engine.World.World`. Tout nouveau fichier manipulant le monde utilise `WorldModel` comme type de paramètre (comme `GameEngine` et `GameWorld`).

### 4.2 Règle de dépendance

`Releve.Game` → `Releve.Engine`, jamais l’inverse. Le moteur ne contient ni dessin, ni lecture de touches, ni son. Il reçoit du texte, renvoie du texte et un état à afficher.

### 4.3 Cycle d’une commande

```text
Saisie (Game) ──► GameEngine.Execute(texte)
                       │
                       ├─ CommandParser.Parse(texte, objets accessibles) ─► Command ou erreur
                       ├─ Go ? ──────────────────────► déplacement (sorties conditionnées)
                       ├─ Vérification de l'instrument (Use) ─► refus éventuel
                       ├─ RoomBase.Handle(command, état) ─► résultat propre à la pièce, ou null
                       └─ Comportements par défaut ───► résultat générique
                       ▼
                   CommandResult (messages) + GameState modifié
Game affiche les messages dans la console, puis relit engine.GetView().
```

## 5. Modèle de domaine (projet Engine)

### 5.1 Énumérations

Les identifiants sont des `enum` (pas de chaînes) : le compilateur détecte les fautes de frappe. **Dans la sauvegarde, les enums sont écrits par leur nom**, jamais par leur valeur numérique.

```csharp
public enum GameAction { Look, Take, Drop, Open, Close, Use, Talk, Go, Inventory }
public enum Direction { North, South, East, West }
public enum ObjectState { None, Closed, Open, Locked, Unlocked, Activated }
public enum ExitState { Absent, Blocked, Open }   // Absent = rien affiché ; Blocked = flèche grisée ; Open = flèche active
public enum LocationKind { Room, Inventory, Nowhere }
```

Les enums de contenu sont enrichis par les documents de contenu, **et également par le monde de test** : les membres `TestRoom*`, `Test*` et `TestAnnouncePlayed` n’existent que pour `TestWorld` et ne doivent jamais apparaître dans le monde réel (§11). Contenu livré à ce jour (les membres de test, inchangés, ne sont pas rélistés ici) :

```csharp
public enum RoomId
{
    Bedroom, TechCorridor, HospitalLobby, Street,      // monde livré (PoC §2)
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
    bool OneWay = false);                   // retour volontairement absent côté destination
```

Les trois états d’affichage sont **calculés**, jamais stockés (`GameEngine.GetView` parcourt les quatre directions à chaque appel) : pas de sortie dans la direction → `Absent` ; sortie sans destination, ou condition fausse → `Blocked` ; sinon `Open`.

### 5.6 Pièces

Une classe par pièce, **sans état propre** : toutes les méthodes reçoivent l’état en paramètre. Les comportements spécifiques (par exemple « passer le badge sur le lecteur ») vivent dans `Handle` de la pièce où se trouve la cible.

```csharp
public abstract class RoomBase
{
    public abstract RoomId Id { get; }
    public abstract string Title { get; }
    public abstract string ImageKey(GameState s);                   // clé d'image, variante possible selon l'état
    public abstract string Description(GameState s);                // texte affiché à l'entrée
    public virtual IEnumerable<string> OnEnter(GameState s) => Array.Empty<string>();   // messages d'entrée supplémentaires
    public abstract IReadOnlyList<Exit> Exits { get; }
    public virtual CommandResult? Handle(Command c, GameState s) => null;   // null = non géré : comportements par défaut
}
```

### 5.7 Résultat, vue et monde

```csharp
public sealed record CommandResult(IReadOnlyList<string> Messages, bool StateChanged = false)
{
    public static CommandResult Say(params string[] m) => new(m);
    public static CommandResult Changed(params string[] m) => new(m, true);
}

public sealed record InventoryItem(ObjectId Id, string Name);

public sealed record RoomView(
    RoomId Room,
    string Title,
    string ImageKey,
    IReadOnlyDictionary<Direction, ExitState> Exits,
    IReadOnlyList<InventoryItem> Inventory);

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
    public GameEngine(WorldModel world, GameState state);
    public GameState State { get; }
    public IReadOnlyList<string> Start();    // à appeler au démarrage : description + OnEnter de la pièce courante
    public CommandResult Execute(string input);
    public RoomView GetView();
}
```

## 6. Analyseur de commandes

### 6.1 Principe

Vocabulaire contrôlé, deux étages : (1) **normalisation et reconnaissance** du texte, (2) **résolution des noms** dans la portée courante. L’analyseur ne cherche pas à comprendre le langage naturel.

### 6.2 Normalisation (identique pour la saisie et pour les alias)

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

Les alias des objets sont normalisés par la même fonction au chargement : « lecteur de badge » devient « lecteur badge ». « clé » et « clef » sont deux alias du même objet.

### 6.3 Verbes reconnus

Le verbe est le premier mot après normalisation. Cette table est celle du moteur ; elle s’étend au fil du projet.

| GameAction | Formes acceptées (normalisées) |
|---|---|
| Look | regarde, regarder, examine, examiner, observe, observer, inspecte, inspecter |
| Take | prends, prend, prendre, prenez, prennez, ramasse, ramasser, saisis, saisir |
| Drop | pose, poser, depose, deposer, lache, lacher, jette, jeter |
| Open | ouvre, ouvrir, ouvrez |
| Close | ferme, fermer, fermez |
| Use | utilise, utiliser, utilisez, passe, passer, applique, appliquer, appuie, appuyer |
| Talk | parle, parler, discute, discuter |
| Go | va, vas, aller, allez, avance, avancer |
| Inventory | inventaire, inv, i |

Directions : `nord`, `sud`, `est`, `ouest` (abréviations `n`, `s`, `e`, `o`). Une direction seule (« Est ») ou précédée de `va` / `va vers` / `aller a` équivaut à `Go`.

La fermeture du jeu est interceptée par la couche Game avant d’appeler le moteur. Trois formes sont reconnues, normalisées : `quitter`, `quitte`, `quittes`. La commande n’est **pas** affichée dans la console : `GameApp.Submit` détecte le mot et demande la fermeture avant tout écho.

### 6.4 Structures de phrase

```text
<verbe>                       regarde, inventaire
<direction>                   est
<verbe> <nom>                 prends badge
<verbe> <prép> <nom>          parle a androide, appuie sur bouton   (la préposition est ignorée)
<verbe> <nom1> <prép> <nom2>  utilise badge sur lecteur             (nom1 = instrument, nom2 = cible)
```

### 6.5 Pseudo-code

```text
Parse(input, scope):          // scope = objets accessibles (voir §6.6)
  mots = Normaliser(input)
  si mots vide -> Erreur(Empty)

  // 1. direction seule : testée AVANT la table des verbes
  si mots[0] est une direction -> Succès(Go, direction)

  verbe = TableVerbes[mots[0]]  ; si absent -> Erreur(UnknownVerb)
  reste = mots[1..]

  // 2. « va [prép] direction » : la direction doit être le seul mot restant
  si verbe == Go:
      si reste commence par une préposition -> la retirer
      si reste est vide -> Erreur(MissingObject)          // « va » seul
      si reste est exactement une direction -> Succès(Go, direction)

  si reste vide:
      si verbe in {Look, Inventory} -> Succès(verbe)
      sinon -> Erreur(MissingObject)       // "Prendre quoi ?"

  si reste[0] est une préposition -> la retirer (si rien ne reste : MissingObject)
  groupe1 = mots de reste jusqu'à la prochaine préposition
  groupe2 = mots après cette préposition (peut être vide)

  cible1 = Résoudre(groupe1, scope)        // peut échouer : UnknownObject ou Ambiguous
  si groupe2 non vide:
      cible2 = Résoudre(groupe2, scope)
      -> Succès(verbe, Instrument = cible1, Target = cible2)
  sinon:
      -> Succès(verbe, Target = cible1)

Résoudre(groupe, scope):
  texte = groupe joint par des espaces
  candidats = objets de scope dont un alias normalisé est EXACTEMENT égal à texte
  si aucun:
      candidats = objets de scope dont un alias CONTIENT tous les mots du groupe
  si 0 candidat  -> Erreur(UnknownObject)
  si 1 candidat  -> cet objet
  si > 1         -> Erreur(Ambiguous, noms des candidats)
```

### 6.6 Portée

Les objets accessibles pour une commande sont ceux dont l’emplacement est la pièce courante ou l’inventaire, **et** dont `VisibleWhen` est nul ou vrai. Un objet que le joueur ne peut pas voir est donc inconnu de l’analyseur.

**Conséquence pour l’écriture du contenu** : la correspondance partielle teste si *tous les mots du groupe* figurent dans l’alias (et non l’inverse). Deux objets de la même pièce partageant un mot d’alias (« lecteur de l’escalier » et « lecteur du local technique ») deviennent ambigus dès que le joueur tape « lecteur ». Les tests de cohérence du §11 détectent ces collisions.

> **Écart connu de la résolution partielle** (relevé pendant le PoC, documenté, non corrigé — §15, point 6) : un objet **invisible** reste partiellement identifiable par ses alias. Dans la chambre, « prends badge » avant « ouvre armoire » résout « badge » vers « le lecteur de badge » et refuse la prise (« Vous ne pouvez pas prendre cela. ») au lieu de signaler un objet inconnu. Comportement documenté par un test (`Badge_Is_Reachable_Only_After_Opening_The_Wardrobe`). Décision PoC V2 (A1) : accepter le comportement, ne rien révéler sur les objets cachés. Toute correction silencieuse serait sans effet sur l'expérience joueur.

### 6.7 Erreurs et messages

| Erreur | Message (neutre) |
|---|---|
| Empty | (rien : la saisie vide est ignorée) |
| UnknownVerb | « Cette commande n’est pas comprise. » |
| MissingObject | « Précisez l’objet de la commande. » |
| UnknownObject | « Vous ne voyez pas cela ici. » |
| Ambiguous | « Précisez : » suivi des noms des candidats joints par « ou » (« le lecteur de l’escalier ou le lecteur du local technique »). |

La correction automatique de fautes de frappe n’est pas prévue. Les formes courantes sont couvertes par la table de verbes et les alias.

## 7. Exécution des commandes

### 7.1 Répartiteur

```text
Execute(input):
  r = Parse(input, scope courant)
  si erreur == Empty    -> aucun message (saisie vide ignorée)
  si autre erreur       -> message correspondant (§6.7)

  c = commande
  si c.Action == Go -> return TryMove(c.Direction)

  si c.Instrument != null et !état.IsInInventory(c.Instrument):
      return « Vous ne l'avez pas sur vous. »

  // 1. comportement propre à la pièce
  res = pièceCourante.Handle(c, état)
  si res != null -> return res

  // 2. comportements par défaut
  return Défauts(c)
```

### 7.2 Déplacement

```text
TryMove(dir):
  sortie = pièceCourante.Exits.Find(dir)
  si absente                      -> « Vous ne pouvez pas aller par là. »
  si To est null ou IsOpen faux   -> sortie.BlockedMessage, ou « Vous ne pouvez pas aller par là. » à défaut
  sinon                           -> état.CurrentRoom = sortie.To
                                     return Description de la nouvelle pièce + OnEnter(état)
```

Une tentative vers une sortie `Blocked` n’est pas exécutée et renvoie le message explicatif.

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
| Use | Sans instrument : « Précisez sur quoi l’utiliser. » (`Messages.UseWithoutInstrument`). Avec instrument non géré : « Rien ne se passe. » |
| Talk | « Rien ne se passe. » |
| Inventory | Liste les noms des objets de l’inventaire, ou « Vous ne portez rien. » Un complément éventuel est ignoré. |
| Go sans direction | « Vous ne pouvez pas aller par là. » (`Messages.CannotGo`) — aucune exception n’est levée. |

Points de détail à connaître :

- Les formes « Vous ouvrez … » / « Vous fermez … » sont **fixes** : elles ne reprennent pas le verbe tapé par le joueur, et ne changent d’état que si l’objet est exactement dans l’état opposé.
- Le vérificateur d’instrument (§7.1) s’applique dès que la commande porte un `Instrument`, quelle que soit l’action : un `Take` rédigé « prends caisse avec levier » exige donc le levier dans l’inventaire.
- Le cas `Action` inconnue ou `Go` sans direction est ramené à « Rien ne se passe. » / « Précisez l’objet de la commande. » ; ces branches sont défensives, l’analyseur garantissant déjà une cible pour les verbes qui en exigent une.

### 7.4 Entrée dans une pièce

`Start()` et chaque déplacement renvoient la description de la pièce, puis les messages de `OnEnter`. Les événements uniques (annonce, scène) utilisent `GameState.Events` pour ne se produire qu’une fois. Au chargement d’une sauvegarde, `Start()` réaffiche la description de la pièce sans rejouer les événements déjà notés.

## 8. Sauvegarde

- **Déclenchement** : **(1)** à la fermeture de la fenêtre uniquement, et seulement si une partie est en cours (nouvelle ou chargée), écrit dans un bloc `finally` pour couvrir aussi une sortie sur exception ; **(2)** après toute commande dont le résultat signale `StateChanged` (donc aussi tout déplacement), via `GameApp.Submit`, uniquement si une partie est en cours. Une seule sauvegarde. Le chemin « quitter » → écriture du fichier a été vérifié dans le jeu réel pendant le PoC V1 et le PoC V2.
- **Contenu** : pièce courante, emplacements des objets, états des objets, événements. **Aucune définition** (textes, images, sorties) n’est sauvegardée.
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
                 périphérique audio, police, panneaux, menu

Tant que la fenêtre n'est pas fermée et que « quitter » n'a pas été saisi:
    si une partie est en cours: lire la console, exécuter la commande, relire GetView()
    sinon:                     afficher et mettre à jour le menu
    dessin (fond, vue du lieu, flèches, inventaire, console — ou le menu)
    musique: mise à jour du flux

À la fermeture : sauvegarde si une partie est en cours, puis libération des textures,
de la police et des flux audio.
```

Détails vérifiés :

- `SetExitKey(KeyboardKey.Null)` : la touche Échap ne ferme plus la fenêtre. La fermeture passe par le bouton de la fenêtre ou par la commande « quitter ».
- La saisie lit les caractères en Unicode (`GetCharPressed`), pas les touches physiques : les accents sont saisis quelle que soit la disposition du clavier.
- Les touches d’édition et de validation de la console (Entrée, Retour arrière, flèches) sont lues avec `IsKeyPressed`, **déclenché sur front** : voir §16 pour la conséquence sur l’automatisation.
- « quitter » n’est pas écho dans la console (§6.3).

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

L’image est **étirée pour remplir exactement `ViewBounds`** (proportions non conservées, déformation horizontale acceptée). Le cadre (`DrawRectangleLinesEx`) est redessiné après l’image. Si l’image d’un lieu est introuvable, un **rectangle de remplacement** porte le titre de la pièce : le jeu fonctionne sans aucune illustration.

La console calcule à chaque image le nombre de lignes visibles et ancre l’historique en bas, juste au-dessus de la ligne de saisie. Une colonne de boutons de défilement (~28 px) à l’extrémité droite de l’aire de l’historique réduit d’autant la largeur de retour à la ligne.

### 9.3 Composants

- **Vue du lieu** : texture chargée depuis `assets/rooms/<ImageKey>.png`, mise en cache par clé, **étirée pour remplir exactement `ViewBounds`** avec `DrawTexturePro` (proportions non conservées). Le cadre est redessiné après l’image. Si l’image est introuvable, rectangle de remplacement.
- **Flèches** : disposition en **croix** centrée horizontalement (nord en haut, sud en bas, ouest à gauche avec libellé à sa gauche, est à droite avec libellé à sa droite) avec titre « Déplacements possibles : », dessinées avec **têtes de flèches seules** (plus de fûts). `Absent` : rien ; `Blocked` : flèche **creuse** (contour seul), gris sombre, libellé atténué ; `Open` : flèche **pleine**, couleur vive, libellé normal. La distinction ne repose pas que sur la couleur. Ce sont des **indicateurs**, pas des boutons : aucune interaction à la souris. La grille de dessin (constantes de `ArrowsPanel.cs`) est reproduite par la mesure pixelique de `agentTools/` (§16).
- **Inventaire** : liste verticale de libellés précédés d’une icône de remplacement (carré de 16 px), titre « Inventaire : », défilement au clavier.
- **Console** : historique avec retour à la ligne automatique (largeur mesurée avec la police, réduite de la colonne des boutons de défilement), ancré en bas, défilement vertical. **Deux types de ligne** : saisie du joueur (écho `> commande`, couleur `ConsoleInput`) et réponse du jeu (couleur `ConsoleResponse`). Ligne de saisie avec invite `> ` et curseur clignotant (2 clignotements par seconde), décalée vers la gauche quand le curseur dépasse le bord droit. Édition : **retour arrière et suppression en répétition quand la touche est maintenue** (`IsKeyPressedRepeat`), flèches gauche/droite, début/fin. Flèches haut/bas : rappel de l’historique des commandes saisies. **Deux boutons cliquables** (haut, bas) dessinés comme formes géométriques dans une colonne à l’extrémité droite de l’aire de l’historique : un clic fait défiler de 3 lignes, le maintien répète ; un bouton est grisé quand le défilement est impossible dans son sens. Page↑ / Page↓ restent actifs.
- **Police** : `Ui/Fonts.cs`, taille 20 (corps) et 64 (titre du menu), interligne 26, chargée depuis `assets/fonts/DejaVuSans.ttf` avec une liste explicite de codepoints. Repli sur la police par défaut de raylib si le fichier est absent ou invalide.
- **Assets** : `Ui/Assets.cs` résout le dossier `assets` à côté de l’exe, sinon en remontant depuis le répertoire courant (lancement depuis les sources). Ajoute `MenuBackground` et `HelpText`.
- **Palette** : `Ui/Palette.cs`, fond bleu-gris sombre, panneaux, bordures, texte, texte atténué, invite, rectangle de remplacement, couleurs des flèches et des lignes de console.

### 9.4 Touches de défilement

- Page précédente / Page suivante : défilement de l’historique de la console.
- Ctrl + Haut / Ctrl + Bas : défilement de l’inventaire.

Le Ctrl est testé avec `IsKeyDown`, les flèches avec `IsKeyPressed`. Aucun conflit avec la console : la flèche Haut sans Ctrl y rappelle l’historique.

### 9.5 Police et accents (point critique)

Le jeu est en français : ne pas se reposer sur la police par défaut de raylib. Une police TTF libre de droits est chargée depuis `assets/fonts` avec la liste explicite des codepoints :

- ASCII imprimable (32 à 126) ;
- Latin-1 Supplement et Latin Extended-A (0x00A0 à 0x017F) : accents français, `œ Œ`, guillemets `«` `»` ;
- ponctuation générale : 0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2026.

Police retenue : **DejaVu Sans 2.37**, licence incluse dans `assets/fonts/DejaVuSans-LICENSE.txt`. **Validé à l’écran** : é è ê ë à â ç ù û ô î ï œ « » ’ … s’affichent et se saisissent (326 glyphes chargés, aucun manquant). La saisie lit les caractères en Unicode, pas les touches physiques (§9.1).

### 9.6 Audio

Deux bandes son, conformément à la note d'intention §10 (la V0.1 n'en prévoyait qu'une : la contradiction est tranchée en faveur de la note) :

| Piste | Fichiers cherchés | Moment de lecture |
|---|---|---|
| `menu` | `assets/audio/menu.ogg` puis `menu.mp3` | à l'écran du menu |
| `game` | `assets/audio/game.ogg` puis `game.mp3` | pendant la partie |

Chaque flux est chargé si présent, mis en boucle (`Looping = true`) et mis à jour à chaque image (`UpdateMusicStream`). Changer de piste arrête la précédente. **La bascule vers `game.ogg` se fait dès la sortie du menu** (choix « Continuer » ou « Nouvelle partie ») via `MusicPlayer.Play(MusicPlayer.GameTrack)` dans `GameApp.StartNewGame` et `ContinueGame`. Si le périphérique audio est indisponible (`InitAudioDevice` échoue) ou si les fichiers sont absents, le jeu continue **sans son**, sans erreur bloquante. Les flux sont arrêtés et déchargés à la fermeture.

### 9.7 Menu

Au lancement : **trois entrées** — « Continuer » (grisé si aucune sauvegarde valide), « Nouvelle partie », « Aide ». Navigation cyclique au clavier (haut/bas, Entrée). La sélection est placée par défaut sur « Continuer » quand une sauvegarde existe, sinon sur « Nouvelle partie ». Valider une entrée grisée ne fait rien. Retour typé : `Continue`, `NewGame`, `Help`.

- **Fond d'écran** : `assets/menu/menuBackground.png` étiré pour remplir toute la fenêtre, dessiné avant le titre et les entrées. Fichier absent : fond uni (`Palette.Background`), sans erreur.
- **Titre** : « Relève » en grand (police secondaire ~64 px, `Fonts.Title`), centré en haut, avec ombre portée pour la lisibilité.
- « Aide » ouvre le **visualiseur de texte** en plein écran avec le contenu de `assets/texts/help.txt` (Échap ou Entrée pour fermer, retour au menu avec la même sélection).

Implémentation (corrigée pendant le PoC, voir §15) : l'existence et la validité de la sauvegarde sont évaluées **une seule fois au démarrage** (`_canContinue` dans `GameApp`), et `MenuScreen.Reset` n'est appelé qu'à l'entrée dans l'état menu. `GameApp.UpdateMenu` n'appelle plus `StartNewGame` par défaut : « Aide » n'entre pas en jeu, ne change pas la musique et ne modifie pas `_playing`.

Une sauvegarde est également effectuée après chaque commande modifiant l'état (déplacement inclus) — voir §8 et §15.

## 10. Étendre le jeu

Pour ajouter un lieu ou un objet :

1. Ajouter les membres nécessaires aux enums `RoomId` / `ObjectId` / `GameEvent`.
2. Créer la classe de la pièce dans `Content/Rooms/` (dérivée de `RoomBase`) : titre, description, sorties, `OnEnter` éventuel, `Handle` pour les interactions propres au lieu. Textes en constantes en tête de classe.
3. Déclarer les objets (`ObjectDef`) dans `Content/Objects/`, avec alias, emplacement initial, état initial, description. Convention suivie par le contenu livré : un fichier d’objets par pièce (`BedroomObjects`, `TechCorridorObjects`, …), agrégé par `GameWorld`.
4. Enregistrer la pièce et les objets dans `World`. Si c’est la pièce de départ, positionner `StartRoom`.
5. Ajouter ou mettre à jour les tests de cohérence (§11) et, pour tout comportement propre à une pièce, un test de scénario.

Contraintes à respecter, vérifiées par les tests ou par les conventions :

- Les interactions qui concernent deux pièces ne partagent pas d’état « porte » : la condition d’une sortie lit l’état d’un objet de la pièce de départ.
- Les alias ne doivent pas entrer en collision involontaire entre objets présents dans la même pièce (§6.6).
- `ImageKey` doit correspondre à un fichier `assets/rooms/<clé>.png`, sinon le rectangle de remplacement s’affiche : comportement attendu, silencieux, ce n’est pas une erreur.
- Tout texte affiché respecte la convention d’encodage du §2 (caractères précomposés, apostrophe typographique).

## 11. Stratégie de tests (projet `Releve.Engine.Tests`)

Les tests remplacent la validation de schéma qu’offrirait un fichier de données. **83 tests**, tous verts.

| Fichier | Ce qu’il couvre |
|---|---|
| `WorldConsistencyTests.cs` | Cohérence du monde livré et du monde de test : chaque pièce a une classe, chaque objet une définition, chaque destination existe, chaque sortie a son retour sauf `OneWay`, sortie sans destination toujours bloquée avec message obligatoire, pas de collision d’alias dans une même pièce. Vérifie aussi les compteurs du monde livré (4 pièces, 22 objets) et son départ (`Bedroom`). |
| `NormalizationTests.cs` | Accents, articles, apostrophes, ponctuation, prépositions canoniques, alias normalisés comme la saisie. |
| `ParserTests.cs` | Variantes de verbes, directions seules et précédées de « va », Look/Inventory sans complément, ambiguïté (liste des candidats), objet invisible inconnu, verbe inconnu. |
| `EngineBehaviourTests.cs` | Comportements par défaut du moteur (§7.3). |
| `PocContentTests.cs` | Contenu du PoC : sorties sans destination toujours bloquées, retours existants, ambiguïtés du couloir (« lecteur », « porte »), armoire fermée puis badge pris, fiche patient affichée à l’identique, scénario de référence complet du réveil à la rue provisoire, sauvegarde puis rechargement (pièce, inventaire, états, événements ; annonce de réveil non rejouée). |
| `SaveTests.cs` | Aller-retour chiffré à l’identique, clés et noms d’enums du JSON, fichier corrompu ignoré, version de schéma inconnue ignorée. |
| `TestWorld/TestWorld.cs` | Monde de test indépendant du contenu réel : pièces, objets dont un objet invisible (`VisibleWhen`), sortie à sens unique, sortie sans destination. |

Les tests génériques (analyseur, états de sortie, sauvegarde, comportements) s’appuient sur `TestWorld`, pas sur le monde livré. Les tests de contenu (`PocContentTests`, `WorldConsistencyTests`) s’appuient au contraire sur `GameWorld.Create()` : c’est le seul endroit qui valide le monde réel de bout en bout. Ajouter ou retirer un membre de `RoomId` / `ObjectId` / `GameEvent` impose de mettre à jour les listes de test de `WorldConsistencyTests` et le monde de test : les tests échouent bruyamment plutôt que de valider des données.

## 12. Initialisation du projet

### 12.1 Livrables

La structure du §4.1 et tout ce qui est décrit dans ce document sont implémentés : modèle de domaine, analyseur, répartiteur, sauvegarde chiffrée, couche d’affichage raylib complète (disposition, vue du lieu, flèches, inventaire, console, police, audio, menu minimal), tests. Le contenu du PoC est livré (§12.2) et validé dans le jeu réel.

### 12.2 Monde livré

Le monde d’amorçage technique décrit dans les versions précédentes (`SandboxA`, `SandboxB`, `SandboxObjects`, `BootstrapWorld`) a été **supprimé** à l’arrivée du contenu réel, comme prévu. Il est remplacé par le monde du PoC, assemblé dans `Content/GameWorld.cs` :

- `Bedroom` (pièce de départ), `TechCorridor`, `HospitalLobby`, `Street`, reliées Est / Ouest / Nord / Sud selon les sorties décrites au §5.5. Deux sorties sans destination (Est du couloir, Nord de l’accueil) servent de fausses pistes.
- 22 objets, dont deux portables (badge, sac poubelle), deux objets à visibilité conditionnée (vêtements d’enfant et badge, visibles seulement armoire ouverte) et quatre lecteurs verrouillés au départ.
- La rue est **provisoire** : aucune valeur de lore, aucune image.

Le détail du contenu (noms, alias, textes, comportements) appartient au document d’implémentation du PoC, pas à ce document.

### 12.3 Jalons

| Jalon | Contenu | État |
|---|---|---|
| 1 | Projet Engine et tests, avec le monde d’amorçage (§5 à §8, §10, §11), sans interface. | Livré |
| 2 | Fenêtre raylib, disposition, console avec saisie et accents, vue de remplacement. | Livré |
| 3 | Flèches, inventaire, sauvegarde, menu minimal, musique. | Livré |
| 4 | Contenu du PoC : enums, pièces, objets, tests de contenu, suppression du monde d’amorçage, vérification dans le jeu réel. | Livré |

### 12.4 Critères d’acceptation

Vérifiés sur le build livré :

- Build et tests au vert à chaque jalon (83 tests).
- Les accents s’affichent et se saisissent correctement dans la console.
- Les trois états de flèche sont visibles et se mettent à jour après chaque action.
- L’inventaire reflète les objets pris, avec défilement au clavier ; la console défile et conserve l’historique.
- Quitter puis « Continuer » restaure la partie à l’identique.
- Le jeu fonctionne sans fichier audio ni images.
- L’exe publié en autonome démarre sur une machine sans SDK .NET, avec la bibliothèque native raylib à côté de l’exe.

Critères propres au PoC, vérifiés dans le jeu réel (détail dans « CompteRenduPOCV0.1.md ») :

- Le scénario de référence se déroule de bout en bout, commande par commande.
- Les trois états de flèche sont corrects dans les quatre lieux.
- Les textes accentués s’affichent correctement, fiche patient et guillemets compris.
- Le monde d’amorçage a entièrement disparu du code, des objets, des tests et des enums de contenu.

## 13. Consignes de réalisation pour l’agent

1. Travailler par jalons, en gardant build et tests au vert.
2. Ne pas ajouter de fonctionnalité, de projet ni de dépendance non mentionnés.
3. Vérifier chaque appel raylib dans la cheatsheet et les exemples officiels plutôt que de s’appuyer sur la mémoire. Contraintes du binding 8.1.0 relevées pendant l’initialisation : `IsFontValid` / `IsTextureValid` (il n’existe ni `IsFontReady` ni `IsTextureReady`), `char.ConvertFromUtf32` (attention à la casse : « tf32 », pas « TF32 »), `music.Looping` qui est un champ de structure et non une méthode.
4. Signaler explicitement tout écart ou ambiguïté plutôt que choisir silencieusement.
5. À la fin, fournir un court récapitulatif : ce qui fonctionne, ce qui reste approximatif, les points à valider.

## 14. Points à valider

1. **Résolution cible** : 1280×720 retenue et implémentée (`Layout.cs`). Images de lieux : trois sur quatre fournies (`Bedroom`, `HospitalLobby`, `TechCorridor`) ; la rue provisoire est sans image (rectangle de remplacement, comportement prévu §9.2). Format définitif à préciser avec les lieux suivants.
2. **Police TTF** : DejaVu Sans 2.37 retenue, licence incluse.
3. **Touches de défilement** : Page↑/Page↓ pour la console, Ctrl+↑/↓ pour l’inventaire.
4. **Dossier de sauvegarde** : `Documents/Releve/save.dat` retenu. Fréquence : **à la fermeture seulement**. Le dossier de données de l’application reste plus conventionnel et est le point à revoir en priorité, notamment pour Linux/macOS.
5. **Messages neutres d’erreur** : implémentés tels quels (§6.7 et §7.3).
6. **Bibliothèque native raylib** : vérifiée présente à côté de l’exe après `dotnet publish`.
7. **Page d’aide pleine page** : prévue par la note d’intention, hors initialisation, non implémentée.

## 15. Écarts connus et points ouverts

Défauts recensés dans le code, corrections attendues et écarts acceptés.

1. **Navigation du menu neutralisée — corrigé.** `GameApp.Run` appelait `MenuScreen.Reset(...)` à chaque image, ce qui ramenait la sélection sur sa valeur par défaut et neutralisait les flèches Haut/Bas. `Reset` n'est plus appelé qu'à l'entrée dans l'état menu (drapeau `_menuEntered`).
2. **Fichier de sauvegarde relu en permanence — corrigé.** `CanContinue()` était évalué plusieurs fois par image, avec lecture et déchiffrement complets du fichier à chaque fois. Le résultat est mémorisé au démarrage (`_canContinue`) et rafraîchi à la sortie d'une partie.
3. **Images de lieux non référencées — résolu.** Les trois images de `assets/rooms/` sont désormais des images vivantes du monde du PoC (clés `Bedroom`, `HospitalLobby`, `TechCorridor`). Seule la rue est sans image, par conception (lieu provisoire).
4. **Page d'aide** — implémentée dans le PoC V2 (lot C4) : entrée « Aide » du menu ouvre le visualiseur de texte en plein écran avec `assets/texts/help.txt`.
5. **Rendu des flèches et icônes d'inventaire** : formes et carrés de remplacement, en attente des assets définitifs. Ordre de sommets des triangles corrigé (anti-horaire) dans le PoC V2 (lot B3).
6. **Résolution partielle des objets invisibles — écart connu, accepté (décision A1 PoC V2).** Un objet caché par `VisibleWhen` reste partiellement identifiable par ses alias (§6.6) : « prends badge » avant « ouvre armoire » refuse la prise au lieu de signaler un objet inconnu. Comportement documenté par un test (`Badge_Is_Reachable_Only_After_Opening_The_Wardrobe`). Aucune correction moteur : cela ne révèle rien sur les objets cachés.
7. **Touche Retour arrière** — corrigée (lot B1) : `KeyboardKey.Backspace` au lieu de `KeyboardKey.Back`, avec répétition en maintien (`IsKeyPressedRepeat`). La validation de la répétition avec un vrai clavier n'est pas automatisable par `agentTools/` : classification « validation humaine » (§7.3 du document PoC V2).
8. **Sauvegarde automatique** (lot C1) : sauvegarde après chaque commande à `StateChanged` (déplacement inclus), plus sauvegarde de fermeture dans un bloc `finally`.

**Points PoC V2 supplémentaires :**

9. **Boutons de défilement console** : un clic = 3 lignes, **pas de répétition en maintien** (supprimée pour éviter le défilement trop rapide). Les boutons sont grisés quand le défilement est impossible.
10. **Flèches déplacements** : **têtes de flèches seules** (triangles pleins/creux), **plus de fûts** (rectangles). Ordre de sommets anti-horaire validé.
11. **Visualiseur de texte (TextViewer)** : normalisation CRLF → LF avant wrapping (plus de `?` en fin de ligne) ; indicateur de défilement bas repositionné au-dessus du bouton « Fermer » pour ne pas chevaucher.
12. **Bascule musique menu/jeu** : correction d'une régression — `game.ogg` est maintenant joué en quittant le menu (ajout de `MusicPlayer.Play(GameTrack)` dans `StartNewGame` et `ContinueGame`).

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
5. **La capture `PrintWindow` sert de référence** pour les mesures pixeliques (client 1280×720, décalage de +8 en x et +39 en y par la bordure). Les zones de `Get-ReleveArrowStates` sont déduites des constantes de `Ui/ArrowsPanel.cs` et `Ui/Layout.cs` ; elles ont été recalibrées pour la nouvelle disposition en croix (lot B3) et doivent être recalibrées si la mise en page du panneau change.
6. **La répétition de Retour arrière et Suppr** est impliquée dans le moteur (`IsKeyPressedRepeat`) mais **ne peut être validée par `agentTools/` comme par un vrai clavier** : voir §7.3 du document PoC V2 (classification « validation humaine »).

## Références

- Raylib-cs sur NuGet : <https://www.nuget.org/packages/Raylib-cs/8.1.0>
- Cheatsheet raylib (liste des fonctions) : <https://www.raylib.com/cheatsheet/cheatsheet.html>
- Exemples officiels raylib : <https://github.com/raysan5/raylib/tree/master/examples>
- Politique de support .NET : <https://dotnet.microsoft.com/platform/support/policy/dotnet-core>
