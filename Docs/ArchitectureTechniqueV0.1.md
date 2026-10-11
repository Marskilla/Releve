# Jeu "Relève" — Architecture technique

> Document de référence — version 0.1
> Valable pour toute la durée du projet. Il décrit la structure du code, pas le contenu du jeu.
> À fournir à l'agent de développement avec la note d'intention (V5) et le mockup bitmap de l'interface. Le document univers/scénario n'est pas nécessaire à ce stade.

## 1. But et portée

Ce document définit l'architecture technique du jeu d'aventure textuel et graphique « Relève » : plateforme, découpage du code, modèle de domaine, analyseur de commandes, exécution, sauvegarde et couche d'affichage. Il sert à **initialiser le projet** (§12) puis de référence pour tout développement ultérieur.

Il ne contient pas le contenu du jeu (lieux, objets, textes). Celui-ci est décrit dans des documents séparés qui s'appuient sur ce document.

## 2. Principes directeurs

- **Petit développement « à l'ancienne »** : le projet est un seul jeu, pas un moteur multi-jeux.
- **Contenu en classes C#** : lieux, objets, textes, alias, sorties sont écrits dans le code. Aucun fichier JSON/YAML de paramétrage du contenu. (Le JSON n'est utilisé que pour la sauvegarde.)
- **Moteur isolé** : le projet du moteur ne dépend ni de raylib ni d'aucune bibliothèque d'affichage ou de son.
- **Pas de MVVM ni de framework UI** : l'interface est un rendu raylib qui lit l'état du moteur à chaque image.
- **Pas de sur-élaboration** : ne rien ajouter qui ne soit pas décrit dans les documents fournis.
- **En cas de contradiction ou d'ambiguïté** entre documents : signaler le point à l'utilisateur au lieu de trancher seul. La note d'intention fait référence pour l'interface et le comportement du jeu.

### Conventions

- Identifiants de code en anglais, textes affichés au joueur en français, commentaires brefs en français.
- Textes joueur : jamais de pensées, souvenirs ni émotions attribués au joueur ; formulations non genrées (pas de participe passé accordé au joueur).
- Dans chaque classe de pièce, les textes sont des constantes regroupées en tête de classe, séparées de la logique.
- Une sortie impossible est expliquée par un message qui décrit ce qui bloque, **sans dire comment débloquer** (note d'intention §2).

## 3. Choix techniques

| Sujet | Décision |
|---|---|
| Plateforme | .NET 8, C# 12, `Nullable` activé. Fin de support de .NET 8 prévue le 10/11/2026, migration non planifiée (choix assumé). |
| Affichage | Bibliothèque **Raylib-cs** (NuGet `Raylib-cs`, version à fixer, 8.1.0 à la date de rédaction), fenêtre fixe. Aucune autre bibliothèque d'affichage. |
| Cible | Windows x64, publication autonome (runtime copié à côté de l'exe). |
| Portabilité | L'architecture doit permettre Linux/macOS sans refonte du moteur. Aucun build Linux/macOS n'est livré à ce stade. |
| Éditeur | VS Code et ligne de commande `dotnet`. Aucun concepteur visuel requis. |
| Tests | xUnit sur le moteur. |
| Sauvegarde | JSON chiffré (AES-GCM), un seul fichier, dossier Documents de l'utilisateur. |

Commandes de référence :

```text
dotnet build
dotnet test
dotnet run --project src/Releve.Game
dotnet publish src/Releve.Game -c Release -r win-x64 --self-contained
```

Les dossiers d'assets sont copiés dans le répertoire de sortie (`CopyToOutputDirectory`). **Vérifier que la bibliothèque native raylib se retrouve bien à côté de l'exe après publication.**

## 4. Architecture générale

### 4.1 Projets

```text
Releve/
  Directory.Build.props        (TargetFramework net8.0, Nullable, ImplicitUsings, LangVersion)
  Directory.Packages.props     (versions des paquets centralisées)
  Releve.sln
  src/
    Releve.Engine/             bibliothèque de classes, AUCUNE dépendance à raylib
      Core/                    GameAction, Direction, ObjectState, Command, GameState, Location, ObjectDef, Exit, RoomView...
      Parsing/                 TextNormalizer, Vocabulary, CommandParser, ParseResult
      World/                   World, RoomBase, Ids (RoomId, ObjectId, GameEvent)
      Content/                 Rooms/ et Objects/ : le contenu du jeu (voir §10)
      Persistence/             ISaveStore, SaveData, SaveSerializer, EncryptedFileSaveStore
      GameEngine.cs
    Releve.Game/               exécutable, dépend de Releve.Engine et de Raylib-cs
      Program.cs, GameApp.cs
      Ui/                      Layout, RoomViewPanel, ArrowsPanel, InventoryPanel, ConsolePanel, Fonts
      Audio/                   MusicPlayer
      assets/                  rooms/, fonts/, audio/
  tests/
    Releve.Engine.Tests/
```

### 4.2 Règle de dépendance

`Releve.Game` → `Releve.Engine`, jamais l'inverse. Le moteur ne contient ni dessin, ni lecture de touches, ni son. Il reçoit du texte, renvoie du texte et un état à afficher.

### 4.3 Cycle d'une commande

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

// Enums dont les membres sont ajoutés par les documents de contenu :
public enum RoomId { /* ... */ }
public enum ObjectId { /* ... */ }
public enum GameEvent { /* événements uniques, ex. annonce déjà jouée */ }
```

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

Un objet n'appartient pas à une pièce : il a un **emplacement**, qui change au fil du jeu (un objet pris passe dans l'inventaire).

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

Chaque sortie appartient à la pièce de **départ** : l'aller et le retour sont deux sorties distinctes. Il n'y a pas d'objet « porte » partagé entre deux pièces. Une sortie peut être conditionnée par **l'état d'un objet de la pièce** (lecteur de badge, serrure).

```csharp
public sealed record Exit(
    Direction Dir,
    RoomId? To,                             // null = sortie sans destination (fausse piste) : toujours bloquée
    Func<GameState, bool>? IsOpen = null,   // null = toujours ouverte (ignoré si To est null)
    string? BlockedMessage = null,
    bool OneWay = false);                   // retour volontairement absent côté destination
```

Les trois états d'affichage sont **calculés**, jamais stockés : pas de sortie dans la direction → `Absent` ; sortie sans destination, ou condition fausse → `Blocked` ; sinon `Open`.

### 5.6 Pièces

Une classe par pièce, **sans état propre** : toutes les méthodes reçoivent l'état en paramètre. Les comportements spécifiques (par exemple « passer le badge sur le lecteur ») vivent dans `Handle` de la pièce où se trouve la cible.

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

public sealed class World   // point de construction unique : assemble toutes les pièces et tous les objets
{
    public RoomId StartRoom { get; }
    public IReadOnlyDictionary<RoomId, RoomBase> Rooms { get; }
    public IReadOnlyDictionary<ObjectId, ObjectDef> Objects { get; }
    public GameState CreateInitialState();   // CurrentRoom = StartRoom ; emplacement et état initiaux de chaque objet
}

public sealed class GameEngine
{
    public GameEngine(World world, GameState state);
    public GameState State { get; }
    public IReadOnlyList<string> Start();    // à appeler au démarrage : description + OnEnter de la pièce courante
    public CommandResult Execute(string input);
    public RoomView GetView();
}
```

## 6. Analyseur de commandes

### 6.1 Principe

Vocabulaire contrôlé, deux étages : (1) **normalisation et reconnaissance** du texte, (2) **résolution des noms** dans la portée courante. L'analyseur ne cherche pas à comprendre le langage naturel.

### 6.2 Normalisation (identique pour la saisie et pour les alias)

```text
Normaliser(texte):
  1. minuscules (culture invariante)
  2. suppression des accents (décomposition Unicode FormD) ; œ -> oe, æ -> ae traités à part
  3. apostrophes (' et ’) remplacées par un espace  ("l'armoire" -> "l armoire")
  4. ponctuation supprimée, espaces multiples réduits
  5. découpage en mots
  6. suppression des articles : le la les l un une des du de d
  7. prépositions ramenées à une forme canonique : sur, avec, a (à, au, aux), dans, vers, contre
```

Les alias des objets sont normalisés par la même fonction au chargement : « lecteur de badge » devient « lecteur badge ». « clé » et « clef » sont deux alias du même objet.

### 6.3 Verbes reconnus

Le verbe est le premier mot après normalisation. Cette table est celle du moteur ; elle s'étend au fil du projet.

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

La commande « quitter » est interceptée par la couche Game avant d'appeler le moteur.

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

  si mots est une direction (ou "va [vers] direction") -> Succès(Go, direction)

  verbe = TableVerbes[mots[0]]  ; si absent -> Erreur(UnknownVerb)
  reste = mots[1..]

  si reste vide:
      si verbe in {Look, Inventory} -> Succès(verbe)
      sinon -> Erreur(MissingObject)       // "Prendre quoi ?"

  si reste[0] est une préposition -> la retirer
  groupe1 = mots de reste jusqu'à la prochaine préposition
  groupe2 = mots après cette préposition (peut être vide)

  cible1 = Résoudre(groupe1, scope)        // peut échouer : UnknownObject ou Ambiguous
  si groupe2 non vide:
      cible2 = Résoudre(groupe2, scope)
      -> Succès(verbe, Instrument = cible1, Target = cible2)
  sinon:
      -> Succès(verbe, Target = cible1)

Résoudre(groupe, scope):
  candidats = objets de scope dont un alias est EXACTEMENT égal au groupe
  si aucun: candidats = objets de scope dont un alias CONTIENT tous les mots du groupe
  si 0 candidat  -> Erreur(UnknownObject)
  si 1 candidat  -> cet objet
  si > 1         -> Erreur(Ambiguous, noms des candidats)
```

### 6.6 Portée

Les objets accessibles pour une commande sont ceux dont l'emplacement est la pièce courante ou l'inventaire, **et** dont `VisibleWhen` est nul ou vrai. Un objet que le joueur ne peut pas voir est donc inconnu de l'analyseur.

### 6.7 Erreurs et messages

| Erreur | Message (neutre) |
|---|---|
| Empty | (rien : la saisie vide est ignorée) |
| UnknownVerb | « Cette commande n'est pas comprise. » |
| MissingObject | « Précisez l'objet de la commande. » |
| UnknownObject | « Vous ne voyez pas cela ici. » |
| Ambiguous | « Précisez : » suivi des noms des candidats (« le lecteur de l'escalier ou le lecteur du local technique »). |

La correction automatique de fautes de frappe n'est pas prévue. Les formes courantes sont couvertes par la table de verbes et les alias.

## 7. Exécution des commandes

### 7.1 Répartiteur

```text
Execute(input):
  r = Parse(input, scope courant)
  si erreur -> message correspondant (§6.7)

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
  si To est null ou IsOpen faux   -> sortie.BlockedMessage
  sinon                           -> état.CurrentRoom = sortie.To
                                     return Description de la nouvelle pièce + OnEnter(état)
```

Une tentative vers une sortie `Blocked` n'est pas exécutée et renvoie le message explicatif.

### 7.3 Comportements par défaut

| Action | Comportement |
|---|---|
| Look sans cible | Réaffiche la description de la pièce. |
| Look avec cible | Affiche `Describe(état)` de l'objet. |
| Take | Si l'objet est portable et présent dans la pièce : il passe dans l'inventaire (« Pris. »). Sinon : « Vous ne pouvez pas prendre cela. » |
| Drop | Si l'objet est dans l'inventaire : il passe dans la pièce courante (« Posé. »). |
| Open / Close | Si l'objet a un état `Open`/`Closed`, le change (« Vous ouvrez … » / « Vous fermez … ») ; sinon « Rien ne se passe. » |
| Use / Talk | « Rien ne se passe. » |
| Inventory | Liste les noms des objets de l'inventaire, ou « Vous ne portez rien. » |

### 7.4 Entrée dans une pièce

`Start()` et chaque déplacement renvoient la description de la pièce, puis les messages de `OnEnter`. Les événements uniques (annonce, scène) utilisent `GameState.Events` pour ne se produire qu'une fois. Au chargement d'une sauvegarde, `Start()` réaffiche la description de la pièce sans rejouer les événements déjà notés.

## 8. Sauvegarde

- **Déclenchement** : automatique à la fermeture de la fenêtre, et après chaque changement de pièce (protège contre un arrêt brutal). Une seule sauvegarde.
- **Contenu** : pièce courante, emplacements des objets, états des objets, événements. **Aucune définition** (textes, images, sorties) n'est sauvegardée.
- **Version de schéma** : champ `SchemaVersion` (entier). Si la version est inconnue ou le fichier illisible, la sauvegarde est ignorée proprement (« Continuer » grisé).
- **Format avant chiffrement** (enums écrits par nom, `System.Text.Json` avec `JsonStringEnumConverter`) :

```json
{
  "schemaVersion": 1,
  "currentRoom": "ExampleRoom",
  "locations": { "ExampleItem": { "kind": "Inventory" } },
  "states": { "ExampleLock": "Unlocked" },
  "events": [ "ExampleEvent" ]
}
```

- **Chiffrement** : AES-256-GCM (`System.Security.Cryptography.AesGcm`), nonce aléatoire de 12 octets et tag de 16 octets stockés avec le contenu. La clé est dérivée d'un secret constant embarqué dans l'application. Il s'agit d'une **obfuscation contre l'édition manuelle**, pas d'une protection. Disponibilité à vérifier sur Linux/macOS.
- **Écriture atomique** : écriture dans un fichier temporaire, puis remplacement du fichier cible.
- **Emplacement** : dossier Documents de l'utilisateur (`Environment.SpecialFolder.MyDocuments`), sous-dossier `Releve`, fichier `save.dat`. Choix à revoir pour Linux/macOS.

```csharp
public interface ISaveStore
{
    bool Exists();
    GameState? Load();                 // null si absent, illisible ou version inconnue
    void Save(GameState state);
}
```

## 9. Couche d'affichage (projet Game, raylib)

### 9.1 Principe

Une boucle simple : lire les entrées, mettre à jour, dessiner, sans liaison de données. Les noms précis des fonctions raylib sont à **vérifier dans la cheatsheet et les exemples officiels** (voir Références), pas à deviner.

```text
Initialisation : fenêtre, police, audio (si disponible), menu
Nouvelle partie ou chargement -> engine.Start() -> messages ajoutés à la console

Tant que la fenêtre n'est pas fermée:
    entrées:
        caractères saisis (Unicode) -> ligne de saisie
        Entrée -> console.Echo("> " + ligne) ; r = engine.Execute(ligne) ; console.Append(r.Messages) ; view = engine.GetView()
        (autres touches : voir §9.4)
    musique: mise à jour du flux à chaque image
    dessin: fond ; vue du lieu ; flèches ; inventaire ; console

À la fermeture : sauvegarde
```

### 9.2 Disposition (valeurs initiales, résolution 1280×720)

D'après les proportions de la note d'intention et du mockup bitmap :

| Zone | Position / taille |
|---|---|
| Zone haute | hauteur 480 (≈ 2/3) |
| Vue du lieu | 960 × 480 (≈ 3/4 de la largeur), image mise à l'échelle en conservant ses proportions, centrée |
| Colonne latérale | 320 de large |
| Déplacements | haut de la colonne, hauteur minimale (≈ 120) |
| Inventaire | reste de la colonne |
| Console | bas de l'écran, 1280 × 240 (≈ 1/3), historique + ligne de saisie |

Si l'image d'un lieu est introuvable, afficher un **rectangle de remplacement** portant le titre de la pièce : le jeu doit fonctionner sans aucune illustration.

### 9.3 Composants

- **Vue du lieu** : texture chargée depuis `assets/rooms/<ImageKey>.png`, mise en cache.
- **Flèches** : croix de quatre flèches (nord en haut, ouest à gauche, est à droite, sud en bas), dessinées avec des formes simples. `Absent` : rien ; `Blocked` : grisée ; `Open` : couleur active. Ce sont des **indicateurs**, pas des boutons : aucune interaction à la souris.
- **Inventaire** : liste verticale de libellés (icône de remplacement facultative), défilement au clavier.
- **Console** : historique avec retour à la ligne automatique (largeur mesurée avec la police), ancré en bas, défilement vertical. Ligne de saisie avec invite `> ` et curseur clignotant. Édition : retour arrière, suppression, flèches gauche/droite, début/fin. Flèches haut/bas : rappel de l'historique des commandes saisies.

### 9.4 Touches de défilement

- Page précédente / Page suivante : défilement de l'historique de la console.
- Ctrl + Haut / Ctrl + Bas : défilement de l'inventaire.

### 9.5 Police et accents (point critique)

Le jeu est en français : ne pas se reposer sur la police par défaut de raylib. Charger une **police TTF libre de droits** (à fournir dans `assets/fonts`) avec la liste explicite des codepoints nécessaires : ASCII, caractères accentués français, « œ Œ », guillemets « » ’ ", points de suspension, tiret cadratin. **Valider à l'écran** l'affichage de : é è ê ë à â ç ù û ô î ï œ « » ’ …

La saisie doit accepter ces caractères (lecture des caractères saisis en Unicode, pas des seules touches physiques).

### 9.6 Audio

Initialiser le périphérique audio, charger `assets/audio/game.ogg` (ou `.mp3`) comme flux musical, activer la lecture en boucle et mettre à jour le flux à chaque image. Si le fichier est absent ou si l'initialisation échoue, continuer **sans son**, sans erreur bloquante.

### 9.7 Menu

Au lancement : « Continuer » (grisé si aucune sauvegarde valide) et « Nouvelle partie ». Navigation au clavier (haut/bas, Entrée). Une page d'aide pleine page défilante est prévue par la note d'intention mais n'entre pas dans l'initialisation.

## 10. Étendre le jeu

Pour ajouter un lieu ou un objet :

1. Ajouter les membres nécessaires aux enums `RoomId` / `ObjectId` / `GameEvent`.
2. Créer la classe de la pièce dans `Content/Rooms/` (dérivée de `RoomBase`) : titre, description, sorties, `OnEnter` éventuel, `Handle` pour les interactions propres au lieu. Textes en constantes en tête de classe.
3. Déclarer les objets (`ObjectDef`) dans `Content/Objects/`, avec alias, emplacement initial, état initial, description.
4. Enregistrer la pièce et les objets dans `World`. Si c'est la pièce de départ, positionner `StartRoom`.
5. Ajouter ou mettre à jour les tests de cohérence (§11) et, pour tout comportement propre à une pièce, un test de scénario.

Les interactions qui concernent deux pièces ne partagent pas d'état « porte » : la condition d'une sortie lit l'état d'un objet de la pièce de départ.

## 11. Stratégie de tests (projet `Releve.Engine.Tests`)

Les tests remplacent la validation de schéma qu'offrirait un fichier de données.

- **Cohérence du monde** : chaque `RoomId` a une classe de pièce ; chaque `ObjectId` a une définition ; chaque destination de sortie existe ; chaque sortie avec destination a son retour dans la pièce d'arrivée, sauf si `OneWay` ; les sorties sans destination sont exclues de cette vérification ; les alias d'une même pièce n'entrent pas en collision involontaire.
- **Normalisation et analyse** : variantes d'une même commande (« Prends clé », « prendre clef », « ramasse clé », « prennez clef »), articles, apostrophes, deux compléments, préposition seule, objet inconnu, objet invisible, ambiguïté.
- **États de sortie** : calcul `Absent` / `Blocked` / `Open` avant et après l'événement qui change la condition.
- **Sauvegarde** : aller-retour sérialisation + chiffrement identique ; version inconnue ou fichier corrompu ignoré.
- **Monde de test** : les tests génériques (analyseur, états de sortie, sauvegarde) s'appuient sur un petit monde défini dans le projet de tests, indépendant du contenu réel du jeu.

## 12. Initialisation du projet

### 12.1 Livrables

L'agent crée la structure du §4.1 et implémente tout ce qui est décrit dans ce document : modèle de domaine, analyseur, répartiteur, sauvegarde chiffrée, couche d'affichage raylib complète (disposition, vue du lieu, flèches, inventaire, console, police, audio, menu minimal), tests.

### 12.2 Monde d'amorçage

Le jeu doit pouvoir démarrer et être validé dès l'initialisation, sans contenu réel. L'agent crée donc un **monde d'amorçage technique** dans `Content/`, destiné à être supprimé lorsque le contenu réel arrivera :

- `SandboxA` (pièce de départ) et `SandboxB`, reliées par Est/Ouest.
- Dans `SandboxA` : un objet portable `SandboxItem`, un objet fixe `SandboxSwitch` (état initial `Locked`). Sortie **Est** vers `SandboxB`, ouverte seulement si `SandboxSwitch` est `Unlocked` (un « utilise `SandboxItem` sur `SandboxSwitch` » le déverrouille). Sortie **Nord** sans destination (jamais ouverte). Sortie Sud/Ouest : absentes.
- `SandboxB` : sortie Ouest vers `SandboxA`.

Ce monde permet de voir les trois états de flèche, de tester la prise d'objet, l'inventaire, l'analyseur et la sauvegarde.

### 12.3 Jalons

1. **Jalon 1** : projet Engine et tests, avec le monde d'amorçage (§5 à §8, §10, §11), sans interface.
2. **Jalon 2** : fenêtre raylib, disposition, console avec saisie et accents, vue de remplacement.
3. **Jalon 3** : flèches, inventaire, sauvegarde, menu minimal, musique.

### 12.4 Critères d'acceptation

- Build et tests au vert à chaque jalon.
- Les accents s'affichent et se saisissent correctement dans la console.
- Les trois états de flèche sont visibles et se mettent à jour après chaque action.
- L'inventaire reflète les objets pris, avec défilement au clavier ; la console défile et conserve l'historique.
- Quitter puis « Continuer » restaure la partie à l'identique.
- Le jeu fonctionne sans fichier audio ni images.
- L'exe publié en autonome démarre sur une machine sans SDK .NET, avec la bibliothèque native raylib à côté de l'exe.

## 13. Consignes de réalisation pour l'agent

1. Travailler par jalons, en gardant build et tests au vert.
2. Ne pas ajouter de fonctionnalité, de projet ni de dépendance non mentionnés.
3. Vérifier chaque appel raylib dans la cheatsheet et les exemples officiels plutôt que de s'appuyer sur la mémoire.
4. Signaler explicitement tout écart ou ambiguïté plutôt que choisir silencieusement.
5. À la fin, fournir un court récapitulatif : ce qui fonctionne, ce qui reste approximatif, les points à valider.

## 14. Points à valider

1. Résolution cible (1280×720 proposé) et format des images de lieux.
2. Choix de la police TTF.
3. Touches de défilement (§9.4).
4. Dossier de sauvegarde (Documents proposé ; le dossier de données de l'application est plus conventionnel) et fréquence.
5. Messages neutres d'erreur de l'analyseur (§6.7).
6. Présence de la bibliothèque native raylib dans le dossier de publication.

## Références

- Raylib-cs sur NuGet : <https://www.nuget.org/packages/Raylib-cs/8.1.0>
- Cheatsheet raylib (liste des fonctions) : <https://www.raylib.com/cheatsheet/cheatsheet.html>
- Exemples officiels raylib : <https://github.com/raysan5/raylib/tree/master/examples>
- Politique de support .NET : <https://dotnet.microsoft.com/platform/support/policy/dotnet-core>