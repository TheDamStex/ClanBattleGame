using ClanBattleGame.Application.State;
using ClanBattleGame.Models;

namespace ClanBattleGame.Application.Commands;

public interface IGameCommand
{
    string Name { get; }
    DateTime CreatedAt { get; }
    void Execute();
    void Undo();
}

public interface ICommandChainNode
{
    void Handle(SquadCommandType cmd);
}

public sealed class PlayerChainNode : ICommandChainNode
{
    public PlayerChainNode(PlayerContext context)
    {
        Context = context;
    }

    public PlayerContext Context { get; }
    public PlayerChainNode? Next { get; set; }

    public void Handle(SquadCommandType cmd)
    {
        switch (cmd)
        {
            case SquadCommandType.Forward:
                Context.CurrentState.OnForward(Context);
                break;
            case SquadCommandType.Backward:
                Context.CurrentState.OnBackward(Context);
                break;
            case SquadCommandType.Fight:
                Context.CurrentState.OnFight(Context);
                break;
        }

        Next?.Handle(cmd);
    }
}

public sealed class PlayerSnapshot
{
    public Guid PlayerId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public PlayerStateType StateType { get; set; }
    public int ActionsPerformed { get; set; }
}

public sealed class SquadCommandResult
{
    public int PlayersAffected { get; set; }
    public int WoundedCount { get; set; }
    public int OutOfBattleCount { get; set; }
    public int RecoveredCount { get; set; }

    public string BuildSummary()
    {
        return $"Поранено: {WoundedCount}, вибуло: {OutOfBattleCount}, відновилось: {RecoveredCount}";
    }
}

public sealed class SquadCommandReceiver
{
    private readonly Squad _squad;
    private readonly AppConfig _config;
    private readonly Services.IRandomProvider _randomProvider;

    public SquadCommandReceiver(Clan clan, Squad squad, AppConfig config, Services.IRandomProvider randomProvider)
    {
        Clan = clan;
        _squad = squad;
        _config = config;
        _randomProvider = randomProvider;
    }

    public Clan Clan { get; }
    public Squad Squad => _squad;

    public List<PlayerSnapshot> CaptureSnapshot()
    {
        return _squad.Players.Select(player => new PlayerSnapshot
        {
            PlayerId = player.Id,
            X = player.Position.X,
            Y = player.Position.Y,
            StateType = player.StateType,
            ActionsPerformed = player.ActionsPerformed
        }).ToList();
    }

    public SquadCommandResult Execute(SquadCommandType commandType)
    {
        var before = _squad.Players.ToDictionary(player => player.Id, player => player.StateType);

        var contexts = _squad.Players.Select(player => new PlayerContext(player, _randomProvider, _config)).ToList();
        var head = BuildChain(contexts);
        head?.Handle(commandType);

        if (commandType is SquadCommandType.Forward or SquadCommandType.Backward)
        {
            var delta = commandType == SquadCommandType.Forward ? 1 : -1;
            _squad.Position = _squad.Position with { X = Math.Clamp(_squad.Position.X + delta, 0, _config.FieldWidth - 1) };
        }

        var result = new SquadCommandResult
        {
            PlayersAffected = contexts.Count(context => context.CurrentState.IsActive)
        };

        foreach (var player in _squad.Players)
        {
            var previous = before[player.Id];
            if (previous == PlayerStateType.Healthy && player.StateType == PlayerStateType.Wounded)
            {
                result.WoundedCount += 1;
            }

            if (previous == PlayerStateType.Wounded && player.StateType == PlayerStateType.OutOfBattle)
            {
                result.OutOfBattleCount += 1;
            }

            if (previous == PlayerStateType.Wounded && player.StateType == PlayerStateType.Healthy)
            {
                result.RecoveredCount += 1;
            }
        }

        return result;
    }

    public void RestoreSnapshot(List<PlayerSnapshot> snapshot)
    {
        var map = snapshot.ToDictionary(item => item.PlayerId, item => item);
        foreach (var player in _squad.Players)
        {
            if (!map.TryGetValue(player.Id, out var state))
            {
                continue;
            }

            player.Position = new Position(state.X, state.Y);
            player.StateType = state.StateType;
            player.ActionsPerformed = state.ActionsPerformed;
        }

        if (_squad.Players.Count > 0)
        {
            var x = _squad.Players.Average(player => player.Position.X);
            var y = _squad.Players.Average(player => player.Position.Y);
            _squad.Position = new Position((int)Math.Round(x), (int)Math.Round(y));
        }
    }

    private static PlayerChainNode? BuildChain(IReadOnlyList<PlayerContext> contexts)
    {
        if (contexts.Count == 0)
        {
            return null;
        }

        PlayerChainNode? head = null;
        PlayerChainNode? current = null;
        foreach (var context in contexts)
        {
            var node = new PlayerChainNode(context);
            if (head is null)
            {
                head = node;
            }
            else
            {
                current!.Next = node;
            }

            current = node;
        }

        return head;
    }
}

public abstract class GameCommandBase : IGameCommand
{
    private readonly SquadCommandReceiver _receiver;
    private readonly SquadCommandType _commandType;

    protected GameCommandBase(string name, string clanName, int squadId, SquadCommandReceiver receiver, SquadCommandType commandType)
    {
        Name = name;
        ClanName = clanName;
        SquadId = squadId;
        _receiver = receiver;
        _commandType = commandType;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; }
    public DateTime CreatedAt { get; }
    public string ClanName { get; }
    public int SquadId { get; }
    public SquadCommandResult LastResult { get; private set; } = new();
    public List<PlayerSnapshot> Snapshot { get; private set; } = new();

    public void Execute()
    {
        Snapshot = _receiver.CaptureSnapshot();
        LastResult = _receiver.Execute(_commandType);
    }

    public void Undo()
    {
        if (Snapshot.Count == 0)
        {
            return;
        }

        _receiver.RestoreSnapshot(Snapshot);
    }
}

public sealed class ForwardCommand : GameCommandBase
{
    public ForwardCommand(string clanName, int squadId, SquadCommandReceiver receiver)
        : base("Forward", clanName, squadId, receiver, SquadCommandType.Forward)
    {
    }
}

public sealed class BackwardCommand : GameCommandBase
{
    public BackwardCommand(string clanName, int squadId, SquadCommandReceiver receiver)
        : base("Backward", clanName, squadId, receiver, SquadCommandType.Backward)
    {
    }
}

public sealed class FightCommand : GameCommandBase
{
    public FightCommand(string clanName, int squadId, SquadCommandReceiver receiver)
        : base("Fight", clanName, squadId, receiver, SquadCommandType.Fight)
    {
    }
}

public sealed class CommandInvoker
{
    private readonly Queue<IGameCommand> _queue = new();
    private readonly List<IGameCommand> _history = new();

    public IReadOnlyList<IGameCommand> History => _history;

    public void Enqueue(IGameCommand command)
    {
        _queue.Enqueue(command);
    }

    public bool ExecuteNext()
    {
        if (_queue.Count == 0)
        {
            return false;
        }

        var command = _queue.Dequeue();
        command.Execute();
        _history.Add(command);
        return true;
    }

    public void ExecuteAll()
    {
        while (ExecuteNext())
        {
        }
    }

    public bool UndoLast()
    {
        if (_history.Count == 0)
        {
            return false;
        }

        var command = _history[^1];
        command.Undo();
        _history.RemoveAt(_history.Count - 1);
        return true;
    }

    public void Clear()
    {
        _queue.Clear();
        _history.Clear();
    }
}
