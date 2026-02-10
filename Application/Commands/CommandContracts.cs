using ClanBattleGame.Domain.Commands;
using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Application.Commands;

public interface ICommandLogRepository
{
    void Save(string path, CommandLog log);
    CommandLog Load(string path);
}

public interface IPlayerActionExecutor
{
    void Execute(Player player, SquadCommandType commandType, AppConfig config, int steps);
}

public interface ISquadParticipant
{
    int SquadId { get; }
    string SquadType { get; }
    int PlayersAffected { get; }
    string LastSummary { get; }
    void ReceiveCommand(SquadCommandRequest request);
}

public interface IClanMediator
{
    void RegisterClan(Clan clan);
    void RegisterSquad(ISquadParticipant squad);
    void SendCommandToSquad(SquadCommandRequest request);
    void BroadcastCommandToAll(SquadCommandRequest request);
}

public sealed class CommandContext
{
    public required Clan Clan { get; init; }
    public required Squad TargetSquad { get; init; }
    public required Player Leader { get; init; }
    public required IRandomProvider Random { get; init; }
    public required AppConfig Config { get; init; }
    public bool IsInDanger { get; init; }
    public bool IsEnemyNear { get; init; }
    public bool IsTooFarForward { get; init; }
}

public interface ICommandHandler
{
    ICommandHandler SetNext(ICommandHandler next);
    bool TryHandle(CommandContext context, out SquadCommandRequest request);
}
