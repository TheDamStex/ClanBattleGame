using ClanBattleGame.Domain.Commands;
using ClanBattleGame.Models;

namespace ClanBattleGame.Application.Commands;

// Виконує базові дії гравця у відповідь на команду.
public sealed class PlayerActionExecutor : IPlayerActionExecutor
{
    public void Execute(Player player, SquadCommandType commandType, AppConfig config, int steps)
    {
        switch (commandType)
        {
            case SquadCommandType.Forward:
                player.Position = player.Position with
                {
                    X = Math.Clamp(player.Position.X + Math.Max(1, steps), 0, config.FieldWidth - 1)
                };
                break;
            case SquadCommandType.Backward:
                player.Position = player.Position with
                {
                    X = Math.Clamp(player.Position.X - Math.Max(1, steps), 0, config.FieldWidth - 1)
                };
                break;
            case SquadCommandType.Fight:
                player.ActionsPerformed += 1;
                break;
        }
    }
}

// Представлення загону як учасника взаємодії через посередника.
public sealed class SquadParticipant : ISquadParticipant
{
    private readonly Squad _squad;
    private readonly AppConfig _config;
    private readonly IPlayerActionExecutor _actionExecutor;

    public SquadParticipant(Squad squad, AppConfig config, IPlayerActionExecutor actionExecutor)
    {
        _squad = squad;
        _config = config;
        _actionExecutor = actionExecutor;
    }

    public int SquadId => _squad.SquadId;
    public string SquadType => _squad.SquadType.ToString();
    public int PlayersAffected { get; private set; }
    public string LastSummary { get; private set; } = string.Empty;

    public void ReceiveCommand(SquadCommandRequest request)
    {
        var steps = Math.Max(1, request.Steps);
        foreach (var player in _squad.Players)
        {
            _actionExecutor.Execute(player, request.CommandType, _config, steps);
        }

        if (request.CommandType is SquadCommandType.Forward or SquadCommandType.Backward)
        {
            var delta = request.CommandType == SquadCommandType.Forward ? steps : -steps;
            _squad.Position = _squad.Position with
            {
                X = Math.Clamp(_squad.Position.X + delta, 0, _config.FieldWidth - 1)
            };
        }

        PlayersAffected = _squad.Players.Count;
        LastSummary = request.CommandType == SquadCommandType.Fight
            ? $"Загін {_squad.SquadId} б'ється. Виконано дій: {_squad.Players.Sum(player => player.ActionsPerformed)}"
            : $"Загін {_squad.SquadId} змінив позицію на ({_squad.Position.X},{_squad.Position.Y})";
    }
}

// Посередник для маршрутизації команд між главою та загонами.
public sealed class ClanMediator : IClanMediator
{
    private readonly Dictionary<int, ISquadParticipant> _participants = new();
    private readonly CommandLog _commandLog;

    public ClanMediator(CommandLog commandLog)
    {
        _commandLog = commandLog;
    }

    public void RegisterClan(Clan clan)
    {
        _commandLog.ClanName = clan.Name;
    }

    public void RegisterSquad(ISquadParticipant squad)
    {
        _participants[squad.SquadId] = squad;
    }

    public void SendCommandToSquad(SquadCommandRequest request)
    {
        if (!_participants.TryGetValue(request.SquadId, out var participant))
        {
            return;
        }

        participant.ReceiveCommand(request);
        _commandLog.Entries.Add(new CommandLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SquadId = participant.SquadId,
            SquadType = participant.SquadType,
            CommandType = request.CommandType,
            PlayersAffected = participant.PlayersAffected,
            Summary = participant.LastSummary
        });
    }

    public void BroadcastCommandToAll(SquadCommandRequest request)
    {
        foreach (var squadId in _participants.Keys)
        {
            var clone = new SquadCommandRequest
            {
                ClanName = request.ClanName,
                SquadId = squadId,
                CommandType = request.CommandType,
                CreatedAt = request.CreatedAt,
                Note = request.Note,
                Steps = request.Steps
            };
            SendCommandToSquad(clone);
        }
    }

    public CommandLog GetCurrentLog() => _commandLog;
}
