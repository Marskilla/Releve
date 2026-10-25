# Jeu "Relève" — Implémentation du POC V4

> Document d'implémentation — version 1.0
> **Prérequis** : POC V3 livré (branche `develop`), architecture technique V0.7 en place (reflet du code V3).
> À fournir à l'agent avec : l'architecture technique (V0.7), la note d'intention (V6), `AGENTS.md`, et les plans et comptes rendus des POC V1, V2 et V3.
> Les mentions « **[archi]** » signalent un constat tiré de l'architecture V0.7 ou d'un audit antérieur, **non confirmé par l'auteur du plan dans le code** : l'agent le **re-vérifie en J0** et signale tout écart dans son compte rendu.
> Tous les textes de ce document sont **provisoires**, sauf mention contraire. Les noms de champs privés cités (`_viewer`, `_playing`, etc.) sont **indicatifs** : utiliser les noms réels du code.

## 0. Règles de travail de l'agent (impératives)

1. **Un jalon à la fois**, dans l'ordre J0 à J11. **Ne jamais renuméroter, fusionner, découper ni réordonner les jalons.** À la fin de chaque jalon : `dotnet build` puis `dotnet test` verts, puis un court message de progression (ce qui est fait, ce qui reste, tout écart). Ne pas passer au jalon suivant si le build ou un test est rouge.
2. **Chaque exigence est numérotée** (J3-2, J8-6, etc.). Une exigence = une chose vérifiable. Le compte rendu rend compte de **chaque** exigence, une par une (§8.3). Une exigence non faite ou partiellement faite est écrite « non faite » ou « partielle » : ne jamais la déclarer faite par défaut.
3. **Aucune commande git d'écriture** (règle d'`AGENTS.md`) : l'utilisateur commite lui-même.
4. **Ne pas modifier** : `.kilo/`, `_ContentSources/`, le dossier de l'installeur InnoSetup (script et images), `assets/rooms/*.png`, les fichiers audio, `assets/menu/menuBackground.png`, `assets/texts/help.txt`, `assets/icons/`, `assets/views/`, le dossier `Docs/` (sauf la création de `Docs/POC_V4/CompteRenduPOC_04.md`), le document d'architecture. Ne **créer** aucun fichier image, icône ou audio.
5. **Pas de migration .NET** : le projet reste sur .NET 8.
6. **Périmètre fermé** : ce document est la liste complète du travail. **Aucune fonction, classe, fichier ou comportement hors de la liste du §2.** Tout besoin imprévu, toute incohérence avec l'architecture ou ce plan, tout constat **[archi]** inexact : le **signaler** dans le compte rendu, ne pas le traiter. En cas de doute, choisir l'option la plus conservatrice (ne rien changer) et le dire.
7. **Raylib** : vérifier chaque appel (rendu sur texture, souris, plein écran, scissor, journalisation, filtre de texture) dans la cheatsheet et les exemples officiels, pas de mémoire. Si une fonction citée n'existe pas dans le binding Raylib-cs 8.1.0, le signaler et ne pas improviser une autre approche sans le dire.
8. **Encodage et textes** : fichiers UTF-8, **conserver le BOM** des fichiers qui en ont un (par exemple `GameEngine.cs`). **Décision du POC V4 : l'apostrophe des textes est l'apostrophe droite `'` (U+0027), jamais `’` (U+2019).** Caractères accentués **précomposés**. Les guillemets français « » et le caractère `…` (U+2026) sont conservés. Relire les fichiers modifiés après écriture pour vérifier qu'aucun caractère n'est corrompu.
9. **Honnêteté du compte rendu** : ne jamais écrire « vérifié dans le jeu réel » pour ce que l'agent n'a pas observé lui-même. L'agent n'a **aucun** moyen d'observer l'écran ni d'envoyer des touches ou des clics : tout ce qui est visuel ou interactif **attend l'utilisateur** (§7).
10. **Ne pas supprimer ni affaiblir un test pour le faire passer** : le modifier et le signaler.

## 1. But et périmètre

### 1.1 But
Terminer le socle technique du jeu sur le **même contenu de lore** que les POC V1 à V3 (chambre, couloir, accueil, rue provisoire) :
- corriger les écarts restants du POC V3 ;
- corriger les contrôles clavier (flèches pendant la saisie, Début / Fin) ;
- passer toutes les apostrophes des textes en apostrophe droite ;
- plein écran (Alt+Entrée) et rendu à l'échelle de l'interface ;
- défilement de l'image du lieu lors des déplacements ;
- mécanisme de fenêtres modales déclenchées par le moteur (fiche patient à deux boutons, image du hublot) ;
- entrée « Options » du menu (musique, plein écran), réglages mémorisés ;
- améliorations de contenu de la chambre, du couloir et de l'accueil ;
- icône de l'exécutable, publication sans console, version affichée « v0.4 ».

### 1.2 Hors périmètre (impératif)
- Aucun nouveau lieu, objet de lore, puzzle ni élément d'histoire. La rue reste provisoire.
- Pas de description détaillée pour le couloir ni l'accueil.
- Pas d'options accessibles en jeu (seulement depuis le menu), pas de réglage de volume, pas de molette de souris, pas de fondu sonore, pas de support Linux/macOS.
- Pas d'icône de la fenêtre au runtime (`SetWindowIcon`), pas de fichier journal d'erreurs.
- Pas de changement du schéma de la sauvegarde de partie (`SchemaVersion` reste 1).
- Aucune intervention sur le dossier de l'installeur InnoSetup.

### 1.3 Critère de fin
Jalons J0 à J11 terminés, scénario de référence V4 (§6) rejoué autant que l'agent le peut (§7), build et tests au vert, compte rendu écrit (§8).

## 2. Évolutions d'architecture autorisées (liste fermée)

**Moteur (`Releve.Engine`)**
1. `Core/TextWrapper.cs` (nouveau) : fonction pure de retour à la ligne (J2).
2. `CommandResult` : deux propriétés optionnelles `MovedTo` (direction du déplacement réussi) et `Dialog` (demande de fenêtre modale), plus `WithDialog` (J7, J8).
3. `Core/Domain.cs` : types `DialogKind`, `DialogChoice`, `DialogRequest` (J8).
4. `World/Ids.cs` : classe statique `DialogIds` (J8).
5. `RoomBase.OnDialogChoice` (virtuelle) et `GameEngine.ResolveDialog` (J8).
6. `Persistence/GameSettings.cs` et `Persistence/SettingsStore.cs` (nouveaux) : réglages (J5).
7. `GameEngine.DefaultBehaviour` : `Look` sans cible traite une chaîne vide comme absente (J2).
8. Contenu : alias, textes et comportements `Handle` listés en J4 et J8.

**Interface (`Releve.Game`)**
9. Nouveaux fichiers : `Ui/ScreenScaler.cs` (J6), `Ui/DialogButtons.cs`, `Ui/ImageViewer.cs` (J9), `Ui/OptionsDialog.cs` (J10).
10. Modifiés : `GameApp` (rendu sur texture, plein écran, raccourcis, transition, dialogues, options, réglages), `ConsolePanel`, `TextViewer`, `ArrowsPanel`, `InventoryPanel`, `RoomViewPanel`, `MenuScreen`, `ScreenFade`, `MusicPlayer`, `Assets`, `Layout`, `GameInfo`.
11. Build : `Releve.Game.csproj` (type de sortie, icône) (J5).

**Règle d'interaction** (inchangée depuis le V3, §2 de l'architecture) : tout est faisable au clavier. Exceptions souris, chacune doublée d'un équivalent clavier : boutons de défilement de la console ; bouton « Fermer » du visualiseur de texte ; entrées du menu ; indicateurs de défilement du visualiseur. **Le POC V4 ajoute** : les boutons des fenêtres modales (fiche patient, image, options) et les cases à cocher des options. Le curseur reste visible. Les flèches de déplacement de l'interface restent des indicateurs, jamais des boutons.

## 3. Jalons

### J0 — Relecture sans modification
- **J0-1** Lire `AGENTS.md`, l'architecture V0.7 en entier, la note d'intention V6 et ce document.
- **J0-2** Lancer `dotnet build` et `dotnet test` ; noter le nombre de tests de départ (attendu : 128).
- **J0-3** Confirmer ou infirmer chacun des constats **[archi]** C1 à C16 du §9, en citant le fichier lu.
- **J0-4** Lire : `GameApp.cs`, `Program.cs`, `Releve.Game.csproj`, `ConsolePanel.cs`, `TextViewer.cs`, `MenuScreen.cs`, `RoomViewPanel.cs`, `ScreenFade.cs`, `ArrowsPanel.cs`, `InventoryPanel.cs`, `MusicPlayer.cs`, `Layout.cs`, `Assets.cs`, `GameEngine.cs`, `Core/Domain.cs`, `World/Ids.cs`, `World/World.cs`, `Persistence/SaveData.cs`, les trois classes de pièces et leurs fichiers d'objets, `TestWorld.cs`, `EncodingTests.cs`, `PocContentTests.cs`, `WorldConsistencyTests.cs`.
- **J0-5** Vérifier dans la cheatsheet raylib et dans le binding Raylib-cs 8.1.0 l'existence et la signature de : `LoadRenderTexture`, `UnloadRenderTexture`, `BeginTextureMode`, `EndTextureMode`, `DrawTexturePro`, `SetTextureFilter`, `SetMouseOffset`, `SetMouseScale`, `ToggleBorderlessWindowed`, `ToggleFullscreen`, `BeginScissorMode`, `EndScissorMode`, `SetTraceLogLevel`, `SetMusicVolume`, `GetScreenWidth`, `GetScreenHeight`. Noter les noms exacts C#.
- **J0-6** Noter dans `Releve.Game.csproj` : `OutputType`, `RuntimeIdentifier`, présence éventuelle d'`ApplicationIcon`.
- **J0-7** **Ne rien modifier.** Envoyer un court message de constats (confirmés, infirmés, fonctions raylib manquantes).

### J1 — Apostrophes droites
- **J1-1** Remplacer `’` (U+2019) par `'` (U+0027) dans **tous** les littéraux de chaîne et commentaires des fichiers `.cs` de `src/` et `tests/` (textes joueur, messages, listes de `FlavourText`, constantes de pièces et d'objets, `"Texte d'aide introuvable."`). Ne pas toucher les littéraux `char`.
- **J1-2** `TextNormalizer` continue de traiter **les deux** apostrophes (`'` et `’`) comme séparateurs : ne rien retirer de sa logique. Un test de normalisation avec `’` doit rester vert.
- **J1-3** Dans `AGENTS.md`, `README.md` et `agentTools/README.md` : remplacer `’` par `'`, sauf s'il s'agit de syntaxe markdown.
- **J1-4** `EncodingTests` : inverser la règle. Les textes du moteur (mêmes jeux de données qu'aujourd'hui) et les littéraux de chaîne des sources scannées ne contiennent **aucun** `’`. Étendre le scan des sources à `src/Releve.Game/**/*.cs`. Conserver les contrôles « pas de caractère décomposé (U+0300 à U+036F) » et « pas de mot blouse ». Garder le même mécanisme de recherche du dossier `src` (échec clair si introuvable).
- **J1-5** Mettre à jour les tests qui citent un texte contenant `’` (`PocContentTests`, `EngineBehaviourTests`, `ReplyTests`, etc.).
- **J1-6** **Interdit** : modifier `help.txt`, le dossier `Docs/`, remplacer les guillemets « » ou le caractère `…`, renommer des identifiants.

### J2 — Corrections des écarts du POC V3
- **J2-1** `ConsolePanel` : l'invite `> ` de la ligne de saisie est dessinée avec `Palette.ConsoleInput` (au lieu de `Palette.Prompt`). Rien d'autre ne change dans la couleur de la ligne de saisie.
- **J2-2** `ConsolePanel` : le défilement maximal de l'historique est `max(0, nombreDeLignes − lignesVisibles)` pour **toutes** les sources de défilement (Page↑/Page↓, boutons, Début/Fin tant qu'ils existent). Le défilement vers le haut s'arrête quand la première ligne de l'historique est la première ligne visible.
- **J2-3** Créer `Core/TextWrapper.cs` (moteur), classe statique : `IReadOnlyList<string> Wrap(string text, float maxWidth, Func<string, float> measureWidth)`. Règles : les retours `\r\n` et `\r` sont normalisés en `\n` ; chaque `\n` termine une ligne et **les lignes vides sont conservées** ; les mots sont séparés par des espaces ; un mot est ajouté à la ligne courante tant que la largeur mesurée ne dépasse pas `maxWidth` ; sinon la ligne est terminée et le mot ouvre la ligne suivante ; **un mot dont la largeur dépasse à lui seul `maxWidth` est coupé** : ses caractères sont ajoutés un par un, et dès que la largeur est dépassée la ligne est terminée et le reste continue sur la ligne suivante ; une chaîne vide donne une liste contenant une seule chaîne vide.
- **J2-4** `ConsolePanel` : le retour à la ligne d'un message utilise `TextWrapper.Wrap` avec `Fonts.MeasureWidth`. Chaque ligne produite garde le type (saisie / réponse) de son message.
- **J2-5** `TextViewer` : le retour à la ligne utilise `TextWrapper.Wrap` avec la largeur `rectangle.Width − 2 × Layout.Padding − 40` (colonne des indicateurs de 40 px). Le défilement maximal reste `max(0, lignes − lignesVisibles)`.
- **J2-6** `TextViewer` : supprimer la variable inutilisée `lastVisible` de `IndicatorDownRect` ; les zones de clic 30 × 30 px des deux indicateurs sont **centrées sur le triangle dessiné** (calculées à partir des mêmes constantes de position que le dessin), sans décalage vertical.
- **J2-7** `ScreenFade` : le premier `Update` appelé après `StartIn()` **ignore** son `dt`.
- **J2-8** `ArrowsPanel` : pour les rangées Nord et Sud, l'ensemble « flèche + libellé » est centré horizontalement dans le panneau. Supprimer le paramètre `centred` (toujours vrai) et la constante `CellWidth` si elle est inutilisée.
- **J2-9** `InventoryPanel` : corriger le commentaire de classe (défilement par Ctrl+Haut / Ctrl+Bas sans Shift ; Page↑/Page↓ ne concernent pas l'inventaire).
- **J2-10** `GameEngine.DefaultBehaviour`, `Look` sans cible : utiliser `DetailedDescription` seulement s'il n'est ni nul ni vide (`string.IsNullOrEmpty`), sinon `Description`.
- **J2-11** Tests : nouveau `TextWrapperTests.cs` (§5.1) ; test du J2-10 (§5.2).
- **J2-12** **Interdit** : toucher `RoomViewPanel` (J7), `MenuScreen` (J10), les flèches ←/→ et Début/Fin (J3). Ne pas changer les couleurs de `Palette`.

### J3 — Contrôles clavier
- **J3-1** `ConsolePanel` : ajouter la propriété publique `bool InputIsEmpty` (vrai si la ligne de saisie ne contient aucun caractère).
- **J3-2** `GameApp.UpdateGame` : en tout début de traitement de l'image, mémoriser `inputEmptyAtFrameStart = _console.InputIsEmpty`. Les raccourcis de déplacement ↑ ↓ ← → ne sont envoyés **que si** `inputEmptyAtFrameStart` est vrai **et** ni Shift ni Ctrl ne sont enfoncés. Quand la ligne de saisie n'est pas vide, ← et → déplacent seulement le curseur de saisie, ↑ et ↓ ne font rien. La répétition (maintien de la touche) est conservée.
- **J3-3** `ConsolePanel` : les touches Début et Fin ne font **plus** défiler l'historique ; elles déplacent seulement le curseur au début / à la fin de la ligne de saisie. Page↑ / Page↓ et les deux boutons de défilement restent inchangés.
- **J3-4** **Interdit** : modifier Shift+↑/↓ (historique des commandes), Ctrl+↑/↓ (inventaire), les touches du `TextViewer` (Début/Fin y restent des touches de défilement), ou ajouter un nouveau raccourci.

### J4 — Contenu : chambre, couloir, accueil
- **J4-1** `BedroomMachines` : ajouter les alias `materiel` et `materiel medical` (formes normalisées). « regarde materiel », « examine matériel médical » renvoient le même texte que « regarde machine ».
- **J4-2** `TechDoor` : ajouter l'alias `porte local technique`. `TechReader` : ajouter l'alias `lecteur local technique`. « regarde porte local technique » et « utilise badge sur lecteur local technique » fonctionnent comme avec « local ». Ne pas ajouter d'alias `local technique` seul (l'ambiguïté porte / lecteur est voulue).
- **J4-3** `WorldConsistencyTests` : nouveau test « nom affiché reconnu » : pour chaque objet du monde livré, **tous** les mots de `Normalize(Name)` sont contenus dans **un même** alias normalisé de l'objet. Si le test échoue pour un objet, ajouter **un** alias égal au nom normalisé de cet objet (attendu : `Wheelchairs`) et lister chaque ajout dans le compte rendu.
- **J4-4** `HospitalLobby` : nouvelle constante `WestBlockedMessage` (texte §4.2). Elle est le message de la sortie Ouest bloquée **et** la réponse à « ouvre » sur `GlassDoor` quand `Terminal` n'est pas `Activated` (supprimer l'ancienne constante devenue inutile). Le message « La porte vitrée est déverrouillée. Le passage est libre vers l'ouest. » (borne activée) est conservé.
- **J4-5** `HospitalLobby.Handle`, `Talk` sur `Android` : réponse selon l'état de `Terminal` (§4.3) : avant activation, la réplique d'émargement ; après activation, la réplique de congé. Quelle que soit la forme de la demande (« parle androide », « demande a l'androide », etc.).
- **J4-6** `HospitalLobby.Description` : nouveau texte (§4.1). `Display` : ajouter l'alias `horloge`. « examine horloge », « regarde afficheur », « lis afficheur » donnent la date du jour (texte inchangé).
- **J4-7** Tests : `ContentV4Tests.cs` (§5.3).
- **J4-8** **Interdit** : modifier la fiche patient ou le comportement de « passe le badge sur la borne » (J8) ; ajouter un objet ; changer les états initiaux.

### J5 — Socle : version, build sans console, icône, réglages
- **J5-1** `GameInfo.Version = "v0.4"`.
- **J5-2** `Releve.Game.csproj` : `OutputType` vaut `WinExe` en configuration Release et `Exe` dans les autres configurations (deux propriétés avec condition `'$(Configuration)' == 'Release'` / `!=`). Ainsi `dotnet publish -c Release -r win-x64 --self-contained` ne produit plus de console, et `dotnet run` en Debug la garde.
- **J5-3** `Releve.Game.csproj` : `<ApplicationIcon>assets\icons\releve.ico</ApplicationIcon>` avec la condition `Exists('assets\icons\releve.ico')`, pour que le build reste vert tant que le fichier n'est pas fourni par l'utilisateur. **Ne jamais créer** de fichier `.ico`.
- **J5-4** Avant `InitWindow`, en configuration Release uniquement (`#if !DEBUG`) : abaisser le niveau de journalisation raylib à `Warning` (`SetTraceLogLevel`). Vérifier le nom exact de l'énumération dans le binding.
- **J5-5** Créer `Persistence/GameSettings.cs` : `public sealed record GameSettings(bool Music = true, bool Fullscreen = false);`.
- **J5-6** Créer `Persistence/SettingsStore.cs` : constructeur avec **chemin de fichier injectable** (valeur par défaut : fichier `settings.json` dans **le même dossier que `save.dat`**, en réutilisant `SaveLocation`) ; `GameSettings Load()` : renvoie les valeurs par défaut si le fichier est absent, illisible ou invalide, **sans jamais lever d'exception** ; `void Save(GameSettings)` : JSON **non chiffré**, indenté, écriture atomique (fichier `.tmp` puis `File.Move(..., overwrite: true)`), dossier créé au besoin, exceptions d'E/S avalées. Le JSON contient `music` et `fullscreen` (les champs inconnus sont ignorés).
- **J5-7** Tests : `SettingsTests.cs` (§5.4).
- **J5-8** Vérification du build sans console (à faire et à consigner) : exécuter `dotnet publish src/Releve.Game -c Release -r win-x64 --self-contained` puis, en PowerShell 5.1, lire le **sous-système** de `Releve.Game.exe` publié : `$b=[IO.File]::ReadAllBytes($exe); $pe=[BitConverter]::ToInt32($b,0x3C); [BitConverter]::ToUInt16($b,$pe+24+68)`. Attendu : **2** (GUI). Faire la même lecture sur l'exe de `dotnet build` en Debug : attendu **3** (console). Consigner les deux valeurs dans le compte rendu.
- **J5-9** **Interdit** : ajouter un journal d'erreurs, modifier le dossier de l'installeur, utiliser `SetWindowIcon`, inclure les réglages dans la sauvegarde de partie.

### J6 — Plein écran (Alt+Entrée)
**Principe** : tout le jeu est dessiné dans une texture de rendu **fixe de 1280 × 720** (`Layout.ScreenWidth` × `Layout.ScreenHeight`), puis cette texture est affichée dans la fenêtre à l'échelle, centrée, avec des bandes noires pour conserver le rapport. En mode fenêtré (1280 × 720), l'échelle vaut 1 et le rendu est identique à aujourd'hui. Aucun panneau ne connaît la taille réelle de la fenêtre.
- **J6-1** `GameApp` : après `InitWindow`, créer la texture de rendu logique (`LoadRenderTexture`), filtre bilinéaire (`SetTextureFilter`), la libérer à la fermeture. Tout le dessin actuel de la boucle (fond, scène, visualiseur, fondu) passe dans `BeginTextureMode` / `EndTextureMode`.
- **J6-2** Créer `Ui/ScreenScaler.cs` : `Update(int windowWidth, int windowHeight)` calcule `Scale = min(windowWidth / 1280f, windowHeight / 720f)` (flottant, **sans arrondi à l'entier**) et `DestRect` (rectangle centré) ; applique `SetMouseOffset(-DestRect.X, -DestRect.Y)` et `SetMouseScale(1 / Scale, 1 / Scale)` pour que `GetMousePosition` renvoie des coordonnées **logiques**. Vérifier la formule exacte de raylib (position = (brute + offset) × scale) dans la documentation ou le code source de raylib ; si elle ne se vérifie pas, **le signaler** sans réécrire les panneaux.
- **J6-3** Présentation : `BeginDrawing`, fond noir, `DrawTexturePro` de la texture de rendu (rectangle source avec hauteur **négative** pour l'inversion verticale propre à raylib) vers `DestRect`, `EndDrawing`. `ScreenScaler.Update` est appelé **à chaque image avant** les mises à jour.
- **J6-4** Rechercher dans toute la couche Game les usages de `GetScreenWidth`, `GetScreenHeight`, `GetMonitorWidth`, `GetMonitorHeight`, `GetWindowPosition` (hors `ScreenScaler` et `GameApp` pour la présentation) et les remplacer par `Layout.ScreenWidth` / `Layout.ScreenHeight`. Lister les remplacements dans le compte rendu.
- **J6-5** `GameApp` : méthode unique `SetFullscreen(bool on)`. Le champ booléen `_fullscreen` est **la source de vérité** de l'état ; l'API raylib n'est appelée que si l'état demandé diffère de `_fullscreen`. Mode retenu : **plein écran en fenêtre sans bordure** (`ToggleBorderlessWindowed`) si la fonction existe dans le binding, sinon `ToggleFullscreen`. Signaler le choix et la preuve.
- **J6-6** Alt+Entrée : en **tout début** de chaque image (avant menu, jeu, visualiseur, fondu, options), si Alt (gauche ou droite) est enfoncé et que Entrée vient d'être pressée (`IsKeyPressed`) : basculer via `SetFullscreen(!_fullscreen)`, enregistrer le réglage (J6-8), vider la file de caractères et **sauter** toutes les mises à jour de cette image. Fonctionne dans tous les états de l'application.
- **J6-7** `ConsolePanel` : ne **valide pas** la ligne de saisie quand Alt est enfoncé.
- **J6-8** Au démarrage, `GameApp` charge `SettingsStore.Load()` (champ `_settings`) ; si `Fullscreen` est vrai, appelle `SetFullscreen(true)` après la création de la fenêtre. À chaque bascule, `_settings = _settings with { Fullscreen = _fullscreen }` puis `SettingsStore.Save(_settings)`.
- **J6-9** **Interdit** : rendre la fenêtre redimensionnable, changer la résolution du moniteur, modifier les constantes de `Layout`, calculer une échelle ou un décalage dans un panneau, modifier la logique de la souris dans les panneaux.

### J7 — Défilement de l'image du lieu
- **J7-1** Moteur : `CommandResult` reçoit la propriété `public Direction? MovedTo { get; init; }`. `GameEngine.Move` la renseigne **uniquement** quand le déplacement réussit (y compris par mot de sortie) ; elle est nulle dans tous les autres cas (déplacement bloqué, autre commande).
- **J7-2** `RoomViewPanel` : corriger d'abord le rendu des zones d'image : dimensions de référence **lues dans la texture de base chargée** (plus de 1774 × 887 codés en dur) ; arrondi **séparé de chaque bord** (`gauche = round(vx + X1 × échelleX)`, `droite = round(vx + X2 × échelleX)`, largeur = droite − gauche ; idem en hauteur) ; commentaire périmé « en conservant les proportions » corrigé.
- **J7-3** `RoomViewPanel` : isoler le dessin d'une vue dans une méthode privée `DrawView(RoomView view, float dx, float dy)` (image de base, zones d'image, rectangle de remplacement si l'image est absente), décalée de `dx, dy`.
- **J7-4** `RoomViewPanel` : API de transition : `BeginTransition(RoomView from, RoomView to, Direction dir)`, `Update(float dt)`, `bool IsTransitioning`, `CompleteTransition()` (termine instantanément). Constante `TransitionDuration = 0.4f` (secondes). Progression adoucie par `e = p × p × (3 − 2p)`. Au démarrage, charger (et mettre en cache) les textures de base et alternatives des **deux** vues. Le premier `Update` après `BeginTransition` **ignore** son `dt`.
- **J7-5** Sens (W = largeur de `ViewBounds`, H = hauteur) : **Est** : vue sortante `dx = −e·W`, vue entrante `dx = W − e·W`. **Ouest** : sortante `dx = +e·W`, entrante `dx = −W + e·W`. **Nord** : sortante `dy = +e·H`, entrante `dy = −H + e·H`. **Sud** : sortante `dy = −e·H`, entrante `dy = H − e·H`.
- **J7-6** Dessin pendant une transition : `BeginScissorMode` sur `Layout.ViewBounds` (vérifier son comportement dans une texture de rendu), dessiner la vue sortante puis l'entrante avec leurs décalages, `EndScissorMode`, **puis** le cadre. Hors transition : comportement actuel.
- **J7-7** `GameApp.Submit` : **avant** `Execute`, appeler `CompleteTransition()` et mémoriser la vue courante `prevView` ; **après**, si `result.MovedTo` n'est pas nul et que la pièce de la nouvelle vue diffère de celle de `prevView`, appeler `BeginTransition(prevView, newView, result.MovedTo.Value)`. `GameApp.Run` appelle `Update(dt)` du panneau à chaque image. Les raccourcis fléchés passent par `Submit` : ils déclenchent donc la transition.
- **J7-8** Pas de transition : démarrage d'une nouvelle partie, « Continuer », déplacement refusé, commande autre qu'un déplacement. La saisie n'est **pas** bloquée pendant la transition ; les messages de la console s'affichent immédiatement.
- **J7-9** Tests : §5.5.
- **J7-10** **Interdit** : durée supérieure à 0,5 s, son, blocage de la saisie, modification de la logique des flèches ou de l'inventaire, transition entre deux vues d'une même pièce.

### J8 — Moteur : fenêtres modales demandées par le moteur
- **J8-1** `Core/Domain.cs` : 
```csharp
public enum DialogKind { Text, Image }
public sealed record DialogChoice(string Id, string Label);
// Content : le texte à afficher (Text) ou la clé de l'image (Image, fichier assets/views/<Content>.png).
public sealed record DialogRequest(DialogKind Kind, string? Title, string Content,
    IReadOnlyList<DialogChoice> Choices, string DefaultChoiceId, string CancelChoiceId);
```
- **J8-2** `CommandResult` : propriété `public DialogRequest? Dialog { get; init; }` et méthode `public CommandResult WithDialog(DialogRequest d) => this with { Dialog = d };`. Les constructions et fabriques existantes (`Say`, `Changed`) restent valides.
- **J8-3** `World/Ids.cs` : classe statique `DialogIds` avec les constantes `Close = "Close"`, `PatientAccept = "PatientAccept"`, `PatientRefuse = "PatientRefuse"`.
- **J8-4** `RoomBase` : `public virtual CommandResult? OnDialogChoice(string choiceId, GameState s) => null;`. `GameEngine` : `public CommandResult ResolveDialog(string choiceId)` appelle `OnDialogChoice` de la pièce courante ; si le résultat est nul (choix inconnu ou sans effet), renvoie `CommandResult.Say()` (aucun message, aucun changement).
- **J8-5** `TechCorridor.Handle`, `Look` avec cible `TechDoor` : renvoie `CommandResult.Say(<description de la porte du local technique>)` **avec** une demande de dialogue : `Kind = Image`, `Title = null`, `Content = "technicalRoom"`, un seul choix `Close` (« Fermer »), défaut et annulation = `Close`. Le texte de la description est réutilisé depuis la constante de `Describe` de `TechDoor` (pas de doublon). Ne **pas** déclencher le dialogue pour `Read` ni pour les autres objets. `Look` sans changement d'état : `Say`, pas `Changed`.
- **J8-6** `HospitalLobby.Handle`, `Use(Instrument = Badge, Target = Terminal)` : renvoie `Say` (pas `Changed`) avec le message `BadgeOnTerminalMessage` (§4.4) **et** une demande de dialogue `Kind = Text`, `Title = null`, `Content` = la fiche patient **existante, inchangée** (constante `HospitalLobby.PatientRecord`, 7 lignes), choix `[PatientAccept « J'accepte de sortir », PatientRefuse « Je ne suis pas prêt à sortir »]`, `DefaultChoiceId = PatientRefuse`, `CancelChoiceId = PatientRefuse`. Si `Terminal` est **déjà** `Activated` : même fiche, un seul choix `Close` (« Fermer »). **La fiche n'est plus affichée dans la console.** L'état de `Terminal` n'est **pas** modifié à ce stade.
- **J8-7** `HospitalLobby.OnDialogChoice` : `PatientAccept` → si `Terminal` n'est pas `Activated`, le passer à `Activated` et renvoyer `Changed(AcceptMessage)` ; sinon aucun effet ; `PatientRefuse` → `Say(RefuseMessage)`, aucun changement d'état ; `Close` → `Say()` ; tout autre identifiant → `null`.
- **J8-8** Tests : `DialogTests.cs` (§5.6) ; adapter les tests existants qui attendaient la fiche dans la console (§5.7).
- **J8-9** **Interdit** : modifier le contenu de la fiche patient, ajouter un champ à `GameState`, sauvegarder une demande de dialogue, faire dépendre le moteur d'un type de la couche Game.

### J9 — Interface : fenêtres modales
- **J9-1** Créer `Ui/DialogButtons.cs` : composant partagé qui, pour une liste de libellés, calcule la disposition d'une **rangée de boutons centrée** (largeur de chaque bouton = largeur du libellé + 2 × `Layout.Padding`, espacement de 20 px), gère l'indice sélectionné, le survol, le clic et le dessin (rectangle plein, bordure ; **bouton sélectionné** : bordure vive et fond plus clair ; pas de glyphes spéciaux).
- **J9-2** `TextViewer` : nouvelle signature `Open(Rectangle rect, string? title, string text, IReadOnlyList<DialogChoice> choices, string defaultChoiceId, string cancelChoiceId)`. L'ancienne signature `Open(rect, title, text)` est **conservée** et équivaut à un seul bouton « Fermer » (`Close`), défaut et annulation = `Close`. Les boutons remplacent le bouton « Fermer » existant (via `DialogButtons`). Propriété `string? ChosenId` renseignée à la validation.
- **J9-3** `TextViewer`, clavier : ← / → et Tab / Maj+Tab changent le bouton sélectionné ; **Entrée** valide le bouton sélectionné ; **Échap** valide `cancelChoiceId`. Les touches de défilement du texte (↑ / ↓, Page↑ / Page↓, Début / Fin) restent inchangées. `Update()` renvoie vrai **uniquement** à la validation d'un bouton. Souris : le survol sélectionne un bouton, un appui du bouton gauche le valide ; mêmes règles anti-clic fantôme qu'aujourd'hui (seul un appui survenu après l'ouverture compte ; aucune entrée traitée dans l'image d'ouverture).
- **J9-4** `Ui/Layout.cs` : constantes `DialogBounds` (rectangle centré, 60 % de la largeur et 60 % de la hauteur de 1280 × 720, soit 768 × 432 à la position (256, 144)) et `ImageDialogBounds` (carré de côté 60 % de `ScreenHeight`, soit 432 × 432 à la position (424, 144)).
- **J9-5** `Ui/Assets.cs` : méthode donnant le chemin `assets/views/<key>.png` (casse exacte).
- **J9-6** Créer `Ui/ImageViewer.cs` : modale carrée `ImageDialogBounds`, opaque, bordure. L'image (`assets/views/<key>.png`) est **étirée sur tout le carré** ; le bouton « Fermer » (`DialogButtons`) est dessiné **par-dessus**, centré en bas, sur un fond opaque. Fermeture : clic sur le bouton, Entrée ou Échap. Si le fichier image est absent : fond uni et texte « Image introuvable. » au centre (comportement attendu, pas d'erreur). Mêmes règles de modalité et anti-clic fantôme que `TextViewer`. API : `Open(string imageKey)`, `Update()` (vrai à la fermeture), `Draw()`, `IsOpen`. Texture mise en cache, libérée à la fermeture du jeu.
- **J9-7** `GameApp.Submit` : après l'ajout des messages de la réponse, si `result.Dialog` n'est pas nul : `Kind = Text` → `TextViewer.Open(Layout.DialogBounds, dialog.Title, dialog.Content, dialog.Choices, dialog.DefaultChoiceId, dialog.CancelChoiceId)` ; `Kind = Image` → `ImageViewer.Open(dialog.Content)`. **Un seul modal à la fois.** Tant qu'un modal est ouvert : ni menu, ni jeu ne sont mis à jour, la file de caractères est vidée à chaque image, la musique continue (comportement actuel du visualiseur d'aide).
- **J9-8** À la fermeture d'un modal ouvert depuis le moteur : appeler `_engine.ResolveDialog(chosenId)` ; ajouter ses messages à la console (type réponse) ; sauvegarder si `StateChanged` ; relire la vue. Ne **pas** appeler `ResolveDialog` pour l'aide du menu.
- **J9-9** **Interdit** : modifier l'aide plein écran (sinon son bouton unique « Fermer »), ajouter la molette, empiler deux modales, appeler `ResolveDialog` sans identifiant, écrire dans la console la fiche patient.

### J10 — Menu « Options » et réglages
- **J10-1** `MenuChoice` reçoit `Options`. `MenuScreen` : quatre entrées, dans l'ordre **Continuer, Nouvelle partie, Options, Aide**. « Options » est toujours accessible. La sélection par défaut et le grisage de « Continuer » ne changent pas. Navigation clavier cyclique sur quatre entrées.
- **J10-2** `MenuScreen` : `DrawEntry` utilise `EntryRect(index)` (même rectangle que `Update`). Un appui du bouton gauche sur une entrée activée **fixe la sélection sur cette entrée** avant de renvoyer le choix, même si la souris n'a pas bougé.
- **J10-3** Créer `Ui/OptionsDialog.cs` : modale centrée (environ 480 × 260 px), opaque, titre « Options », deux cases à cocher « Musique » et « Plein écran », un bouton « Fermer » (`DialogButtons`). Cases dessinées **en géométrie** (carré de contour, repère plein quand coché ; pas de glyphe). Clavier : ↑ / ↓ et Tab / Maj+Tab déplacent le focus (cyclique sur trois contrôles) ; Espace ou Entrée active le contrôle en focus (bascule d'une case, ou fermeture) ; Échap ferme. Souris : le survol met le focus, un appui du bouton gauche bascule la case ou ferme. Mêmes règles de modalité et anti-clic fantôme.
- **J10-4** `GameApp` : le choix `Options` ouvre l'`OptionsDialog` (aucun fondu, aucune mise à jour du menu tant qu'elle est ouverte, file de caractères vidée). La case « Plein écran » **reflète `_fullscreen` à chaque image** (donc aussi après un Alt+Entrée pendant que la fenêtre est ouverte) ; la cocher ou la décocher appelle `SetFullscreen` puis enregistre le réglage. La case « Musique » appelle `MusicPlayer.SetEnabled` puis enregistre le réglage.
- **J10-5** `MusicPlayer` : méthode `SetEnabled(bool)`. Désactivée, aucune piste n'est audible ; réactivée, la piste courante est de nouveau audible, sans redémarrer la musique du menu inutilement. L'état est appliqué à **toute nouvelle piste** (menu, jeu). Approche recommandée : volume du flux courant à 0 / à sa valeur normale (`SetMusicVolume`) plutôt qu'arrêt / reprise ; vérifier l'API. Au démarrage, appliquer `_settings.Music`.
- **J10-6** À la fermeture de la fenêtre Options, retour au menu avec la sélection sur « Options » (même mécanisme que le retour de l'aide, drapeau `_menuEntered`).
- **J10-7** **Interdit** : accès aux options depuis le jeu, case ou curseur supplémentaire, réglage de volume, modification de l'entrée « Aide ».

### J11 — Documentation et compte rendu
Voir §8.

## 4. Textes (provisoires, à copier tels quels ; apostrophes droites)

### 4.1 Accueil — description (`HospitalLobby.Description`)
```text
Le hall d'accueil est vaste, lumineux et parfaitement entretenu. Derrière le comptoir, un androïde se tient immobile. Un afficheur lumineux est fixé au-dessus du comptoir.
```

### 4.2 Accueil — porte vitrée fermée (`WestBlockedMessage`)
```text
La porte vitrée de l'entrée est fermée. L'androïde s'adresse alors à vous : « S'il vous plaît ! » Il semble vous inviter à venir le voir.
```

### 4.3 Accueil — réponses de l'androïde (« parle androide »)
Borne non activée :
```text
L'androïde, qui semblait être en pause, prend vie et s'adresse à vous : « La politique de l'établissement nous empêche de laisser sortir des patients sans émargement préalable. Passez votre badge sur la borne du comptoir pour émarger, merci. »
```
Borne activée :
```text
L'androïde s'anime : « Merci d'avoir fait appel à nos services, et j'espère, pour vous, ne pas vous revoir bientôt. Ah ah ah ! »
```

### 4.4 Accueil — borne et fiche patient
`BadgeOnTerminalMessage` :
```text
Vous passez le badge devant la borne. La fiche patient s'affiche.
```
`AcceptMessage` :
```text
Vous validez la sortie. La porte vitrée de l'entrée se déverrouille.
```
`RefuseMessage` :
```text
Vous ne validez pas la sortie.
```
Boutons : « J'accepte de sortir », « Je ne suis pas prêt à sortir », « Fermer ».

### 4.5 Interface
Entrée de menu « Options ». Fenêtre : titre « Options », cases « Musique » et « Plein écran », bouton « Fermer ». Image absente : « Image introuvable. ».

## 5. Tests

Les tests existants restent verts, sauf ceux dont le comportement change volontairement (§5.7). Nombre de tests attendu en fin de POC : supérieur à 150.

### 5.1 `TextWrapperTests.cs` (nouveau) — mesure simulée : 1 unité par caractère
- Un texte plus court que la largeur : une seule ligne.
- Retour à la ligne aux espaces ; aucune ligne ne dépasse la largeur.
- Un mot plus long que la largeur est coupé ; aucune ligne ne dépasse la largeur ; la concaténation des morceaux redonne le mot.
- Les lignes vides sont conservées (`"a\n\nb"` donne trois lignes).
- `\r\n` est traité comme `\n`.
- Chaîne vide : une liste d'une chaîne vide.

### 5.2 `EngineBehaviourTests.cs`
- `Look` sans cible dans une pièce dont `DetailedDescription` renvoie une chaîne vide renvoie la `Description` (utiliser une classe de pièce **locale au fichier de test** pour fabriquer un petit monde).

### 5.3 `ContentV4Tests.cs` (nouveau)
- « regarde materiel » et « examine materiel medical » (chambre) renvoient le même texte que « regarde machine ».
- Couloir : « utilise badge sur lecteur local technique » répond « Le lecteur refuse le badge. Son voyant clignote en rouge. » ; « regarde porte local technique » renvoie la description de la porte ; « porte » seule et « lecteur » seul restent ambigus.
- Accueil : la description contient « afficheur » ; « examine horloge » et « examine afficheur » renvoient la date du jour ; « ouest » porte fermée renvoie `WestBlockedMessage` (§4.2) ; « ouvre porte vitrée » (borne non activée) renvoie le même message.
- Androïde : « parle androide » avant activation renvoie la réplique d'émargement ; après activation de la borne (état forcé à `Activated`), la réplique de congé.
- `WorldConsistencyTests` : test « nom affiché reconnu » (J4-3).

### 5.4 `SettingsTests.cs` (nouveau, fichier temporaire)
- Fichier absent : valeurs par défaut (`Music` vrai, `Fullscreen` faux).
- Aller-retour : `Save` puis `Load` redonnent les mêmes valeurs.
- Fichier au contenu invalide : valeurs par défaut, aucune exception.
- Champ inconnu dans le JSON : ignoré, champs connus lus.
- `Save` dans un dossier inexistant crée le dossier.

### 5.5 Déplacement (`EngineBehaviourTests.cs` ou `PocContentTests.cs`)
- « est » depuis la chambre, lecteur déverrouillé : `MovedTo = East` et `StateChanged`.
- « est » depuis la chambre, lecteur verrouillé : `MovedTo` nul.
- « sors » (mot de sortie) depuis la chambre déverrouillée : `MovedTo = East`.
- Une commande autre qu'un déplacement (« regarde ») : `MovedTo` nul.

### 5.6 `DialogTests.cs` (nouveau)
- « passe le badge sur la borne » (badge en inventaire, accueil) : le résultat contient une demande `Kind = Text` avec deux choix (`PatientAccept`, `PatientRefuse`), `DefaultChoiceId = PatientRefuse`, `CancelChoiceId = PatientRefuse` ; `StateChanged` faux ; `Terminal` n'est **pas** `Activated` ; les messages ne contiennent **pas** « FICHE PATIENT ».
- Le contenu de la demande est **identique** à la fiche patient attendue (7 lignes du §3.3 du PoC V1, apostrophes droites).
- `ResolveDialog("PatientRefuse")` : `Terminal` reste non activé, `StateChanged` faux, la sortie Ouest reste bloquée.
- `ResolveDialog("PatientAccept")` : `Terminal` passe à `Activated`, `StateChanged` vrai, la sortie Ouest est ouverte, « parle androide » donne la réplique de congé.
- Après acceptation, « passe le badge sur la borne » renvoie une demande avec un **seul** choix `Close`.
- `ResolveDialog("n'importe quoi")` : aucun message, aucun changement d'état.
- « regarde porte local technique » : la réponse contient la description **et** une demande `Kind = Image`, `Content = "technicalRoom"`, un seul choix `Close` ; `StateChanged` faux. « regarde hublot » idem. « lis porte local technique » : pas de demande de dialogue.
- Sauvegarde puis rechargement après acceptation : `Terminal` reste `Activated` et la sortie Ouest ouverte.

### 5.7 Tests existants à adapter volontairement
- Tous ceux qui citent un texte avec `’` (J1-5).
- `EncodingTests` (inversion, J1-4).
- `PocContentTests` : scénario de référence (la fiche passe par le dialogue ; étape « ouest » après `ResolveDialog("PatientAccept")`) ; textes de l'accueil et de la réplique de l'androïde ; message de la porte vitrée.
- Les compteurs de `WorldConsistencyTests` (4 pièces, 22 objets) ne changent pas.

## 6. Scénario de référence V4 (pour l'utilisateur ; l'agent en rejoue ce que les tests couvrent)

1. Menu : « v0.4 » en bas à droite ; entrées Continuer, Nouvelle partie, **Options**, Aide ; clic et clavier fonctionnent ; l'icône apparaît sur l'exécutable.
2. Options : cocher / décocher « Musique » coupe / rétablit le son ; cocher « Plein écran » bascule ; Échap ou « Fermer » ramène au menu avec la sélection sur « Options ». Relancer le jeu : les deux réglages sont conservés.
3. Alt+Entrée à tout moment : bascule plein écran / fenêtré, **interface proportionnée avec bandes noires**, image du lieu et souris (menu, boutons de la console) bien alignées.
4. Nouvelle partie. Chambre : « regarde materiel » = « regarde machine ». « ouvre armoire », « prends badge », « utilise badge sur lecteur », « est » : l'image du couloir **glisse** depuis la droite pendant 0,4 s ; « ouest » : l'inverse.
5. Couloir : « regarde porte local technique » : texte dans la console **et** fenêtre carrée au centre avec l'image du hublot ; « Fermer », Échap ou Entrée la ferment. « utilise badge sur lecteur local technique » : refus.
6. Taper « ouvre » (sans entrée) puis ← / → : seul le curseur bouge, aucun déplacement ; ligne vide : ← / → déplacent. Début / Fin ne font plus défiler la console ; Page↑ / Page↓ si.
7. Escalier, accueil : la description cite l'afficheur ; « examine horloge » donne la date ; « ouest » : message de l'androïde ; « parle androide » : réplique d'émargement ; transition verticale vers le haut au passage de l'escalier.
8. « passe le badge sur la borne » : **fenêtre à 60 % de l'écran** avec la fiche ; boutons « J'accepte de sortir » / « Je ne suis pas prêt à sortir » ; ← / → les sélectionnent ; Échap = refus (rien ne change). Puis accepter : la porte vitrée de l'image s'ouvre, « ouest » mène à la rue, l'androïde donne sa réplique de congé.
9. Quitter, relancer, « Continuer » : état conservé ; pas de fondu ni de message d'accueil.
10. Publication : `dotnet publish -c Release -r win-x64 --self-contained` : aucune console ne s'ouvre avec le jeu.

## 7. Ce que l'agent peut et ne peut pas vérifier

L'agent **n'a pas** de moyen d'observer l'écran ni d'envoyer des touches ou des clics. Il vérifie donc ce qui relève du build, des tests, de la relecture du code et de la lecture de l'en-tête de l'exe ; tout le reste **attend l'utilisateur**.

| Élément | Tests | Relecture du code | Validation humaine |
|---|---|---|---|
| Moteur : alias, textes, dialogues, `MovedTo`, retour à la ligne, réglages | oui | oui | non |
| Sous-système de l'exe publié (GUI / console) | non (lecture PE) | oui | **oui** (aucune console à l'écran) |
| Plein écran : bascule, bandes noires, souris alignée, netteté du texte | non | oui | **oui** |
| Défilement d'image : sens, durée, fluidité, absence d'à-coup | non | oui | **oui** |
| Modales : fiche patient (60 %), image du hublot, boutons, sélection clavier | non | oui | **oui** |
| Menu Options, cases, mémorisation des réglages | non (hors store) | oui | **oui** |
| Musique coupée / rétablie | non | oui | **oui** |
| Touches : ←/→ avec saisie, Début/Fin, Alt+Entrée | non | oui | **oui** |
| Icône de l'exécutable | non | oui (csproj) | **oui** |

## 8. Documentation et compte rendu

### 8.1 Documents à mettre à jour par l'agent
- `AGENTS.md` : mention du POC V4, version « v0.4 », contrat d'assets (`assets/views/`, `assets/icons/`), limites de vérification de l'agent (plein écran, souris, défilement, modales).
- `README.md` : liste des assets, version affichée, publication sans console.
- `agentTools/README.md` : mentionner que plein écran, transition d'image, modales et souris ne sont pas automatisables.
- **Ne pas modifier** le document d'architecture : l'auteur du plan le réécrit lui-même (V0.8) à partir du compte rendu.

### 8.2 Compte rendu `Docs/POC_V4/CompteRenduPOC_04.md`
Sections obligatoires :
1. **Résultat** : jalons terminés, nombre de tests avant et après, valeurs lues pour le sous-système de l'exe (J5-8).
2. **Traçabilité exigence par exigence** : un tableau avec **toutes** les exigences J0-1 à J10-7 : identifiant | statut (**faite / partielle / non faite**) | fichier(s) | méthode(s) | preuve (nom du test, ou « relecture », ou « attend l'utilisateur »).
3. **Constats [archi]** C1 à C16 : confirmé ou infirmé, avec le fichier consulté.
4. **Vérifié par les tests** (fichiers et ce qu'ils couvrent).
5. **Vérifié par relecture du code seulement.**
6. **Attend la validation humaine** : reprendre le tableau du §7, avec ce qu'il faut regarder précisément (une phrase par point).
7. **Écarts et besoins signalés** (tout ce qui est hors liste du §2, toute incohérence, toute fonction raylib manquante ou de nom différent, tout alias ajouté par J4-3, tous les remplacements de J6-4).
8. **Éléments pour l'architecture V0.8** : liste des types, méthodes, constantes et fichiers ajoutés ou modifiés (noms exacts), des constantes de `Layout` ajoutées, des choix techniques faits (plein écran, volume de la musique).
9. **Points à valider** (§10).
La section « vérifié dans le jeu réel » **ne doit être présente que si l'agent a réellement observé le jeu** ; sinon, écrire : « non vérifié visuellement par l'agent ».

## 9. Constats [archi] à confirmer en J0

| # | Constat | Fichier |
|---|---|---|
| C1 | L'invite `> ` de la ligne de saisie utilise `Palette.Prompt` | `ConsolePanel.cs` |
| C2 | Le défilement maximal est `Count − 1` (une seule ligne visible en haut) | `ConsolePanel.cs` |
| C3 | Un mot plus large que la zone n'est pas coupé (console et visualiseur) | `ConsolePanel.cs`, `TextViewer.cs` |
| C4 | La largeur de retour à la ligne du visualiseur n'est pas réduite de 40 px | `TextViewer.cs` |
| C5 | Le premier `Update` après `StartIn` n'ignore pas `dt` | `ScreenFade.cs` |
| C6 | Zones d'image : pas d'arrondi par bord, 1774 × 887 en dur, commentaire périmé | `RoomViewPanel.cs` |
| C7 | `DrawEntry` ne partage pas `EntryRect` ; un clic ne fixe pas la sélection sans mouvement de souris | `MenuScreen.cs` |
| C8 | `Look` sans cible teste `DetailedDescription` contre `null` seulement | `GameEngine.cs` |
| C9 | ← / → (et ↑ / ↓) déplacent le joueur **et** le curseur pendant une saisie | `GameApp.cs`, `ConsolePanel.cs` |
| C10 | Début / Fin font défiler la console **et** déplacent le curseur | `ConsolePanel.cs` |
| C11 | `ArrowsPanel` : libellés Nord/Sud décalés, paramètre `centred` mort, `CellWidth` inutilisée | `ArrowsPanel.cs` |
| C12 | `lastVisible` inutilisée ; zone de clic des indicateurs décalée d'environ 5 px | `TextViewer.cs` |
| C13 | Commentaire de classe périmé (cite Page↑/Page↓) | `InventoryPanel.cs` |
| C14 | `OutputType` est `Exe` (console ouverte en Release) | `Releve.Game.csproj` |
| C15 | Textes joueur avec `’` | pièces, objets, `Messages.cs`, `FlavourText.cs`, `GameApp.cs` |
| C16 | `TechDoor` et `TechReader` n'ont pas d'alias contenant « local technique » | `TechCorridorObjects.cs` |

## 10. Points à valider par l'utilisateur
1. Durée du défilement d'image (0,4 s) et sens choisis (Est : l'image entre par la droite ; Nord : par le haut).
2. Rendu du plein écran : filtre bilinéaire (légère perte de netteté du texte hors fenêtre 1280 × 720), plein écran en fenêtre sans bordure.
3. Modale de l'image du hublot : carré de 432 px, bouton « Fermer » posé sur le bas de l'image.
4. Fiche patient : bouton sélectionné au départ (« Je ne suis pas prêt à sortir »), Échap = refus, taille de 60 % × 60 %.
5. Textes provisoires du §4.
6. Taille et disposition de la fenêtre Options.
7. Icône de la fenêtre au runtime (barre de titre, barre des tâches) : seule l'icône de l'exécutable est traitée.
8. Absence de journal d'erreurs en Release (un plantage se ferme sans message).

## 11. Pistes pour la suite (hors périmètre, à titre de mémoire)
Icône de fenêtre (`SetWindowIcon` avec un PNG), journal d'erreurs, options accessibles en jeu, réglage de volume, descriptions détaillées du couloir et de l'accueil, molette de souris, fondu sonore, support Linux/macOS, lettre des parents et décision de fin (réutilisent `TextViewer` et les dialogues du V4).