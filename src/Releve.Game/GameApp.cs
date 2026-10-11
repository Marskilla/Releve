using Raylib_cs;
using Releve.Engine;
using Releve.Engine.Content;
using Releve.Engine.Core;
using Releve.Engine.Parsing;
using Releve.Engine.Persistence;
using Releve.Engine.World;
using Releve.Game.Audio;
using Releve.Game.Ui;

namespace Releve.Game;

/// <summary>
/// Boucle principale (§9.1) : entrees, mise a jour, dessin, sans liaison de donnees.
/// </summary>
public sealed class GameApp
{
    private Fonts _fonts = null!;
    private ConsolePanel _console = null!;
    private RoomViewPanel _roomView = null!;
    private ArrowsPanel _arrows = null!;
    private InventoryPanel _inventory = null!;
    private MenuScreen _menu = null!;
    private MusicPlayer _music = null!;
    private ISaveStore _saveStore = null!;

    private GameEngine _engine = null!;
    private RoomView _view = null!;
    private bool _playing;
    private bool _quitRequested;
    private bool _canContinue;
    private bool _menuEntered;

    public void Run()
    {
        Raylib.InitWindow(Layout.ScreenWidth, Layout.ScreenHeight, "Relève");
        Raylib.SetExitKey(KeyboardKey.Null); // fermeture uniquement par bouton ou commande « quitter »
        Raylib.SetTargetFPS(60);

        _music = new MusicPlayer();
        _music.LoadTracks();
        _music.Play(MusicPlayer.MenuTrack);

        _fonts = new Fonts();
        _console = new ConsolePanel(_fonts);
        _roomView = new RoomViewPanel(_fonts);
        _arrows = new ArrowsPanel(_fonts);
        _inventory = new InventoryPanel(_fonts);
        _menu = new MenuScreen(_fonts);

        _saveStore = new EncryptedFileSaveStore();

        // La sauvegarde est lue une seule fois au demarrage : le menu la relit a chaque image sinon (§15).
        _canContinue = _saveStore.Exists() && _saveStore.Load() is not null;

        while (!Raylib.WindowShouldClose() && !_quitRequested)
        {
            if (_playing)
            {
                UpdateGame();
            }
            else
            {
                // Reset uniquement a l'entree dans le menu : la navigation est neutralisee sinon (§15).
                if (!_menuEntered)
                {
                    _menu.Reset(_canContinue);
                    _menuEntered = true;
                }

                UpdateMenu(_canContinue);
            }

            Draw(_canContinue);
        }

        // Sauvegarde automatique à la fermeture (note d'intention §10).
        if (_playing)
        {
            _saveStore.Save(_engine.State);
        }

        _roomView.Dispose();
        _fonts.Dispose();
        _music.Dispose();
        Raylib.CloseWindow();
    }

    private void UpdateMenu(bool canContinue)
    {
        var choice = _menu.Update(canContinue);
        if (choice is null)
        {
            return;
        }

        _music.Play(MusicPlayer.GameTrack);
        _menuEntered = false;

        if (choice.Value == 0)
        {
            ContinueGame();
        }
        else
        {
            StartNewGame();
        }

        _playing = true;
    }

    private void UpdateGame()
    {
        _inventory.Update(_view.Inventory.Count);

        if (_console.TryReadCommand(out var command))
        {
            Submit(command);
        }
    }

    private void Submit(string command)
    {
        // « quitter » est intercepte par la couche Game avant d'appeler le moteur (§6.3).
        var words = TextNormalizer.Normalize(command);
        if (words.Count > 0 && Vocabulary.QuitWords.Contains(words[0]))
        {
            _quitRequested = true;
            return;
        }

        _console.Append("> " + command);
        var result = _engine.Execute(command);
        _console.Append(result.Messages);
        _view = _engine.GetView();
    }

    private void StartNewGame()
    {
        var world = GameWorld.Create();
        _engine = new GameEngine(world, world.CreateInitialState());
        _view = _engine.GetView();
        _console.Clear();
        _console.Append(_engine.Start());
        _inventory.ResetScroll();
    }

    private void ContinueGame()
    {
        var saved = _saveStore.Load();
        if (saved is null)
        {
            StartNewGame();
            return;
        }

        var world = GameWorld.Create();
        _engine = new GameEngine(world, saved);
        _view = _engine.GetView();
        _console.Clear();
        _console.Append(_engine.Start());
        _inventory.ResetScroll();
    }

    private void Draw(bool canContinue)
    {
        Raylib.BeginDrawing();

        if (_playing)
        {
            Raylib.ClearBackground(Palette.Background);
            _roomView.Draw(_view.ImageKey, _view.Title);
            _arrows.Draw(_view.Exits);
            _inventory.Draw(_view.Inventory);
            _console.Draw();
        }
        else
        {
            _menu.Draw(canContinue);
        }

        Raylib.EndDrawing();

        _music.Update();
    }
}
