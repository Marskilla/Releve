# AGENTS.md

Repo-specific notes for working in **Relève** (C# text/graphic adventure). Read before changing code.

## Layout & entrypoints

- `src/Releve.Engine/` — pure game engine: world, parser, state, save. **Must never reference raylib or any display/audio library.** Tests only cover this project.

- `src/Releve.Game/` — raylib UI. Entry point: `src/Releve.Game/Program.cs` → `GameApp.Run()` (loop in `GameApp.cs`, panels in `Ui/`).

- `tests/Releve.Engine.Tests/` — the only test project. Includes its own world (`TestWorld/TestWorld.cs`) independent of the real content.

- Docs: `ArchitectureTechnique.md` (V0.6) is the spec and mirrors the shipped code; `NoteIntentionJeuAventure.md` (V6) governs UI/behaviour and **wins on conflicts** there. `CompteRenduPOCV0.1.md` records what was built and verified (shipped world: 4 rooms, 22 objects, 83 tests); `ImplementationPOCV0.1.md` (that plan) is now implemented — later phases come from `UniversScenarioLoreJeuAventure.md` (v0.8), do not implement them yet. PoC V2 documents live under `Docs/POC_V2/`; PoC V1 under `Docs/POC_V1/`; **PoC V3 under `Docs/POC_V3/`** (implementation plan, compte rendu); **PoC V4 under `Docs/POC_V4/`** (implementation plan, compte rendu, version v0.4).

## Commands

```powershell

dotnet build

dotnet test                       # builds everything, runs xunit

dotnet run --project src/Releve.Game

dotnet publish src/Releve.Game -c Release -r win-x64 --self-contained

dotnet test --filter "FullyQualifiedName~NormalizationTests"   # focused run

```

- The game build output is under `bin/Debug/net8.0/**win-x64**/` (fixed `RuntimeIdentifier` in the csproj), not `net8.0/`. `.vscode/launch.json` depends on this path.

- **Stop the running `Releve.Game` process before rebuilding** — it locks `Releve.Game.exe` and the build fails with MSB3027.

- There is no CI, linter, formatter, or typecheck step. `dotnet build` (with `Nullable=enable` from `Directory.Build.props`) is the only static gate. Keep build + tests green after each change.

- Package versions are central-managed in `Directory.Packages.props`; do not add a version inside a csproj.

## Agent tools

- `agentTools/` is the exclusive location for temporary tools and working files created by the coding agent. Whenever the agent creates a script, utility, standalone code snippet, temporary project, test harness, generated input/output, log, or other intermediate artifact to inspect, modify, generate, migrate, or test files, it must put that artifact under `agentTools/`. This includes PowerShell scripts (`.ps1`), temporary C#/.NET code or projects, helper scripts, and one-off test files.

- Do not create agent-generated temporary files anywhere else. Do not place them in the repository root, source folders (`src/`), test folders (`tests/`), documentation or asset folders, the system temp directory, the user's profile, or any other location on the local disk. Use a subdirectory inside `agentTools/` when separation is useful; create it there rather than elsewhere.

- Temporary tools must remain isolated from the application and test projects: do not add them to solution or project files, do not let them become part of the normal build/publish output, and do not change the project's build or test configuration just to run them. The existing rule that no standalone `.cs` files belong directly in `agentTools/` remains in force; put temporary .NET code in an appropriately isolated subdirectory/project under `agentTools/`, and ensure it is not included by the application's or test projects' compile globs.

- Before finishing, remove disposable files created under `agentTools/` when they are no longer needed. Keep only reusable agent tooling, documented scenarios, and files intentionally maintained as part of `agentTools/`. Never delete or overwrite existing user files or established tools there just because they appear temporary.

- Existing reusable automation for driving the real game window (keyboard input, screenshots, arrow-state pixel checks) also lives in `agentTools/` and is not part of the build. See `agentTools/README.md`.

- Recipe: start the game, `Import-Module ./agentTools/ReleveGameTools.psm1`, `Wait-ReleveGameWindow`, then `Send-ReleveGameCommands -CommandFile ./agentTools/scenarios/poc-reference.txt`. `scenarios/quit-and-save.txt` sends `quitter` to exercise the save-on-quit menu.

- **Input must run from a no-window process** — a visible PowerShell steals the foreground and injection never reaches the game. Characters need `SendKeys`; `SendInput` (`wVk` or `KEYEVENTF_UNICODE`) and `PostMessage(WM_CHAR)` do not reach the console input. Focus needs `AttachThreadInput` + `SetForegroundWindow` (see `Set-ReleveGameFocus`/`Wait-ReleveGameFocus`); a synthetic mouse click once made the window invisible.

- **Enter/Backspace need a *held* `keybd_event` pulse, not `SendKeys`.** `Ui/ConsolePanel.cs` reads them with `IsKeyPressed`, an edge-triggered keyboard state: an instantaneous keydown+keyup (`SendKeys.SendWait '{ENTER}'`) that lands entirely between two 60 Hz frames leaves no edge and is silently dropped — consecutive commands end up glued on one line and the engine answers « Vous ne voyez pas cela ici. ». The module's internal `Invoke-ReleveKeyPulse` helper (used by `Send-ReleveGameEnter`/`Send-ReleveGameCommands`) holds the key ~60-80 ms. Characters are unaffected (they go through raylib's char queue). Also in `Raylib-cs gotchas` below.

- **Backspace repetition validation**: `Ui/ConsolePanel.cs` now uses `IsKeyPressedRepeat(KeyboardKey.Backspace)` (and `Delete`), but **this cannot be validated by `agentTools/` as a real keyboard** : classify as « validation humaine » (§7.3 du document PoC V2). Document this limit in `agentTools/README.md` and in the architecture §16.

- Command files must be read as UTF8 (PS 5.1 defaults to ANSI and mangles accents), and `Get-ReleveArrowStates` zones are derived from `Ui/ArrowsPanel.cs`/`Layout.cs` constants — recalibrate if that panel's layout changes.

- Keyboard/layout note: raylib's char queue is capped at 16 codepoints per frame (`MAX_CHAR_PRESSED_QUEUE` in raylib's `rcore.c`); one character per keystroke with ~700 ms between commands is fine.

## Conventions that differ from defaults

- **Player-facing text is French and must be written with precomposed characters and the typographic apostrophe `'`** (e.g. `"Vous l'avez pas sur vous."`, not `n'est`, not decomposed `é`). raylib renders each codepoint independently, so decomposed accents (`e` + U+0301) display garbage. Verify with a regex scan for `[\u0300-\u036f]` after editing text. Identifiers stay English; room text lives in `private const` fields at the top of the class.

- Errors are neutral, present-tense, and never attribute thoughts to the player (see `Core/Messages.cs` and §6.7 of the architecture doc).

- Room/object/enum ids are declared in `src/Releve.Engine/World/Ids.cs`. Adding a room means: enum member + class in `Content/Rooms/` + registration in `Content/GameWorld.cs` + update consistency tests.

## Raylib-cs 8.1.0 gotchas (verified, not from memory)

- `IsFontValid(Font)` / `IsTextureValid(Texture2D)` — there is no `IsFontReady`/`IsTextureReady`.

- `char.ConvertFromUtf32(int)` (lowercase "tf32").

- `Music.looping` is a struct field (`music.Looping = true`); there is no `SetMusicLooping`.

- **Keyboard injection into the game window: `SendKeys` for characters, *held* `keybd_event` for state keys.** `SendInput` (`wVk` or `KEYEVENTF_UNICODE`) and `PostMessage(WM_CHAR)` do not reach the console input, contrary to what was previously noted here. Enter/Backspace are read with `IsKeyPressed` (edge-triggered): an instantaneous keydown+keyup landing between two 60 Hz frames is silently dropped, so the module's internal `Invoke-ReleveKeyPulse` helper holds the key 60-80 ms before releasing. A held pulse also fixes main-menu validation (`MenuScreen` uses `IsKeyPressed(Enter)` too). The working recipe lives in `agentTools/` (see `Set-ReleveGameFocus` and `Send-ReleveGameCommands`).

- raylib's char queue is capped at 16 codepoints per frame (`MAX_CHAR_PRESSED_QUEUE` in raylib's `rcore.c`). Pasting a whole line drops the tail; per-character input is fine. Don't "fix" this by guessing — the cap is upstream.

- Fonts/textures must load **after** `Raylib.InitWindow` (OpenGL context required).

## Assets contract

- `src/Releve.Game/assets/**` is copied to the output folder via `<None Include="assets\**\*" CopyToOutputDirectory>`, so adding a file needs no csproj change.

- Room images: `assets/rooms/<ImageKey>.png`, loaded and cached by `RoomViewPanel`. Wrong key or missing file → replacement rectangle with the room title. The game must stay playable with **no images and no audio**.

- **Room image variants (PoC V3)**: `assets/rooms/<ImageKey>_AltState.png` — same dimensions as the base image, loaded on demand by `RoomViewPanel`. Contains the visual changes for active `ImagePatch` zones (opened door, unlocked reader, etc.). If missing, the corresponding patches are silently not drawn.

- Menu background: `assets/menu/menuBackground.png` (PoC V2), stretched to fill the window; missing file → solid background, no error.

- Help text: `assets/texts/help.txt` (PoC V2, UTF-8, lorem ipsum placeholder); missing file → "Texte d'aide introuvable." in the viewer.

- Font: `assets/fonts/DejaVuSans.ttf` (free, licence in `assets/fonts/DejaVuSans-LICENSE.txt`), loaded with an explicit codepoint list in `Ui/Fonts.cs` covering ASCII, French accents, `œ Œ`, guillemets and typographic dashes.

- Audio: `assets/audio/menu.*` and `assets/audio/game.*` (`.ogg`/`.mp3`), looped. Missing files just mean silence.

- `assets/rooms/Bedroom.png`, `HospitalLobby.png`, `TechCorridor.png` are the PoC world's live room images (keys `Bedroom`, `HospitalLobby`, `TechCorridor` from `Content/Rooms/`). The street (key `Street`) has **no** image and shows the replacement rectangle with its title — intended, the old `sandbox_a`/`sandbox_b` bootstrap world is gone. Do not treat the missing `Street.png` as a bug.

- `assets/views/<key>.png` (PoC V4) : images pour le visualiseur modal carré (`ImageViewer`), étirées sur tout le carré 432×432. Clé `"technicalRoom"` pour le hublot de la porte du local technique. Fichier manquant → fond uni + « Image introuvable. » (pas d'erreur).

- `assets/icons/releve.ico` (PoC V4, optionnel) : icône de l'exécutable, utilisée via `<ApplicationIcon Condition="Exists(...)">` dans `Releve.Game.csproj`. Absente → icône par défaut, pas d'erreur.

## Agent limitations

- The agent **cannot observe the screen** nor send keystrokes/clicks to the real game window. Visual and interactive elements (fade transitions, image patch rendering, mouse hover/click on menu/console/text viewer, key repeat) are **"not visually verified by the agent"** and require human validation.

- `agentTools/` automation (keyboard injection, screenshots) works only for agents that can use it; the current agent cannot. Mouse interactions, fade timing, fullscreen toggling (Alt+Entrée), image scrolling (transitions), and modal dialogs (text viewer, image viewer, options) are explicitly **not automatable** — classify as "human validation".

## Git — forbidden actions

- **Never run any git write action** (`add`, `commit`, `push`, `stash`, `checkout`, `reset`, `merge`, tag, PR, config change). Do not commit, amend, or rebase — even if the user seems to be waiting for it. The user commits themselves.

- Read-only git commands (`status`, `diff`, `log`) are fine when they explain the repo. Never treat missing git or a dirty tree as a bug to fix.

## Testing / save-state gotchas

- Save file: `Documents/Releve/save.dat` (AES-256-GCM obfuscation, not encryption-grade). Delete it to get the "no save" menu state; it is written automatically on quit. Note `Documents` resolves to `D:\MesDocuments` here, not `%USERPROFILE%\Documents`.

- Adding/removing `RoomId`/`ObjectId`/`GameEvent` members requires updating `WorldConsistencyTests`' `TestRoomIds`/`TestObjectIds` arrays and the test world — tests intentionally fail loudly on world inconsistency instead of using data validation.

- The `GameApp` layer intercepts the word `quitter` before the engine and saves on shutdown.

- **Test count**: 128 tests (PoC V3: +25 tests — `EncodingTests`, `BedroomV3Tests`, `ReplyTests`, `ImagePatchTests`).