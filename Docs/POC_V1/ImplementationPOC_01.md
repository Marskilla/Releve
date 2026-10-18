# Jeu "Relève" — Implémentation du PoC

> Document d'implémentation — version 0.1
> Valable pour le PoC (et, éventuellement, comme base de la suite du jeu).
> **Prérequis** : l'architecture technique est en place (document « Architecture technique », jalons 1 à 3 terminés). Ce document n'en redéfinit rien : il décrit uniquement le **contenu** à ajouter et la façon de le valider.
> À fournir à l'agent avec : l'architecture technique, la note d'intention (V5) et le document univers/scénario (V0.7).

## 1. But et périmètre

### 1.1 But

Implémenter, sur l'architecture existante, un contenu de jeu minimal et jouable de bout en bout : les trois premiers lieux du lore (chambre de réveil, couloir technique, accueil de l'hôpital) et une rue **provisoire**. Le PoC doit démontrer que le contenu réel fonctionne sur le moteur et l'interface.

### 1.2 À faire

- Ajouter les membres d'enums, les classes de pièces et les objets décrits au §3, dans `Content/`, selon la procédure du §10 de l'architecture.
- **Remplacer le monde d'amorçage** (`SandboxA`, `SandboxB`, leurs objets et leurs tests dépendants) : le supprimer et positionner `StartRoom` sur la chambre de réveil.
- Écrire les tests propres à ce contenu (§4).
- Fournir les images de remplacement nécessaires si elles n'existent pas (aucune illustration n'est requise).

### 1.3 Hors périmètre

Lieux au-delà de la rue provisoire, scène de fin, page d'aide, seconde bande son, tout changement d'architecture. Si le contenu semble exiger une évolution du moteur, **le signaler** plutôt que modifier l'architecture.

### 1.4 Sources de vérité

Le document univers/scénario (§5.1 à §5.3) pour le lieu et le lore, la note d'intention pour le comportement. Les textes de ce document sont **provisoires**, à l'exception de ceux issus du lore : la voix de réveil, la date et la fiche patient. Ils respectent les conventions de rédaction de l'architecture (§2).

Choix propres au PoC, confirmés : l'armoire est fermée au départ ; le couloir contient volontairement deux lecteurs de badge (test d'ambiguïté).

## 2. Identifiants à ajouter

```csharp
public enum RoomId { Bedroom, TechCorridor, HospitalLobby, Street }

public enum ObjectId
{
    // Chambre de réveil
    BedroomBed, BedroomMachines, BedroomWardrobe, ChildClothes, Badge,
    BedroomReader, BedroomDoor, BedroomBin, GarbageBag,
    // Couloir technique
    StairsDoor, StairsReader, TechDoor, TechReader,
    // Accueil de l'hôpital
    Android, Terminal, Display, Wheelchairs, GlassDoor, Plants,
    Elevator, ElevatorReader, ElevatorButton
}

public enum GameEvent { WakeAnnouncementDone }
```

## 3. Contenu

Dans les tableaux, « emplacement » désigne l'emplacement initial et « état » l'état initial. Sauf mention contraire, l'état est `None` et l'objet n'est pas portable.

### 3.1 Chambre de réveil (`Bedroom`) — pièce de départ

- **Titre** : Chambre de réveil. **Clé d'image** : `Bedroom`.
- **OnEnter** (une seule fois, grâce à l'événement `WakeAnnouncementDone`) : « Une voix de synthèse annonce : « Réveil terminé, constantes stables. » »
- **Description** : « Vous portez une blouse médicale. La pièce est propre, éclairée, sans fenêtre. »
- **Sortie** : **Est** → `TechCorridor`, ouverte si `BedroomReader` est `Unlocked`. Message si bloquée : « La porte est solidement fermée et le lecteur de badge affiche un rouge fixe. » Aucune autre sortie.

| ObjectId | Nom | Alias | Portable | État | Emplacement | Description / comportement |
|---|---|---|---|---|---|---|
| BedroomBed | le lit | lit | non | None | Bedroom | « Un lit médicalisé. » |
| BedroomMachines | les machines | machine, machines, moniteur, appareil, appareils | non | None | Bedroom | « Dossier partiel affiché : maladie inconnue évolutive, traitement adaptatif expérimental. Statut : réveil terminé, constantes stables. Le reste du dossier est indisponible. » |
| BedroomWardrobe | l'armoire | armoire, placard | non | **Closed** | Bedroom | Fermée : « Une armoire en contreplaqué, de modèle hospitalier standard. » Ouverte : « L'armoire est ouverte : elle contient des vêtements et un badge. » Ouvrir (comportement par défaut) : état → `Open`, « Vous ouvrez l'armoire. » |
| ChildClothes | les vêtements | vetements, vetement, habits | non | None | Bedroom, visible si l'armoire est `Open` | « Des vêtements d'enfant, bien trop petits pour vous. » |
| Badge | le badge | badge, carte, badge identification | **oui** | None | Bedroom, visible si l'armoire est `Open` | « Un badge d'identification. La photo est celle d'un enfant d'environ huit ans. » |
| BedroomReader | le lecteur de badge | lecteur, lecteur badge | non | **Locked** | Bedroom | `Locked` : « Un lecteur de badge est fixé près de la porte. Son voyant est rouge. » `Unlocked` : « … Son voyant est vert. » |
| BedroomDoor | la porte | porte | non | None | Bedroom | « Une porte pleine, équipée d'un lecteur de badge. » Ouvrir tant que le lecteur est `Locked` : même message que la sortie bloquée. |
| BedroomBin | la corbeille | corbeille, poubelle | non | None | Bedroom | « Une corbeille. Elle ne contient qu'un sac poubelle vide. » |
| GarbageBag | le sac poubelle | sac, sac poubelle | **oui** | None | Bedroom | « Un sac poubelle, vide. » |

**Comportement spécifique (`Handle`)** : `Use(Instrument = Badge, Target = BedroomReader)` — `BedroomReader` passe à `Unlocked` ; message : « Vous passez le badge devant le lecteur. Le voyant passe au vert et la porte se déverrouille. » Répété : « Le lecteur est déjà déverrouillé. »

### 3.2 Couloir technique (`TechCorridor`)

- **Titre** : Couloir technique. **Clé d'image** : `TechCorridor`.
- **Description** : « Un couloir de service étroit, sans fenêtre. Quelques plafonniers sont faibles ou éteints ; une poussière légère recouvre le sol. »
- **Sorties** :
  - **Ouest** → `Bedroom`, toujours ouverte.
  - **Nord** → `HospitalLobby` (par l'escalier), ouverte si `StairsReader` est `Unlocked`. Message si bloquée : « La porte de l'escalier est fermée. Le lecteur de badge affiche un rouge fixe. »
  - **Est** : **sortie sans destination** (fausse piste, toujours bloquée). Message : « La porte du local technique est fermée. Le lecteur de badge affiche un rouge fixe. »

| ObjectId | Nom | Alias | État | Description / comportement |
|---|---|---|---|---|
| StairsDoor | la porte de l'escalier | porte escalier, porte fond, escalier | None | « Une porte pleine percée d'un grand vitrage en verre armé. On distingue les premières marches, qui montent vers une lumière plus claire. » |
| StairsReader | le lecteur de l'escalier | lecteur escalier, lecteur fond | **Locked** | `Locked` : « Le voyant du lecteur est rouge. » `Unlocked` : « Le voyant du lecteur est vert. » |
| TechDoor | la porte du local technique | porte local, porte technique, hublot | None | « Une porte pleine à hublot. À travers, on distingue les éléments d'une chaufferie, une rangée de lave-linge à l'arrêt depuis longtemps et des compteurs électriques. » |
| TechReader | le lecteur du local technique | lecteur local, lecteur technique | **Locked** | « Le voyant du lecteur est rouge. » |

Tous ces objets sont fixes (non portables) et situés dans `TechCorridor`.

**Comportements spécifiques (`Handle`)** :
- `Use(Badge, StairsReader)` → `Unlocked` ; « Le voyant passe au vert : la porte de l'escalier se déverrouille. »
- `Use(Badge, TechReader)` → état inchangé ; « Le lecteur refuse le badge. Son voyant clignote en rouge. »

**Ambiguïté volontaire** : dans ce lieu, « porte » et « lecteur » (sans précision) correspondent chacun à deux objets et doivent produire l'erreur d'ambiguïté de l'analyseur.

### 3.3 Accueil de l'hôpital (`HospitalLobby`)

- **Titre** : Accueil de l'hôpital. **Clé d'image** : `HospitalLobby`.
- **Description** : « Le hall d'accueil est vaste, lumineux et parfaitement entretenu. Derrière le comptoir, un androïde se tient immobile. »
- **Sorties** :
  - **Sud** → `TechCorridor`, toujours ouverte.
  - **Ouest** → `Street`, ouverte si `Terminal` est `Activated`. Message si bloquée : « La porte vitrée de l'entrée reste fermée. »
  - **Nord** : **sortie sans destination** (ascenseur, fausse piste). Message : « Les portes de l'ascenseur restent closes. Le lecteur de badge affiche un rouge fixe. »
  - **Est** : aucune sortie.
- Aucun objet de ce lieu n'est portable.

| ObjectId | Nom | Alias | État | Description / comportement |
|---|---|---|---|---|
| Android | l'androïde | androide, android, robot | None | « Un androïde à l'allure humanoïde sobre, immobile derrière le comptoir, en position d'attente. » **Talk** : « L'androïde s'anime : « Veuillez passer votre badge sur la borne pour sortir. » » C'est sa seule réponse, quelle que soit la forme de la demande. |
| Terminal | la borne | borne, borne interactive, terminal | None | « Une borne interactive équipée d'un lecteur de badge. » |
| Display | l'afficheur | afficheur, date, affichage | None | « L'afficheur indique : 9 octobre 2173. » |
| Wheelchairs | les fauteuils et brancards | fauteuil, fauteuils, fauteuil roulant, brancard, brancards | None | « Des fauteuils roulants et des brancards sont alignés à leurs bornes de recharge, tous présents, comme prêts à accueillir des patients. » |
| GlassDoor | la porte vitrée | porte, porte vitree, vitre, rue | None | « À travers la porte vitrée, on voit une rue sans véhicule ni passant, gagnée par une végétation spontanée. » |
| Plants | les plantes | plante, plantes | None | « Quelques plantes en pot, vertes et soignées. » |
| Elevator | l'ascenseur | ascenseur | None | « Les portes métalliques d'un ascenseur, avec un lecteur de badge et un unique bouton. » |
| ElevatorReader | le lecteur de l'ascenseur | lecteur, lecteur ascenseur | **Locked** | « Le voyant du lecteur est rouge. » **Use(Badge, ElevatorReader)** : « Le lecteur refuse le badge. » |
| ElevatorButton | le bouton | bouton | None | « Un bouton « monter ». » **Use** (« appuie sur le bouton ») : « Le bouton ne produit aucun effet. » |

**Comportement spécifique (`Handle`)** : `Use(Instrument = Badge, Target = Terminal)` — la fiche patient s'affiche dans la console sous forme de texte, avec des libellés neutres, puis `Terminal` passe à `Activated` :

```text
FICHE PATIENT
Nom : Marchand
Prénom : Alex
Date d'entrée : 14 mars 2113
Motif d'admission : maladie inconnue évolutive nécessitant un traitement adaptatif
Conditions de sortie : maladie en rémission complète
Adresse de sortie : Alex Marchand, chez M. et Mme Marchand, 12 rue des Tilleuls
```

Message ajouté : « La porte vitrée de l'entrée se déverrouille. » Répéter l'action réaffiche la fiche.

### 3.4 Rue (`Street`) — provisoire

- **Titre** : La rue (provisoire). **Clé d'image** : `Street`.
- **Description** : « Une rue vide, gagnée par la végétation. (Lieu provisoire : fin du PoC.) »
- **Sortie** : **Est** → `HospitalLobby`, toujours ouverte. Aucun objet.
- Ce lieu n'a aucune valeur de lore : la rue n'est pas encore rédigée dans le document univers/scénario.

### 3.5 Images

Une image par pièce, `assets/rooms/<ImageKey>.png`, selon la convention du lore (point de vue côté sud, face au nord, aucune personne visible). Aucune image n'est requise pour le PoC (rectangles de remplacement de l'architecture §9.2).

## 4. Tests propres au PoC

En plus des tests de cohérence génériques de l'architecture (§11), qui doivent rester au vert avec ce contenu :

- Les sorties **sans destination** (Est du couloir, Nord de l'accueil) sont bien toujours `Blocked`.
- Aucune autre sortie n'est à sens unique : chaque retour existe (Chambre ↔ Couloir, Couloir ↔ Accueil, Accueil ↔ Rue).
- Dans le couloir, « utilise badge sur lecteur » produit l'erreur d'ambiguïté et propose les deux lecteurs ; « porte » seule aussi.
- « prends badge » avant « ouvre armoire » : objet inconnu ; après : réussi.
- La fiche patient s'affiche à l'identique du §3.3.

### 4.1 Scénario de référence (test d'intégration)

Depuis une nouvelle partie, dans cet ordre :

1. `Start()` : annonce de réveil, puis description de la chambre. Flèche Est : `Blocked`.
2. « est » → message de porte fermée.
3. « prends badge » → inconnu (armoire fermée). « ouvre armoire », puis « prends badge » → réussi.
4. « utilise badge sur lecteur » → lecteur `Unlocked`, flèche Est `Open`.
5. « est » → couloir. Flèches Nord et Est : `Blocked`.
6. « utilise badge sur lecteur » → erreur d'ambiguïté.
7. « utilise badge sur lecteur escalier » → débloqué. « nord » → accueil.
8. « ouest » → porte vitrée fermée. « parle à l'androïde » → réponse de l'androïde.
9. « passe le badge sur la borne » → fiche patient affichée, flèche Ouest `Open`.
10. « ouest » → rue provisoire.
11. Sauvegarde puis rechargement : pièce, inventaire et états identiques. Au rechargement, l'annonce de réveil n'est pas rejouée.

## 5. Critères d'acceptation

- Le scénario de référence se déroule de bout en bout dans le jeu réel (pas seulement dans les tests).
- Les trois états de flèche sont corrects dans les quatre lieux.
- Les textes accentués s'affichent correctement, y compris la fiche patient et les guillemets.
- Le monde d'amorçage a entièrement disparu (code, objets, tests, enums).
- Build et tests au vert.

## 6. Consignes de réalisation pour l'agent

1. Jalon 1 : enums, classes de pièces, objets, tests du §4 (sans toucher à l'interface).
2. Jalon 2 : suppression du monde d'amorçage, jeu jouable de bout en bout, vérification du scénario dans l'interface.
3. Ne pas modifier l'architecture. Si le contenu l'exige, signaler le besoin.
4. Respecter à la lettre les alias, les états initiaux et les conditions de sortie des tableaux ; signaler toute incohérence avec le lore plutôt que la corriger silencieusement.
5. À la fin, fournir un récapitulatif : ce qui fonctionne, ce qui reste approximatif, les points à valider.

## 7. Points à valider

1. Textes provisoires des objets (hors voix de réveil, date et fiche patient).
2. Formulation exacte de la réponse de l'androïde et des messages d'accompagnement (déverrouillages, refus de badge).
3. Description et rôle définitif de la rue, à remplacer lorsque le §5.4 du lore sera rédigé.