using System.IO;
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
    private TextViewer _textViewer = null!;
    private MusicPlayer _music = null!;
    private ISaveStore _saveStore = null!;

    private Texture2D? _menuBackground;
    private GameEngine _engine = null!;
    private RoomView _view = null!;
    private bool _playing;
    private bool _quitRequested;
    private bool _canContinue;
    private bool _menuEntered;
    private ScreenFade _fade = null!;

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
        _textViewer = new TextViewer(_fonts);
        _fade = new ScreenFade();

        _saveStore = new EncryptedFileSaveStore();

        // Image de fond du menu (lot C2) : chargee après InitWindow, absente = fond uni sans erreur.
        var menuBackgroundPath = Assets.MenuBackground;
        if (File.Exists(menuBackgroundPath))
        {
            _menuBackground = Raylib.LoadTexture(menuBackgroundPath);
            if (!Raylib.IsTextureValid(_menuBackground.Value))
            {
                _menuBackground = null;
            }
            else
            {
                _menu.SetBackground(_menuBackground.Value);
            }
        }

        // La sauvegarde est lue une seule fois au demarrage : le menu la relit a chaque image sinon (§15).
        _canContinue = _saveStore.Exists() && _saveStore.Load() is not null;

try
            {
                while (!Raylib.WindowShouldClose() && !_quitRequested)
                {
                    // Calcul du dt borné (pour éviter les à-coups).
                    var dt = MathF.Min(Raylib.GetFrameTime(), 0.05f);

                    // Fondu actif : ne mettre à jour ni le menu, ni le jeu, ni le visualiseur ;
                    // vider la file de caractères à chaque image.
                    if (_fade.IsActive)
                    {
                        while (Raylib.GetCharPressed() > 0)
                        {
                        }

                        var fadeEvent = _fade.Update(dt);
                        if (fadeEvent == ScreenFade.FadeEvent.OutFinished)
                        {
                            // La sortie est finie : démarrer la nouvelle partie, puis fondu d'entrée.
                            StartNewGame();
                            _playing = true;
                            _fade.StartIn();
                        }
                    }
                    else
                    {
                        // Le visualiseur est modal : il est mis à jour en priorité, que l'on soit
                        // dans le menu ou en partie (lot C3). Il bloque UpdateMenu et UpdateGame.
                        if (_textViewer.IsOpen)
                        {
                            // Vider la file de caractères pour qu'aucune saisie ne fuie dans la console à la fermeture.
                            while (Raylib.GetCharPressed() > 0)
                            {
                            }

                            if (_textViewer.Update())
                            {
                                // Fraîchement fermé : si on était dans le menu, repositionner la sélection.
                                _menuEntered = true;
                            }
                        }
                        else if (_playing)
                        {
                            UpdateGame();
                        }
                        else
                        {
                            // Reset uniquement à l'entrée dans le menu : la navigation est neutralisée sinon (§15).
                            if (!_menuEntered)
                            {
                                _menu.Reset(_canContinue);
                                _menuEntered = true;
                            }

                            UpdateMenu(_canContinue);
                        }
                    }

                    Draw(_canContinue);
                }
            }
        finally
        {
            // Sauvegarde de securite a la sortie, y compris sur exception (C1). Les exceptions
            // ne sont pas avalees : elles se propagent apres la sauvegarde.
            if (_playing)
            {
                _saveStore.Save(_engine.State);
            }

            if (_menuBackground is { } bg && Raylib.IsTextureValid(bg))
            {
                Raylib.UnloadTexture(bg);
            }

            _roomView.Dispose();
            _fonts.Dispose();
            _music.Dispose();
            Raylib.CloseWindow();
        }
    }

private void UpdateMenu(bool canContinue)
        {
            // Tant que le visualiseur est ouvert, le menu ne progresse pas (modal, lot C3).
            if (_textViewer.IsOpen)
            {
                return;
            }

            var choice = _menu.Update(canContinue);
            if (choice is null)
            {
                return;
            }

            _menuEntered = false;

            switch (choice)
            {
                case MenuScreen.MenuChoice.Continue:
                    ContinueGame();
                    _playing = true;
                    break;

                case MenuScreen.MenuChoice.NewGame:
                    // Fondu de sortie : le menu reste affiché pendant la sortie.
                    _fade.StartOut();
                    // Ne PAS appeler StartNewGame() ni mettre _playing à vrai ici.
                    // La transition se fait dans la boucle principale sur OutFinished.
                    break;

                case MenuScreen.MenuChoice.Help:
                    OpenHelp();
                    break;
            }
        }

private void OpenHelp()
        {
            // Lot C4 : texte d'aide depuis assets/texts/help.txt.
            string helpText;
            try
            {
                helpText = File.Exists(Assets.HelpText)
                    ? File.ReadAllText(Assets.HelpText)
                    : "Texte d’aide introuvable.";
            }
            catch
            {
                helpText = "Texte d’aide introuvable.";
            }

            // Plein ecran : le visualiseur recouvre toute la fenêtre.
            _textViewer.Open(new Rectangle(0, 0, Layout.ScreenWidth, Layout.ScreenHeight), "Aide", helpText);
        }

    private void UpdateGame()
    {
        _inventory.Update(_view.Inventory.Count);

        // Raccourcis clavier pour les deplacements (J6.4) : fleches directionnelles
        // Up = nord, Down = sud, Right = est, Left = ouest
        // Utilise IsKeyPressedRepeat pour permettre le maintien (repetition)
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressedRepeat(KeyboardKey.Up))
        {
            Submit("nord");
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressedRepeat(KeyboardKey.Down))
        {
            Submit("sud");
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressedRepeat(KeyboardKey.Right))
        {
            Submit("est");
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressedRepeat(KeyboardKey.Left))
        {
            Submit("ouest");
        }

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

        _console.Append("> " + command, true);
        var result = _engine.Execute(command);
        _console.Append(result.Messages);
        _view = _engine.GetView();

        // Sauvegarde automatique après tout changement d'etat (deplacement inclus) (C1).
        if (_playing && result.StateChanged)
        {
            _saveStore.Save(_engine.State);
        }
    }

    private void StartNewGame()
    {
        var world = GameWorld.Create();
        _engine = new GameEngine(world, world.CreateInitialState());
        _view = _engine.GetView();
        _console.Clear();
        _console.Append(_engine.Start());
        _inventory.ResetScroll();
        _music.Play(MusicPlayer.GameTrack);
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
        _music.Play(MusicPlayer.GameTrack);
    }

private void Draw(bool canContinue)
        {
            Raylib.BeginDrawing();

            if (_playing)
            {
                Raylib.ClearBackground(Palette.Background);
                _roomView.Draw(_view);
                _arrows.Draw(_view.Exits);
                _inventory.Draw(_view.Inventory);
                _console.Draw();
            }
            else
            {
                _menu.Draw(canContinue);
            }

            // Le visualiseur est dessiné par-dessus la scène courante (lot C3).
            if (_textViewer.IsOpen)
            {
                _textViewer.Draw();
            }

            // Fondu par-dessus tout.
            _fade.Draw();

            Raylib.EndDrawing();

            _music.Update();
        }
}
