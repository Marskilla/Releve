using Releve.Engine.Core;
using Releve.Engine.Parsing;
using Releve.Engine.World;

namespace Releve.Engine;

/// <summary>
/// Recoit du texte, renvoie du texte et un etat a afficher. Aucune dependance a l'affichage ou au son.
/// </summary>
public sealed class GameEngine
{
    private readonly WorldModel _world;
    private readonly ReplyPicker _replyPicker;
    private readonly Random _random;

    public GameEngine(WorldModel world, GameState state, Random? random = null)
    {
        _world = world;
        State = state;
        _random = random ?? new Random();
        _replyPicker = new ReplyPicker(_random);
    }

    public GameState State { get; }

    /// <summary>A appeler au demarrage : IntroMessages, Description, OnEnter de la piece courante.</summary>
    public IReadOnlyList<string> Start() => RoomMessages(State.CurrentRoom);

    public CommandResult Execute(string input)
    {
        var words = TextNormalizer.Normalize(input);
        if (words.Count > 0 && ProfanityFilter.Contains(words))
        {
            return CommandResult.Say(_replyPicker.Pick(FlavourText.ProfanityReplies, "ProfanityReplies"));
        }

        var parse = CommandParser.Parse(input, Scope(), ExitWords());
        if (parse.Error == ParseError.Empty)
        {
            return CommandResult.Say(); // la saisie vide est ignoree
        }

        if (parse.Error != ParseError.None)
        {
            if (parse.Error == ParseError.UnknownVerb)
            {
                var useHumour = _random.NextDouble() < FlavourText.HumourProbability;
                var list = useHumour ? FlavourText.UnknownHumour : FlavourText.UnknownNeutral;
                return CommandResult.Say(_replyPicker.Pick(list, useHumour ? "UnknownHumour" : "UnknownNeutral"));
            }
            return CommandResult.Say(parse.ErrorMessage);
        }

        var command = parse.Command!;

        if (command.Action == GameAction.Go)
        {
            if (command.Direction is null)
            {
                return CommandResult.Say(Messages.CannotGo);
            }
            return Move(command.Direction.Value);
        }

        // L'instrument doit etre dans l'inventaire.
        if (command.Instrument is { } instrument && !State.IsInInventory(instrument))
        {
            return CommandResult.Say(Messages.NotCarried);
        }

        var room = _world.Rooms[State.CurrentRoom];

        // 1. comportement propre a la piece
        var roomResult = room.Handle(command, State);
        if (roomResult is not null)
        {
            return roomResult;
        }

        // 2. comportements par defaut
        return DefaultBehaviour(command);
    }

    /// <summary>Instantane relu par la couche d'affichage a chaque image.</summary>
    public RoomView GetView()
    {
        var room = _world.Rooms[State.CurrentRoom];

        var exits = new Dictionary<Direction, ExitState>();
        foreach (var direction in Enum.GetValues<Direction>())
        {
            var exit = room.Exits.FirstOrDefault(e => e.Dir == direction);
            exits[direction] = exit is null
                ? ExitState.Absent
                : exit.To is null || (exit.IsOpen is not null && !exit.IsOpen(State))
                    ? ExitState.Blocked
                    : ExitState.Open;
        }

        var inventory = InventoryItems();
        var activePatches = room.ImagePatches
            .Where(p => p.IsActive(State))
            .Select(p => new ImageRect(p.X1, p.Y1, p.X2, p.Y2))
            .ToList();

        return new RoomView(State.CurrentRoom, room.Title, room.ImageKey(State), exits, inventory, activePatches);
    }

    private CommandResult DefaultBehaviour(Command command)
    {
        // Verbes sans complement : description de la piece ou inventaire.
        if (command.Target is not { } target)
        {
            return command.Action switch
            {
                GameAction.Look => CommandResult.Say(
                    _world.Rooms[State.CurrentRoom].DetailedDescription(State)
                    ?? _world.Rooms[State.CurrentRoom].Description(State)),
                GameAction.Read => CommandResult.Say(Messages.NothingToRead),
                GameAction.Inventory => InventoryResult(),
                _ => CommandResult.Say(Messages.MissingObject),
            };
        }

        var def = _world.Objects[target];

        switch (command.Action)
        {
            case GameAction.Look:
                return CommandResult.Say(def.Describe(State));

            case GameAction.Read:
                return CommandResult.Say(Messages.NothingToRead);

            case GameAction.Take:
                if (def.Portable && IsHere(target))
                {
                    State.Locations[target] = Location.Inventory;
                    return CommandResult.Changed(Messages.Taken);
                }

                return CommandResult.Say(Messages.CannotTake);

            case GameAction.Drop:
                if (State.IsInInventory(target))
                {
                    State.Locations[target] = Location.InRoom(State.CurrentRoom);
                    return CommandResult.Changed(Messages.Dropped);
                }

                return CommandResult.Say(Messages.CannotDrop);

            case GameAction.Open:
                return SetOpenClosed(target, def, ObjectState.Open);

            case GameAction.Close:
                return SetOpenClosed(target, def, ObjectState.Closed);

            case GameAction.Use:
                return command.Instrument is null
                    ? (State.IsInInventory(target)
                        ? CommandResult.Say(Messages.UseWithoutInstrument)
                        : CommandResult.Say(Messages.NothingHappens))
                    : CommandResult.Say(Messages.NothingHappens);

            case GameAction.Talk:
                return CommandResult.Say(Messages.NothingHappens);

            case GameAction.Inventory:
                return InventoryResult();

            default:
                return CommandResult.Say(Messages.NothingHappens);
        }
    }

    private CommandResult SetOpenClosed(ObjectId target, ObjectDef def, ObjectState wanted)
    {
        var opposite = wanted == ObjectState.Open ? ObjectState.Closed : ObjectState.Open;
        if (State.StateOf(target) != opposite)
        {
            return CommandResult.Say(Messages.NothingHappens);
        }

        State.States[target] = wanted;
        var verb = wanted == ObjectState.Open ? "ouvrez" : "fermez";
        return CommandResult.Changed($"Vous {verb} {def.Name}.");
    }

    private CommandResult Move(Direction direction)
    {
        var room = _world.Rooms[State.CurrentRoom];
        var exit = room.Exits.FirstOrDefault(e => e.Dir == direction);
        if (exit is null)
        {
            return CommandResult.Say(Messages.CannotGo);
        }

        if (exit.To is null || (exit.IsOpen is not null && !exit.IsOpen(State)))
        {
            return CommandResult.Say(exit.BlockedMessage ?? Messages.CannotGo);
        }

        State.CurrentRoom = exit.To.Value;
        return CommandResult.Changed(RoomMessages(State.CurrentRoom).ToArray());
    }

    private IReadOnlyList<string> RoomMessages(RoomId roomId)
    {
        var room = _world.Rooms[roomId];
        var messages = new List<string>();
        messages.AddRange(room.IntroMessages(State));
        messages.Add(room.Description(State));
        messages.AddRange(room.OnEnter(State));
        return messages;
    }

    private bool IsHere(ObjectId id) =>
        State.Locations.TryGetValue(id, out var location)
        && location.Kind == LocationKind.Room
        && location.Room == State.CurrentRoom;

    private CommandResult InventoryResult()
    {
        var items = InventoryItems();
        return items.Count == 0
            ? CommandResult.Say(Messages.EmptyInventory)
            : CommandResult.Say(items.Select(i => i.Name).ToArray());
    }

    private List<InventoryItem> InventoryItems() => State.Locations
        .Where(kv => kv.Value.Kind == LocationKind.Inventory)
        .Select(kv => new InventoryItem(kv.Key, _world.Objects[kv.Key].Name))
        .ToList();

    /// <summary>Objets accessibles : piece courante ou inventaire, et visibles (§6.6).</summary>
    private List<ObjectDef> Scope() => _world.Objects.Values
        .Where(def =>
        {
            if (!State.Locations.TryGetValue(def.Id, out var location))
            {
                return false;
            }

            var accessible = location.Kind == LocationKind.Inventory
                || (location.Kind == LocationKind.Room && location.Room == State.CurrentRoom);
            return accessible && (def.VisibleWhen is null || def.VisibleWhen(State));
        })
        .ToList();

    /// <summary>Mots de sortie de la piece courante, associes a leur direction.</summary>
    private IReadOnlyDictionary<string, Direction> ExitWords()
    {
        var words = new Dictionary<string, Direction>(StringComparer.Ordinal);
        var room = _world.Rooms[State.CurrentRoom];
        foreach (var exit in room.Exits)
        {
            if (exit.Words is not null)
            {
                foreach (var word in exit.Words)
                {
                    words[word] = exit.Dir;
                }
            }
        }

        return words;
    }
}
