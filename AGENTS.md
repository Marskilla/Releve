# AGENTS.md

Repo-specific notes for working in **Relève** (C# text/graphic adventure). Read before changing code.

## Layout & entrypoints

- `src/Releve.Engine/` — pure game engine: world, parser, state, save. **Must never reference raylib or any display/audio library.** Tests only cover this project.
- `src/Releve.Game/` — raylib UI. Entry point: `src/Releve.Game/Program.cs` → `GameApp.Run()` (loop in `GameApp.cs`, panels in `Ui/`).
- `tests/Releve.Engine.Tests/` — the only test project. Includes its own world (`TestWorld/TestWorld.cs`) independent of the real content.
- Docs: `ArchitectureTechniqueV0.2.md` is the spec and mirrors the shipped code; `NoteIntentionJeuAventureV5.md` governs UI/behaviour and **wins on conflicts** there. `ImplementationPOCV0.1.md` and `UniversScenarioLoreJeuAventureV0.7.md` describe later phases — do not implement from them yet.

## Commands

```powershell
dotnet build
dotnet test                       # builds everything, runs xunit
dotnet run --project src/Releve.Game
dotnet publish src/Releve.Game -c Release -r win-x64 --self-contained
dotnet test --filter "FullyQualifiedName~NormalizationTests"   # focused run
```

- The game build output is under `bin/Debug/net8.0/**win-x64**/` (fixed `RuntimeIdentifier` in the csproj), not `net8.0/`. `.vscode/launch.json` depends on this path.
- **Stop the running `Releve.Game` process before rebuilding** — it locks `Releve.Game.exe` and the build fails with MSB3027.
- There is no CI, linter, formatter, or typecheck step. `dotnet build` (with `Nullable=enable` from `Directory.Build.props`) is the only static gate. Keep build + tests green after each change.
- Package versions are central-managed in `Directory.Packages.props`; do not add a version inside a csproj.

## Conventions that differ from defaults

- **Player-facing text is French and must be written with precomposed characters and the typographic apostrophe `’`** (e.g. `"Vous l’avez pas sur vous."`, not `n'est`, not decomposed `é`). raylib renders each codepoint independently, so decomposed accents (`e` + U+0301) display garbage. Verify with a regex scan for `[\u0300-\u036f]` after editing text. Identifiers stay English; room text lives in `private const` fields at the top of the class.
- Errors are neutral, present-tense, and never attribute thoughts to the player (see `Core/Messages.cs` and §6.7 of the architecture doc).
- Room/object/enum ids are declared in `src/Releve.Engine/World/Ids.cs`. Adding a room means: enum member + class in `Content/Rooms/` + registration in `Content/BootstrapWorld.cs` + update consistency tests.

## Raylib-cs 8.1.0 gotchas (verified, not from memory)

- `IsFontValid(Font)` / `IsTextureValid(Texture2D)` — there is no `IsFontReady`/`IsTextureReady`.
- `char.ConvertFromUtf32(int)` (lowercase "tf32").
- `Music.looping` is a struct field (`music.Looping = true`); there is no `SetMusicLooping`.
- **`KEYEVENTF_UNICODE`-style literal typed chars work, but virtual-key injection (`SendInput` with `wVk`) never reaches GLFW** — use `keybd_event` if you automate special keys.
- raylib's char queue is capped at 16 codepoints per frame (`MAX_CHAR_PRESSED_QUEUE` in raylib's `rcore.c`). Pasting a whole line drops the tail; per-character input is fine. Don't "fix" this by guessing — the cap is upstream.
- Fonts/textures must load **after** `Raylib.InitWindow` (OpenGL context required).

## Assets contract

- `src/Releve.Game/assets/**` is copied to the output folder via `<None Include="assets\**\*" CopyToOutputDirectory>`, so adding a file needs no csproj change.
- Room images: `assets/rooms/<ImageKey>.png`, loaded and cached by `RoomViewPanel`. Wrong key or missing file → replacement rectangle with the room title. The game must stay playable with **no images and no audio**.
- Font: `assets/fonts/DejaVuSans.ttf` (free, licence in `assets/fonts/DejaVuSans-LICENSE.txt`), loaded with an explicit codepoint list in `Ui/Fonts.cs` covering ASCII, French accents, `œ Œ`, guillemets and typographic dashes.
- Audio: `assets/audio/menu.*` and `assets/audio/game.*` (`.ogg`/`.mp3`), looped. Missing files just mean silence.
- `assets/rooms/Bedroom.png`, `HospitalLobby.png`, `TechCorridor.png` are **unreferenced leftovers**; the bootstrap world uses `sandbox_a`/`sandbox_b` keys only. Do not treat them as live content.

## Git — forbidden actions

- **Never run any git write action** (`add`, `commit`, `push`, `stash`, `checkout`, `reset`, `merge`, tag, PR, config change). Do not commit, amend, or rebase — even if the user seems to be waiting for it. The user commits themselves.
- Read-only git commands (`status`, `diff`, `log`) are fine when they explain the repo. Never treat missing git or a dirty tree as a bug to fix.

## Testing / save-state gotchas

- Save file: `Documents/Releve/save.dat` (AES-256-GCM obfuscation, not encryption-grade). Delete it to get the "no save" menu state; it is written automatically on quit. Note `Documents` resolves to `D:\MesDocuments` here, not `%USERPROFILE%\Documents`.
- Adding/removing `RoomId`/`ObjectId`/`GameEvent` members requires updating `WorldConsistencyTests`' `testRooms`/`testObjects` arrays and the test world — tests intentionally fail loudly on world inconsistency instead of using data validation.
- The `GameApp` layer intercepts the word `quitter` before the engine and saves on shutdown.
