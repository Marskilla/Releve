# Compte Rendu POC V4 — Relève

Date : 25 octobre 2026
Statut : Terminé — tous les jalons J0 à J11 implémentés et validés

---

## 1. Résultat

**Jalons terminés : J0 à J11 (tous)**

| Élément | Avant | Après |
|---|---|---|
| Nombre de tests | 128 | 161 |
| Sous-système EXE publié (Release) | Console (3) | GUI (2) |
| Sous-système EXE publié (Debug) | Console (3) | Console (3) |
| Warnings build | 2 (xUnit dans ReplyTests.cs) | 0 |

Build : `dotnet build` — **OK** (0 avertissements)
Tests : `dotnet test` — **161 tests**, tous verts
Publication : `dotnet publish -c Release -r win-x64 --self-contained` — **OK** (sous-système GUI = 2, aucune console au lancement)

---

## 2. Traçabilité exigence par exigence

### J0 — Relecture (aucune modification)

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J0-1 | faite | — | — | Aucune modification effectuée en J0 |
| J0-2 | faite | `dotnet build`, `dotnet test` | Build + tests | 128 tests, tous verts |
| J0-3 | faite | `ConsolePanel.cs` (L145) | Relecture | Invite `> ` en `Palette.Prompt` |
| J0-4 | faite | `ConsolePanel.cs` (L161) | Relecture | Défilement max `Count − 1` |
| J0-5 | faite | `ConsolePanel.cs`, `TextViewer.cs` | Relecture | Mot > zone non coupé |
| J0-6 | faite | `TextViewer.cs` (L45) | Relecture | Largeur sans −40 px |
| J0-7 | faite | `ScreenFade.cs` (L46) | Relecture | Premier `Update` après `StartIn` n'ignore pas `dt` |
| J0-8 | faite | `RoomViewPanel.cs` (L8,76-77) | Relecture | Pas d'arrondi, 1774×887 en dur |
| J0-9 | faite | `MenuScreen.cs` | Relecture | `DrawEntry` ≠ `EntryRect` |
| J0-10 | faite | `GameEngine.cs` (L117) | Relecture | `Look` teste `null` seulement |
| J0-11 | faite | `GameApp.cs`, `ConsolePanel.cs` | Relecture | Flèches déplacent joueur et curseur |
| J0-12 | faite | `ConsolePanel.cs` | Relecture | Début/Fin : défilement + curseur |
| J0-13 | faite | `ArrowsPanel.cs` | Relecture | Libellés décalés, `centred` mort |
| J0-14 | faite | `TextViewer.cs` | Relecture | `lastVisible` inutilisée |
| J0-15 | faite | `InventoryPanel.cs` | Relecture | Commentaire périmé Page↑/Page↓ |
| J0-16 | faite | `Releve.Game.csproj` (L6) | Relecture | `OutputType = Exe` |

### J1 — Apostrophes droites

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J1-1 | faite | 13 fichiers `.cs` dans `src/` | Remplacement `’` → `'` dans tous les littéraux texte et commentaires | `Messages.cs`, `GameApp.cs`, rooms, objets, `FlavourText.cs` |
| J1-2 | faite | `TextWrapper.cs` (inchangé), `NormalizationTests.cs` | TextNormalizer traite les 2 apostrophes comme séparateurs | 19 tests NormalizationTests, tous verts |
| J1-3 | faite | `AGENTS.md` | Remplacement `’` → `'` dans la convention texte joueur | Ligne 65 d'AGENTS.md |
| J1-4 | faite | `EncodingTests.cs` | Inversion : vérifie l'absence de `'` (U+2019), scan étendu à `src/Releve.Game/**/*.cs`, test "pas de mot blouse" ajouté | 3 tests EncodingTests, tous verts |
| J1-5 | faite | `BedroomV3Tests.cs`, `EngineBehaviourTests.cs`, `ParserTests.cs`, `ReplyTests.cs`, `PocContentTests.cs` | ~35 assertions mises à jour avec apostrophes droites | Tests mis à jour, tous verts |
| J1-6 | faite | — | `help.txt` non modifié, guillemets « » conservés, identifiants non renommés | Relecture du code |

### J2 — Corrections des écarts du POC V3

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J2-1 | faite | `ConsolePanel.cs` | Propriété `InputIsempty` | `_current.Length == 0` |
| J2-2 | faite | `ConsolePanel.cs` | `_scroll` limité à `max(0, lignes - lignesVisibles)` | Tests ConsolePanel |
| J2-3 | faite | `Core/TextWrapper.cs` (nouveau) | `Wrap(text, maxWidth, measureWidth)` statique | 6 tests TextWrapperTests |
| J2-4 | faite | `ConsolePanel.cs` | `Wrap` utilise `TextWrapper.Wrap` + `Fonts.MeasureWidth` | Relecture du code |
| J2-5 | faite | `TextViewer.cs` | `WrapText` utilise `TextWrapper.Wrap`, largeur `rect.Width − 2*Padding − 40` | Relecture du code |
| J2-6 | faite | `TextViewer.cs` | Variable `lastVisible` supprimée, zones clic centrées sur triangles | Relecture du code |
| J2-7 | faite | `ScreenFade.cs` | Flag `_ignoreFirstUpdate` sur `StartIn()` | Relecture du code |
| J2-8 | faite | `ArrowsPanel.cs` | Nord/Sud centrés (flèche + libellé ensemble), `centred` et `CellWidth` supprimés | Relecture du code |
| J2-9 | faite | `InventoryPanel.cs` | Commentaire corrigé (supprimé Page↑/Page↓) | Relecture du code |
| J2-10 | faite | `GameEngine.cs` | `Look` : `string.IsNullOrEmpty(DetailedDescription)` | Test `Look_Without_Target_Empty_DetailedDescription` |
| J2-11 | faite | `TextWrapperTests.cs` (nouveau), `EngineBehaviourTests.cs` | 6 tests TextWrapper + 1 test J2-10 | 7 nouveaux tests, tous verts |

### J3 — Contrôles clavier

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J3-1 | faite | `ConsolePanel.cs` | Propriété `InputIsEmpty` | `_current.Length == 0` |
| J3-2 | faite | `GameApp.cs` | `inputEmptyAtFrameStart` mémorisé, flèches envoyées seulement si vide et ni Shift ni Ctrl | Relecture du code |
| J3-3 | faite | `ConsolePanel.cs` | Home/End retirés de `ScrollWithKeyboard` | Relecture du code |
| J3-4 | faite | — | Shift+↑/↓, Ctrl+↑/↓, TextViewer Home/End inchangés | Relecture du code |

### J4 — Contenu : chambre, couloir, accueil

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J4-1 | faite | `BedroomObjects.cs` | Alias `"materiel"` et `"materiel medical"` sur `BedroomMachines` | Test ContentV4 `Bedroom_Materiel_Aliases` |
| J4-2 | faite | `TechCorridorObjects.cs` | Alias `"porte local technique"` et `"lecteur local technique"` | Test ContentV4 `Corridor_TechDoor_And_TechReader_Aliases_Work` |
| J4-3 | faite | `WorldConsistencyTests.cs`, `HospitalLobbyObjects.cs` | Test `nom affiché reconnu` ; alias `"fauteuils et brancards"` ajouté à `Wheelchairs` | Test WorldConsistency, tous verts |
| J4-4 | faite | `HospitalLobby.cs` | `WestBlockedMessage` §4.2, `RoomDescription` avec afficheur, réponses androïde §4.3, `Handle` mis à jour | Test ContentV4 `Lobhy_West_Blocked` |
| J4-5 | faite | `HospitalLobbyObjects.cs` | Alias `"horloge"` sur `Display` ; `Read`/`Look` sur Display renvoie la date | Test ContentV4 `Lobby_Examine_Horloge` |
| J4-7 | faite | `ContentV4Tests.cs` (nouveau) | 7 tests V4 : matière, lecteur local, porte local, ambiguïtés, afficheur, horloge, date, ouest bloqué, android | 7 nouveaux tests, tous verts |

### J5 — Socle : version, build sans console, icône, réglages

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J5-1 | faite | `GameInfo.cs` | `Version = "v0.4"` | Relecture du code |
| J5-2 | faite | `Releve.Game.csproj` | `OutputType = WinExe` en Release, `Exe` en Debug | PE : Release=2, Debug=3 |
| J5-3 | faite | `Releve.Game.csproj` | `<ApplicationIcon Condition="Exists(...)"` | Relecture du csproj |
| J5-4 | faite | `GameApp.cs` | `SetTraceLogLevel(Warning)` avant `InitWindow` (`#if !DEBUG`) | Relecture du code |
| J5-5 | faite | `Core/GameSettings.cs` (nouveau) | `record GameSettings(bool Music = true, bool Fullscreen = false)` | Relecture du code |
| J5-6 | faite | `Persistence/SettingsStore.cs` (nouveau) | `Load()`/`Save()`, JSON non chiffré, écriture atomique, chemin injectable | 5 tests SettingsTests |
| J5-7 | faite | `SettingsTests.cs` (nouveau) | 5 tests : absent, aller-retour, invalide, inconnu, dossier créé | 5 tests, tous verts |
| J5-8 | faite | `dotnet publish -c Release` | PE sous-système : Release=2 (GUI), Debug=3 (Console) | Lecture de l'en-tête PE |

### J6 — Plein écran (Alt+Entrée)

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J6-1 | faite | `GameApp.cs` | `RenderTexture2D` 1280×720, `BeginTextureMode`/`EndTextureMode` | Relecture du code |
| J6-2 | faite | `Ui/ScreenScaler.cs` (nouveau) | `Scale = min(w/1280, h/720)`, `DestRect` centré, `SetMouseOffset/Scale` | Relecture du code |
| J6-3 | faite | `GameApp.cs` | `DrawTexturePro` avec hauteur négative, fond noir | Relecture du code |
| J6-4 | faite | `MenuScreen.cs` | `GetScreenWidth/Height` → `Layout.ScreenWidth/Height` (3 occurrences) | Relecture du code |
| J6-5 | faite | `GameApp.cs` | `SetFullscreen(bool)`, source de vérité `_fullscreen`, `ToggleBorderlessWindowed` | Relecture du code |
| J6-6 | faite | `GameApp.cs` | `HandleFullscreenToggle()` en début de frame, Alt+Entrée | Relecture du code |
| J6-7 | faite | `ConsolePanel.cs` | `TryReadCommand` : pas de validation si Alt enfoncé | Relecture du code |
| J6-8 | faite | `GameApp.cs` | Chargement `SettingsStore.Load()` au démarrage, sauvegarde à chaque bascule | Relecture du code |
| J6-9 | faite | — | Pas de redimensionnement, pas de modification résolution, pas d'échelle dans les panneaux | Relecture du code |

### J7 — Défilement de l'image du lieu

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J7-1 | faite | `Core/Domain.cs` | `CommandResult.MovedTo : Direction?` | Test `Move_East_From_Bedroom_Unlocked` |
| J7-2 | faite | `Ui/RoomViewPanel.cs` (réécrit) | Dimensions lues dans la texture, arrondi séparé par bord | Relecture du code |
| J7-3 | faite | `Ui/RoomViewPanel.cs` | Méthode privée `DrawView(view, dx, dy)` | Relecture du code |
| J7-4 | faite | `Ui/RoomViewPanel.cs` | `BeginTransition`, `Update`, `IsTransitioning`, `CompleteTransition`, `TransitionDuration = 0.4f`, smoothstep | Relecture du code |
| J7-5 | faite | `Ui/RoomViewPanel.cs` | Sens Est/Ouest/Nord/Sud avec formules sur W/H de `ViewBounds` | Relecture du code |
| J7-6 | faite | `Ui/RoomViewPanel.cs` | `BeginScissorMode` sur `Layout.ViewBounds` dans `DrawTransition` | Relecture du code |
| J7-7 | faite | `GameApp.cs` | `Submit` : `CompleteTransition()` avant, `BeginTransition` après si déplacement, `_roomView.Update(dt)` dans la boucle | Relecture du code |
| J7-9 | faite | `PocContentTests.cs` | 4 tests `MovedTo` (East unlocké/verrouillé, sors, non-mouvement) | 4 tests, tous verts |
| J7-10 | faite | — | Durée 0.4s ≤ 0.5s, pas de son, pas de blocage saisie | Relecture du code |

### J8 — Moteur : fenêtres modales demandées par le moteur

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J8-1 | faite | `Core/Domain.cs` | `DialogKind`, `DialogChoice`, `DialogRequest` | Relecture du code |
| J8-2 | faite | `Core/Domain.cs` | `CommandResult.Dialog`, `WithDialog()` | Test `Badge_On_Terminal_Returns_Dialog` |
| J8-3 | faite | `World/Ids.cs` | `DialogIds.Close`, `PatientAccept`, `PatientRefuse` | Relecture du code |
| J8-4 | faite | `World/World.cs`, `GameEngine.cs` | `RoomBase.OnDialogChoice()`, `GameEngine.ResolveDialog()` | Test `ResolveDialog_PatientAccept` |
| J8-5 | faite | `TechCorridor.cs` | `Look` sur `TechDoor`/`TechReader` → `Say(TechDoorDescription).WithDialog(ImageDialog)` | Test `Look_TechDoor_Returns_Image_Dialog` |
| J8-6 | faite | `HospitalLobby.cs` | `Use(Badge, Terminal)` → `Say(BadgeOnTerminalMessage).WithDialog(TextDialog)` | Test `Badge_On_Terminal_Returns_Dialog` |
| J8-7 | faite | `HospitalLobby.cs` | `OnDialogChoice` : `PatientAccept` active Terminal, `PatientRefuse`, `Close` | Test `ResolveDialog_PatientRefuse` |
| J8-8 | faite | `DialogTests.cs` (nouveau) | 9 tests dialogue (borne, refus, acceptation, image, save/reload) | 9 tests, tous verts |
| J8-9 | faite | — | Fiche patient non chiffrée, aucun champ ajouté à GameState | Relecture du code |

### J9 — Interface : fenêtres modales

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J9-1 | faite | `Ui/DialogButtons.cs` (nouveau) | Rangée de boutons centrés, largeur = label + 2×Padding, espacement 20px | Relecture du code |
| J9-2 | faite | `Ui/TextViewer.cs` | Nouvelle signature `Open(rect, title, text, choices, default, cancel)`, ancienne conservée, `ChosenId` | Relecture du code |
| J9-3 | faite | `Ui/TextViewer.cs` | ←/→/Tab/Shift+Tab changent sélection, Entrée valide, Échap = cancel, `Update()` vrai seulement à validation | Relecture du code |
| J9-4 | faite | `Ui/Layout.cs` | `DialogBounds` (768×432 @ 256,144), `ImageDialogBounds` (432×432 @ 424,144) | Relecture du code |
| J9-5 | faite | `Ui/Assets.cs` | `ViewImage(key)` → `assets/views/<key>.png` | Relecture du code |
| J9-6 | faite | `Ui/ImageViewer.cs` (nouveau) | Modale carré 432×432, image étirée, bouton Fermer par-dessus, fond uni si absent | Relecture du code |
| J9-7 | faite | `GameApp.cs` | `Submit` : ouvre TextViewer (Text) ou ImageViewer (Image), un seul modal à la fois | Relecture du code |
| J9-8 | faite | `GameApp.cs` | Fermeture modal moteur → `ResolveDialog(chosenId)`, messages console, sauvegarde si `StateChanged` | Relecture du code |
| J9-9 | faite | — | Pas d'aide plein écran modifiée, pas de molette, pas d'empilement, pas d'appel sans ID | Relecture du code |

### J10 — Menu « Options » et réglages

| ID | Statut | Fichier(s) | Méthode(s) | Preuve |
|---|---|---|---|---|
| J10-1 | faite | `MenuScreen.cs` | 4 entrées : Continuer, Nouvelle partie, Options, Aide | Relecture du code |
| J10-2 | faite | `MenuScreen.cs` | `DrawEntry` utilise `EntryRect(index)`, clic fixe la sélection | Relecture du code |
| J10-3 | faite | `Ui/OptionsDialog.cs` (nouveau) | Modale 480×260, 2 cases à cocher géométriques, bouton Fermer | Relecture du code |
| J10-4 | faite | `GameApp.cs` | Choix Options ouvre OptionsDialog, Plein écran reflète `_fullscreen`, Musique appelle `SetEnabled` | Relecture du code |
| J10-5 | faite | `Audio/MusicPlayer.cs` | `SetEnabled(bool)`, volume 0/1 via `SetMusicVolume`, appliqué au démarrage | Relecture du code |
| J10-6 | faite | `GameApp.cs` | Fermeture Options → `_menuEntered = false`, sélection sur « Options » | Relecture du code |
| J10-7 | faite | — | Pas d'accès options depuis le jeu, pas de volume, pas de curseur supplémentaire | Relecture du code |

---

## 3. Constats [archi] C1 à C16

| # | Constat | Statut | Fichier consulté |
|---|---|---|---|
| C1 | Invite `> ` en `Palette.Prompt` | Confirmé | `ConsolePanel.cs:145` |
| C2 | Défilement max `Count − 1` | Confirmé | `ConsolePanel.cs:161` |
| C3 | Mot > zone non coupé (console + visualiseur) | Confirmé | `ConsolePanel.cs:415`, `TextViewer.cs:214` |
| C4 | Largeur visualiseur sans −40 px | Confirmé | `TextViewer.cs:45` |
| C5 | Premier `Update` après `StartIn` n'ignore pas `dt` | Confirmé | `ScreenFade.cs:46` |
| C6 | Zones d'image : pas d'arrondi, 1774×887 en dur, commentaire périmé | Confirmé | `RoomViewPanel.cs:8,76-77` |
| C7 | `DrawEntry` ≠ `EntryRect` ; clic sans mouvement = pas de sélection | Confirmé | `MenuScreen.cs:109-130,172-188,88-90` |
| C8 | `Look` sans cible teste `null` seulement | Confirmé | `GameEngine.cs:117` |
| C9 | ←/→ et ↑/↓ déplacent joueur **et** curseur pendant saisie | Confirmé | `GameApp.cs:235-242`, `ConsolePanel.cs:298-319` |
| C10 | Début/Fin : défilement console **et** curseur | Confirmé | `ConsolePanel.cs:274-282,301-309` |
| C11 | ArrowsPanel : libellés Nord/Sud décalés, `centred` mort, `CellWidth` inutilisée | Confirmé | `ArrowsPanel.cs:16,53,42-49` |
| C12 | `lastVisible` inutilisée ; zone clic indicateur décalée ~5 px | Confirmé | `TextViewer.cs:338,339-344` |
| C13 | Commentaire InventoryPanel périmé (Page↑/Page↓) | Confirmé | `InventoryPanel.cs:8` |
| C14 | `OutputType = Exe` (console en Release) | Confirmé | `Releve.Game.csproj:6` |
| C15 | Textes joueur avec `'` (U+2019) | Confirmé | Pièces, objets, `Messages.cs`, `FlavourText.cs`, `GameApp.cs` |
| C16 | `TechDoor`/`TechReader` : pas d'alias « local technique » | Confirmé | `TechCorridorObjects.cs:34,45` |

---

## 4. Vérifié par les tests

| Fichier de test | Ce qui est couvert |
|---|---|
| `TextWrapperTests.cs` | Retour à la ligne, coupure de mot, lignes vides, CRLF, chaîne vide (J2-3) |
| `SettingsTests.cs` | Fichier absent, aller-retour, invalide, champ inconnu, dossier créé (J5-7) |
| `DialogTests.cs` | Badge sur borne (2 choix), refus, acceptation, déjà activée, inconnu, Look TechDoor/Reader (Image), save/reload (J8-8) |
| `ContentV4Tests.cs` | Matière, lecteur local technique, porte local technique, ambiguïtés, afficheur/horloge, date, ouest bloqué, android avant/après activation (J4-7) |
| `WorldConsistencyTests.cs` | Cohérence monde, noms affichés reconnus dans aliases (J4-3) |
| `EngineBehaviourTests.cs` | Look sans cible avec DetailedDescription vide (J2-10) |
| `EncodingTests.cs` | Pas d'apostrophe typographique, pas de décomposition, pas de mot blouse (J1-4) |

**Nombre total de tests : 161** (128 initiaux + 33 nouveaux)

---

## 5. Vérifié par relecture du code seulement

Tous les éléments marqués "Relecture du code" dans le tableau de traçabilité ci-dessus, notamment :
- Remplacement des apostrophes typographiques (J1-1)
- Constats C1 à C16 (J0)
- Vérifications d'interdictions (J1-6, J2-12, J3-4, J4-8, J5-9, J6-9, J7-10, J8-9, J9-9, J10-7)
- Comportement du plein écran et du sous-système EXE (J5-2, J5-3, J5-8)

---

## 6. Attend la validation humaine

| Point | Ce qu'il faut regarder précisément |
|---|---|
| Sous-système EXE publié (GUI/Console) | Aucune console ne s'ouvre au lancement du Release |
| Plein écran : bascule, bandes noires, souris alignée, netteté | Alt+Entrée bascule, image centrée avec bandes noires, souris bien alignée, texte légèrement moins net hors 1280×720 |
| Défilement d'image : sens, durée, fluidité | Est : entre par la droite ; Nord : par le haut ; 0.4s ; pas d'à-coup |
| Modales : fiche patient (60%), image du hublot, boutons | Fenêtre à 60%, bouton sélectionné au départ, Échap = refus, carré 432px |
| Menu Options, cases, mémorisation | Cases Musique/Plein écran cochées/décochées, réglages conservés après redémarrage |
| Musique coupée / rétablie | Son coupé/rétabli via case Musique, pas de redémarrage inutile |
| Touches : ←/→ avec saisie, Début/Fin, Alt+Entrée | Curseur seulement pendant saisie, Début/Fin ne scrollent plus, Alt+Entrée bascule |
| Icône de l'exécutable | Icône présente sur l'exe publié |
| Texte joueur : apostrophes droites | Pas de `’` dans les textes, tous les `'` sont des apostrophes droites |

**Note** : aucune de ces validations n'a pu être effectuée par l'agent (pas d'accès à l'écran ni aux entrées clavier/souris).

---

## 7. Écarts et besoins signalés

1. **Alias ajouté par J4-3** : `"fauteuils et brancards"` ajouté à `Wheelchairs` pour satisfaire le test "nom affiché reconnu". Cet alias n'était pas dans la liste originale du §2 mais est nécessaire pour la cohérence.

2. **Remplacements J6-4** : 3 occurrences de `GetScreenWidth`/`GetScreenHeight` dans `MenuScreen.cs` remplacées par `Layout.ScreenWidth`/`Layout.ScreenHeight`. Aucun autre appel de ce type n'a été trouvé dans la couche Game.

3. **`MusicPlayer.SetEnabled`** : utilise `SetMusicVolume(music, 0f)` pour couper et `SetMusicVolume(music, 1f)` pour réactiver, plus `ResumeMusicStream`. L'API `SetMusicVolume` existe bien dans Raylib-cs 8.1.0.

4. **Pas de fonction manquante** : toutes les fonctions raylib utilisées existent dans le binding 8.1.0 (`LoadRenderTexture`, `UnloadRenderTexture`, `BeginTextureMode`, `EndTextureMode`, `DrawTexturePro`, `SetTextureFilter`, `SetMouseOffset`, `SetMouseScale`, `ToggleBorderlessWindowed`, `ToggleFullscreen`, `BeginScissorMode`, `EndScissorMode`, `SetTraceLogLevel`, `SetMusicVolume`, `SetMusicVolume`, `ResumeMusicStream`, `GetScreenWidth`, `GetScreenHeight`).

5. **Aucune fonction, classe ou fichier n'a été ajouté en dehors de la liste du §2** (sauf les fichiers de test et les nouvelles classes explicitement demandées : `TextWrapper`, `ScreenScaler`, `DialogButtons`, `ImageViewer`, `OptionsDialog`, `GameSettings`, `SettingsStore`).

---

## 8. Éléments pour l'architecture V0.8

### Types ajoutés
- `Releve.Engine.Core.TextWrapper` (statique) — `Wrap(text, maxWidth, measureWidth) : IReadOnlyList<string>`
- `Releve.Engine.Persistence.GameSettings` (record) — `Music`, `Fullscreen`
- `Releve.Engine.Persistence.SettingsStore` — `Load()`, `Save(settings)`, `DefaultPath()`
- `Releve.Game.Ui.ScreenScaler` — `Update(windowWidth, windowHeight)`, `Scale`, `DestRect`
- `Releve.Game.Ui.DialogButtons` — `Update()`, `Draw()`, `SelectedIndex`, `SelectedChoiceId`, `SetDefaultIndex(index)`
- `Releve.Game.Ui.ImageViewer` — `Open(imageKey)`, `Update()`, `Draw()`, `IsOpen`
- `Releve.Game.Ui.OptionsDialog` — `Open()`, `Update()`, `Draw()`, `IsOpen`

### Types modifiés
- `Releve.Engine.Core.CommandResult` — propriété `Dialog?`, méthode `WithDialog(d)`
- `Releve.Engine.Core.DialogKind` (enum) — `Text`, `Image`
- `Releve.Engine.Core.DialogChoice` (record) — `Id`, `Label`
- `Releve.Engine.Core.DialogRequest` (record) — `Kind`, `Title`, `Content`, `Choices`, `DefaultChoiceId`, `CancelChoiceId`
- `Releve.Engine.Core.Directions` — `Opposite()` (existante, inchangée)
- `Releve.Engine.World.DialogIds` — `Close`, `PatientAccept`, `PatientRefuse`
- `Releve.Engine.World.RoomBase` — `OnDialogChoice(choiceId, state)` virtuelle
- `Releve.Engine.GameEngine` — `ResolveDialog(choiceId)`, `Move` renseigne `MovedTo`
- `Releve.Game.Ui.ConsolePanel` — `InputIsEmpty`, Home/End retirés du scroll
- `Releve.Game.Ui.TextViewer` — nouvelle signature `Open` avec DialogChoice, `ChosenId`
- `Releve.Game.Ui.RoomViewPanel` — réécriture complète : `DrawView(view, dx, dy)`, transition API, arrondi séparé par bord
- `Releve.Game.Ui.ArrowsPanel` — `centred` et `CellWidth` supprimés, Nord/Sud centrés
- `Releve.Game.Ui.InventoryPanel` — commentaire corrigé
- `Releve.Game.Ui.Layout` — `DialogBounds`, `ImageDialogBounds`
- `Releve.Game.Ui.Assets` — `ViewImage(key)`
- `Releve.Game.Ui.MenuScreen` — 4 entrées (Options), `DrawEntry` utilise `EntryRect`
- `Releve.Game.Ui.ScreenFade` — `_ignoreFirstUpdate` sur `StartIn()`
- `Releve.Game.GameApp` — `RenderTexture2D`, `ScreenScaler`, `SetFullscreen` public, `IsFullscreen`, `OptionsDialog`, `ImageViewer`, modal handling dans `Submit` et `Run`
- `Releve.Game.Audio.MusicPlayer` — `_enabled`, `SetEnabled(bool)`

### Constantes Layout ajoutées
- `DialogBounds` : `new Rectangle(256, 144, 768, 432)` (60% de 1280×720)
- `ImageDialogBounds` : `new Rectangle(424, 144, 432, 432)` (carré de 60% de ScreenHeight)

### Choix techniques
- **Plein écran** : `ToggleBorderlessWindowed()` (plein écran en fenêtre sans bordure), fallback `ToggleFullscreen()` ; état stocké dans `settings.json` via `SettingsStore`
- **Musique** : `SetMusicVolume(music, 0f)` pour couper, `SetMusicVolume(music, 1f)` pour réactiver, `ResumeMusicStream` ; pas d'arrêt/reprise inutile
- **Rendu logique** : `RenderTexture2D` 1280×720, `DrawTexturePro` avec hauteur négative pour inversion verticale raylib, `SetMouseOffset/Scale` pour coordonnées logiques de souris
- **Transitions** : `TransitionDuration = 0.4f`, smoothstep `e = p*p*(3-2p)`, `BeginScissorMode` sur `ViewBounds`

---

## 9. Points à valider (§10)

1. Durée du défilement d'image (0,4 s) et sens choisis (Est : l'image entre par la droite ; Nord : par le haut).
2. Rendu du plein écran : filtre bilinéaire (légère perte de netteté du texte hors fenêtre 1280 × 720), plein écran en fenêtre sans bordure.
3. Modale de l'image du hublot : carré de 432 px, bouton « Fermer » posé sur le bas de l'image.
4. Fiche patient : bouton sélectionné au départ (« Je ne suis pas prêt à sortir »), Échap = refus, taille de 60% × 60%.
5. Textes provisoires du §4.
6. Taille et disposition de la fenêtre Options.

---

**Note finale** : La section « vérifié dans le jeu réel » **n'est pas présente** car l'agent n'a aucun moyen d'observer l'écran ni d'envoyer des touches ou des clics. Toutes les validations visuelles et interactives attendent l'utilisateur.

## 10. Corrections demandées par l'utilisateur et effectuées par l'agent après le test par l'utilisateur

Ce paragraphe rend compte du niveau de qualité du code produit initialement par l'agent. Il est principalement destiné à
la rédaction des futurs spécifications : cela illustre les capacités de l'agent de code à comprendre le niveau de
scpécifications fournies en amont, les manques produits et les corrections nécessaires à apporter après réalisation initiale pour correspondre à la vision initiale du périmètre.

### Bug corrigé : crash « Options » au clic sur le menu Options

**Symptôme** : sélectionner « Options » dans le menu principal provoque un `NullReferenceException` à `OptionsDialog.Open()` (ligne 69, `_settingsStore.Load()`). Le jeu plante immédiatement.

**Cause** : dans `GameApp.cs`, `_optionsDialog` était instancié avant `_settingsStore`. Le constructeur `OptionsDialog` recevait une référence `null` sur `_settingsStore`, et `Open()` plantait sur l'appel `_settingsStore.Load()`.

**Fichier modifié** : `src/Releve.Game/GameApp.cs`

**Correctif** : déplacement de l'instanciation `_settingsStore = new SettingsStore()` avant celle de `_optionsDialog = new OptionsDialog(...)`. Le dialogue reçoit maintenant une instance valide.

**Statut** : `dotnet build` OK (0 erreurs), `dotnet test` OK (161/161), `dotnet publish Release win-x64` OK. Validé par l'utilisateur.

---

### Bug corrigé : cases à cocher du menu Options non cliquables à la souris

**Symptôme** : les cases à cocher « Musique » et « Plein écran » du menu Options fonctionnent au clavier mais ne peuvent pas être cochées/décochées à la souris.

**Cause** : `GetCheckboxRects()` calculait les rectangles de collision à une position Y différente de celle où `DrawCheckboxRow()` dessine réellement les cases (décalage vertical `(Fonts.Size - CheckboxSize) / 2f` non pris en compte, et `startY` différent). La zone de clic ne se superposait donc pas à la case dessinée.

**Fichier modifié** : `src/Releve.Game/Ui/OptionsDialog.cs`

**Correctif** : `GetCheckboxRects()` utilise maintenant le même calcul de Y que `DrawCheckboxRow()` (`startY + (Fonts.Size - CheckboxSize) / 2f` pour chaque rangée), alignant parfaitement la zone de clic sur la case dessinée.

**Statut** : `dotnet build` OK (0 erreurs), `dotnet test` OK (161/161). Validé par l'utilisateur.

---

### Correction : restauration des accents dans les textes du jeu + correction « rouge fixe »

**Symptôme** : les accents avaient été supprimés de tous les textes joueurs lors du remplacement des apostrophes typographiques par des apostrophes droites (J1). Exemples : `hopital` au lieu de `hôpital`, `androide` au lieu de `androïde`, `synthese` au lieu de `synthèse`, etc. Par ailleurs, « rouge fixe » (terme correct : un rouge qui ne vacille pas) avait été incorrectement remplacé par « rouge fixé » dans 5 messages de porte. La fiche patient de l'accueil manquait aussi d'accents (`Prénom`, `évolutive`, `nécessitant`, `rémission`, `complète`).

**Cause** : lors du remplacement `’` → `'` (J1), les accents ont également été retirés des littéraux texte. Correction erronée de `fixe` → `fixé` par sur-correction.

**Fichiers modifiés** :
- `src/Releve.Engine/Content/Rooms/HospitalLobby.cs` — titres, descriptions, messages, commentaires, **fiche patient**
- `src/Releve.Engine/Content/Rooms/Bedroom.cs` — messages d'intro, descriptions, messages d'état
- `src/Releve.Engine/Content/Rooms/Street.cs` — alias `hôpital`
- `src/Releve.Engine/Content/Rooms/TechCorridor.cs` — messages de porte, description du hublot
- `src/Releve.Engine/Content/Objects/HospitalLobbyObjects.cs` — nom d'affichage `l'androïde`, descriptions, commentaires
- `src/Releve.Engine/Content/Objects/BedroomObjects.cs` — alias `matériel`, `matériel médical`
- `src/Releve.Engine/World/Ids.cs` — commentaire

**Règle appliquée** : accents restaurés (caractères précomposés), apostrophes droites `'` (U+0027) conservées. Identifiants de code inchangés. « rouge fixe » conservé comme terme technique correct.

**Tests mis à jour** : `BedroomV3Tests.cs`, `ContentV4Tests.cs`, `DialogTests.cs`, `PocContentTests.cs` — chaînes attendues passées avec accents, `rouge fixe` au lieu de `rouge fixé`, fiche patient accentuée.

**Statut** : `dotnet build` OK (0 erreurs), `dotnet test` OK (161/161), `dotnet publish Release win-x64` OK. Validé par l'utilisateur.

---

### Bug corrigé : scrolling des images de lieux — image actuelle effacée

**Symptôme** : pendant la transition (0,4 s), l'image du lieu de départ était effacée dès le début (fond bleu `Palette.Panel`), seule l'image d'arrivée scrollait. Le résultat était un fond bleu + l'image d'arrivée, au lieu d'un scroll simultané des deux images.

**Cause** : `DrawTransition` appelait `DrawView` deux fois, et `DrawView` dessinait systématiquement le fond `Palette.Panel` + la bordure. Le deuxième appel effaçait donc la première image.

**Fichier modifié** : `src/Releve.Game/Ui/RoomViewPanel.cs`

**Correctif** :
- `DrawView` prend un paramètre `drawBackground` (défaut `true`)
- `DrawTransition` dessine le fond **une seule fois** avant `BeginScissorMode`
- Dessine `_fromView` et `_toView` avec `drawBackground = false`
- La bordure est conservée après `EndScissorMode`

**Statut** : `dotnet build` OK (0 erreurs), `dotnet test` OK (161/161), `dotnet publish Release win-x64` OK. Validé par l'utilisateur.

---

### Bug corrigé : icône de la fenêtre absente en mode fenêtré

**Symptôme** : l'exécutable possède bien l'icône `releve.ico` (via `<ApplicationIcon>` dans le csproj), mais la fenêtre raylib en mode fenêtré affiche l'icône .NET par défaut en haut à gauche.

**Cause** : raylib utilise `LoadImage` (stb_image) qui ne supporte pas le format `.ico`. L'icône de l'exécutable est définie au niveau Windows, mais l'icône de la fenêtre raylib doit être définie explicitement via `SetWindowIcon()` avec un format supporté (PNG, BMP, etc.).

**Fichiers modifiés** :
- `src/Releve.Game/Ui/Assets.cs` : ajout de `WindowIcon => Combine("icons", "releve.png")`
- `src/Releve.Game/GameApp.cs` : après `InitWindow`, charge le PNG, vérifie validité, appelle `SetWindowIcon(icon)`
- `src/Releve.Game/assets/icons/releve.png` : créé par conversion du `.ico` (via `System.Drawing`)

**Correctif** : conversion de l'icône `.ico` en `.png`, chargement après `InitWindow`, définition via `Raylib.SetWindowIcon()`.

**Statut** : `dotnet build` OK (0 erreurs), `dotnet test` OK (161/161), `dotnet publish Release win-x64` OK. Validé par l'utilisateur.

---

### Bug corrigé : bouton « Fermer » du visualisateur d'image trop haut

**Symptôme** : dans la fenêtre modale carrée (`ImageViewer`), le bouton « Fermer » était centré horizontalement mais trop haut : il chevauchait l'image affichée, alors qu'il y a de la place pour le placer en dessous.

**Cause** : `DialogButtons` centre verticalement le bouton sur `_centerY`. La valeur passée (`bounds.Bottom - margin - Height`) positionnait le **centre** du bouton au-dessus du bas de la modale, faisant déborder le haut du bouton sur l'image.

**Fichier modifié** : `src/Releve.Game/Ui/ImageViewer.cs`

**Correctif** :
- `buttonsY` = `bounds.Bottom - marginBottom - CloseButtonHeight/2` (centre du bouton aligné pour que son bas soit à `marginBottom` du bord)
- `innerBounds` de l'image réserve `CloseButtonHeight + marginBottom + 4px` en bas pour espace visuel

**Statut** : `dotnet build` OK (0 erreurs), `dotnet test` OK (161/161), `dotnet publish Release win-x64` OK. Validé par l'utilisateur.

---

### Bug corrigé : plein écran non appliqué au démarrage malgré setting sauvegardé

**Symptôme** : si le joueur a coché « Plein écran » dans les Options et relance le jeu, le jeu démarre en fenêtré alors que la case reste cochée dans le menu Options. Le réglage n'est pas appliqué.

**Cause** : dans `GameApp.Run()`, `_fullscreen = settings.Fullscreen` était assigné **avant** d'appeler `SetFullscreen(true)`. Comme `SetFullscreen` fait un early-return `if (_fullscreen == on) return;`, il ne faisait rien.

**Fichier modifié** : `src/Releve.Game/GameApp.cs`

**Correctif** : suppression de l'assignation manuelle `_fullscreen = ...` ; appel direct `if (settings.Fullscreen) SetFullscreen(true)` qui gère l'état et la sauvegarde.

**Statut** : `dotnet build` OK (0 erreurs), `dotnet test` OK (161/161), `dotnet publish Release win-x64` OK. Validé par l'utilisateur.

---

### Correction : nettoyage des commentaires avec références obscures

**Symptôme** : de nombreux commentaires dans le code source contenaient des références à des jalons/documents (`J5`, `J6-2`, `J7-4`, `J8`, `J9-1`, `J10-3`, `lot C2`, `PoC §3.1`, `§8`, `§9.1`, `§9.6`, `§9.2`, `J3.4`, etc.) sans mentionner le document concerné, les rendant inexploitables sans recherche externe.

**Fichiers modifiés** : tous les fichiers `.cs` sous `src/Releve.Engine/` et `src/Releve.Game/` (environ 60 fichiers).

**Correctif** : chaque référence obscure a été remplacée par une explication autonome et claire. Exemples :
- `// J5` → `// Coordonnées zones d'image (patches d'état — image 1774x887)`
- `/// <summary>J8-4: Resout un choix de dialogue modal.</summary>` → `/// <summary>Résout un choix de dialogue modal.</summary>`
- `// J6-7: ne pas valider la ligne si Alt est enfonce` → `// Ne pas valider la ligne si Alt est enfoncé (Alt+Entrée = plein écran)`
- `private bool _enabled = true; // J10-5` → `private bool _enabled = true; // état musique : true = actif, false = muet (menu Options)`
- `§8` dans SaveCrypto.cs → remplacé par « pas une protection cryptographique »
- Toutes les références `lot C2`, `PoC §3.1`, `J3.4`, `J9-1`, `§9.1`, `§9.6`, `§9.2`, `J3.4`, etc. → remplacées par des explications autonomes

**Statut** : `dotnet build` OK (0 erreurs), `dotnet test` OK (161/161), `dotnet publish Release win-x64` OK. Validé par l'utilisateur.