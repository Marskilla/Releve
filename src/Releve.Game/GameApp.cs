using System.IO;
using System.Numerics;
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
    private ImageViewer _imageViewer = null!;
    private OptionsDialog _optionsDialog = null!;
    private MusicPlayer _music = null!;
    private ISaveStore _saveStore = null!;
    private SettingsStore _settingsStore = null!;

    private Texture2D? _menuBackground;
    private GameEngine _engine = null!;
    private RoomView _view = null!;
    private bool _playing;
    private bool _quitRequested;
    private bool _canContinue;
    private bool _menuEntered;
    private ScreenFade _fade = null!;

    // J6: rendu logique 1280x720 dans texture
    private RenderTexture2D _renderTexture;
    private ScreenScaler _screenScaler = null!;
    private bool _fullscreen;

    /// <summary>J10-4: Acces lecture a l'etat plein ecran pour OptionsDialog.</summary>
    public bool IsFullscreen => _fullscreen;

    public void Run()
    {
#if !DEBUG
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
#endif
        Raylib.InitWindow(Layout.ScreenWidth, Layout.ScreenHeight, "Relève");
        Raylib.SetExitKey(KeyboardKey.Null); // fermeture uniquement par bouton ou commande « quitter »
        Raylib.SetTargetFPS(60);

        // J6: creer la texture de rendu logique
        _renderTexture = Raylib.LoadRenderTexture(Layout.ScreenWidth, Layout.ScreenHeight);
        Raylib.SetTextureFilter(_renderTexture.Texture, TextureFilter.Bilinear);

        _screenScaler = new ScreenScaler();

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
        _imageViewer = new ImageViewer(_fonts);

        _saveStore = new EncryptedFileSaveStore();
        _settingsStore = new SettingsStore();

        _optionsDialog = new OptionsDialog(_fonts, _settingsStore, _music, this);
        _fade = new ScreenFade();

        // J10-5: appliquer le reglage Musique au demarrage
        var settings = _settingsStore.Load();
        _music.SetEnabled(settings.Music);

        // J6-8: charger reglages, appliquer plein ecran
        _fullscreen = settings.Fullscreen;
        if (_fullscreen)
        {
            SetFullscreen(true);
        }

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
                // J6-6: Alt+Entree en tout debut de frame (avant menu, jeu, visualiseur, fondu, options)
                HandleFullscreenToggle();

                // J6-2: mettre a jour l'echelle et l'offset souris AVANT les mises a jour
                _screenScaler.Update(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

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
                    // Les visualiseurs modaux (texte, image, options) sont mis a jour en priorite,
                    // que l'on soit dans le menu ou en partie (J9-7, J10-4).
                    // Ils bloquent UpdateMenu et UpdateGame.
                    bool modalOpen = _textViewer.IsOpen || _imageViewer.IsOpen || _optionsDialog.IsOpen;

                    if (modalOpen)
                    {
                        // Vider la file de caracteres pour qu'aucune saisie ne fuie.
                        while (Raylib.GetCharPressed() > 0)
                        {
                        }

                        // TextViewer
                        if (_textViewer.IsOpen)
                        {
                            if (_textViewer.Update())
                            {
                                // J9-8: a la fermeture d'un modal ouvert depuis le moteur
                                if (_playing)
                                {
                                    var chosenId = _textViewer.ChosenId;
                                    if (!string.IsNullOrEmpty(chosenId))
                                    {
                                        var dialogResult = _engine.ResolveDialog(chosenId!);
                                        _console.Append(dialogResult.Messages);
                                        if (dialogResult.StateChanged)
                                        {
                                            _saveStore.Save(_engine.State);
                                        }
                                        _view = _engine.GetView();
                                    }
                                }
                                else
                                {
                                    // Aide du menu : ne pas appeler ResolveDialog
                                    _menuEntered = true;
                                }
                            }
                        }
                        // ImageViewer
                        else if (_imageViewer.IsOpen)
                        {
                            if (_imageViewer.Update())
                            {
                                // J9-8: a la fermeture d'un modal Image ouvert depuis le moteur
                                if (_playing)
                                {
                                    var chosenId = DialogIds.Close; // ImageViewer n'a qu'un bouton Close
                                    var dialogResult = _engine.ResolveDialog(chosenId);
                                    _console.Append(dialogResult.Messages);
                                    if (dialogResult.StateChanged)
                                    {
                                        _saveStore.Save(_engine.State);
                                    }
                                    _view = _engine.GetView();
                                }
                            }
                        }
                        // OptionsDialog
                        else if (_optionsDialog.IsOpen)
                        {
                            if (_optionsDialog.Update())
                            {
                                // J10-6: a la fermeture, retour au menu avec selection sur « Options »
                                _menuEntered = false; // force Reset au prochain UpdateMenu
                            }
                        }
                    }
                    else if (_playing)
                    {
                        // J7-7: mettre a jour la transition de piece a chaque image
                        _roomView.Update(dt);
                        UpdateGame();
                    }
                    else
                    {
                        // Reset uniquement a l'entree dans le menu : la navigation est neutralisee sinon.
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

            Raylib.UnloadRenderTexture(_renderTexture);
            _roomView.Dispose();
            _fonts.Dispose();
            _music.Dispose();
            Raylib.CloseWindow();
        }
    }

    /// <summary>J6-6: Bascule plein ecran sur Alt+Entree. Fonctionne dans tous les etats.</summary>
    private void HandleFullscreenToggle()
    {
        bool altDown = Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt);
        if (altDown && Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            SetFullscreen(!_fullscreen);
            // J6-6: vider la file de caracteres et sauter les mises a jour de cette frame
            while (Raylib.GetCharPressed() > 0)
            {
            }
            // Sauter le reste de la frame en revenant au debut de la boucle
            // (on ne fait pas Draw ici, la boucle continue)
        }
    }

    /// <summary>J6-5, J10-4: Source de verite _fullscreen. N'appelle l'API raylib que si l'etat change.</summary>
    public void SetFullscreen(bool on)
    {
        if (_fullscreen == on)
        {
            return;
        }

        _fullscreen = on;

        // Mode retenu : plein ecran en fenetre sans bordure (ToggleBorderlessWindowed)
        // Verifier la disponibilite dans le binding
        try
        {
            Raylib.ToggleBorderlessWindowed();
        }
        catch
        {
            // Fallback si non disponible
            Raylib.ToggleFullscreen();
        }

        // Sauvegarder le reglage
        var settings = _settingsStore.Load();
        settings = settings with { Fullscreen = _fullscreen };
        _settingsStore.Save(settings);
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

                case MenuScreen.MenuChoice.Options:
                    _optionsDialog.Open();
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
                    : "Texte d'aide introuvable.";
            }
            catch
            {
                helpText = "Texte d'aide introuvable.";
            }

            // Plein ecran : le visualiseur recouvre toute la fenêtre.
            _textViewer.Open(new Rectangle(0, 0, Layout.ScreenWidth, Layout.ScreenHeight), "Aide", helpText);
        }

private void UpdateGame()
        {
            // J3-2: memoriser l'état de la ligne de saisie au debut de la frame
            var inputEmptyAtFrameStart = _console.InputIsEmpty;

            _inventory.Update(_view.Inventory.Count);

            // Raccourcis clavier pour les deplacements : fleches directionnelles
            // Up = nord, Down = sud, Right = est, Left = ouest
            // Utilise IsKeyPressedRepeat pour permettre le maintien (repetition)
            // NE se declenche que si NI Shift NI Ctrl n'est enfonce (pour eviter les conflits
            // avec le rappel historique Shift+fleches).
            // J3-2: ne deplace le joueur que si la ligne de saisie etait vide au debut de la frame.
            bool shiftDown = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
            bool ctrlDown = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
            bool modifierFree = !shiftDown && !ctrlDown;

            if (inputEmptyAtFrameStart && modifierFree && (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressedRepeat(KeyboardKey.Up)))
            {
                Submit("nord");
            }
            else if (inputEmptyAtFrameStart && modifierFree && (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressedRepeat(KeyboardKey.Down)))
            {
                Submit("sud");
            }
            else if (inputEmptyAtFrameStart && modifierFree && (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressedRepeat(KeyboardKey.Right)))
            {
                Submit("est");
            }
            else if (inputEmptyAtFrameStart && modifierFree && (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressedRepeat(KeyboardKey.Left)))
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

            // J7-7: avant Execute, terminer toute transition en cours et memoriser la vue courante
            _roomView.CompleteTransition();
            var prevView = _view;

            _console.Append("> " + command, true);
            _console.RecordCommand(command); // historisation pour rappel Shift+Up/Down (toutes commandes)
            var result = _engine.Execute(command);
            _console.Append(result.Messages);
            _view = _engine.GetView();

            // J7-7: apres Execute, si deplacement reussi vers une autre piece, demarrer la transition
            if (result.MovedTo.HasValue && _view.Room != prevView.Room)
            {
                _roomView.BeginTransition(prevView, _view, result.MovedTo.Value);
            }

            // J9-7: si le moteur demande un dialogue modal, l'ouvrir
            if (result.Dialog is not null)
            {
                if (result.Dialog.Kind == DialogKind.Text)
                {
                    _textViewer.Open(Layout.DialogBounds, result.Dialog.Title, result.Dialog.Content,
                        result.Dialog.Choices, result.Dialog.DefaultChoiceId, result.Dialog.CancelChoiceId);
                }
                else if (result.Dialog.Kind == DialogKind.Image)
                {
                    _imageViewer.Open(result.Dialog.Content);
                }
            }

            // Sauvegarde automatique apres tout changement d'etat (deplacement inclus) (C1).
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
            // J6-1: Tout le dessin passe dans la texture de rendu logique
            Raylib.BeginTextureMode(_renderTexture);

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

            // Le visualiseur est dessiné par-dessus la scène courante (lot C3, J9, J10).
            if (_textViewer.IsOpen)
            {
                _textViewer.Draw();
            }
            if (_imageViewer.IsOpen)
            {
                _imageViewer.Draw();
            }
            if (_optionsDialog.IsOpen)
            {
                _optionsDialog.Draw();
            }

            // Fondu par-dessus tout.
            _fade.Draw();

            Raylib.EndTextureMode();

            // J6-3: Présentation : fond noir, DrawTexturePro avec hauteur négative pour inversion verticale
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // Rectangle source avec hauteur négative (inversion verticale propre à raylib)
            var sourceRect = new Rectangle(0, 0, Layout.ScreenWidth, -Layout.ScreenHeight);
            Raylib.DrawTexturePro(_renderTexture.Texture, sourceRect, _screenScaler.DestRect, Vector2.Zero, 0f, Color.White);

            Raylib.EndDrawing();

            _music.Update();
        }
    }
