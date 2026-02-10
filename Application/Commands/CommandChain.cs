using ClanBattleGame.Domain.Commands;
using ClanBattleGame.Models;

namespace ClanBattleGame.Application.Commands;

public abstract class CommandHandlerBase : ICommandHandler
{
    private ICommandHandler? _next;

    public ICommandHandler SetNext(ICommandHandler next)
    {
        _next = next;
        return next;
    }

    public bool TryHandle(CommandContext context, out SquadCommandRequest request)
    {
        if (TryCreate(context, out request))
        {
            return true;
        }

        if (_next is not null)
        {
            return _next.TryHandle(context, out request);
        }

        request = default!;
        return false;
    }

    protected abstract bool TryCreate(CommandContext context, out SquadCommandRequest request);

    protected static SquadCommandRequest BuildRequest(CommandContext context, SquadCommandType commandType, int steps, string note)
    {
        return new SquadCommandRequest
        {
            ClanName = context.Clan.Name,
            SquadId = context.TargetSquad.SquadId,
            CommandType = commandType,
            CreatedAt = DateTime.UtcNow,
            Note = note,
            Steps = Math.Max(1, steps)
        };
    }
}

public sealed class FightCommandHandler : CommandHandlerBase
{
    protected override bool TryCreate(CommandContext context, out SquadCommandRequest request)
    {
        var chance = context.Random.NextInt(0, 100);
        if (context.IsEnemyNear || chance < 30)
        {
            request = BuildRequest(context, SquadCommandType.Fight, 1, "Загін атакує");
            return true;
        }

        request = default!;
        return false;
    }
}

public sealed class BackwardCommandHandler : CommandHandlerBase
{
    protected override bool TryCreate(CommandContext context, out SquadCommandRequest request)
    {
        var nearBorder = context.TargetSquad.Position.X >= context.Config.FieldWidth - 2;
        var chance = context.Random.NextInt(0, 100);
        if (nearBorder || context.IsInDanger || chance < 20)
        {
            request = BuildRequest(context, SquadCommandType.Backward, 1, "Загін відступає");
            return true;
        }

        request = default!;
        return false;
    }
}

public sealed class ForwardCommandHandler : CommandHandlerBase
{
    protected override bool TryCreate(CommandContext context, out SquadCommandRequest request)
    {
        var center = context.Config.FieldWidth / 2;
        var farFromCenter = context.TargetSquad.Position.X < center;
        var chance = context.Random.NextInt(0, 100);
        if (farFromCenter || context.IsTooFarForward || chance < 60)
        {
            request = BuildRequest(context, SquadCommandType.Forward, 1, "Загін рухається вперед");
            return true;
        }

        request = default!;
        return false;
    }
}

public sealed class DefaultCommandHandler : CommandHandlerBase
{
    protected override bool TryCreate(CommandContext context, out SquadCommandRequest request)
    {
        request = BuildRequest(context, SquadCommandType.Forward, 1, "Типова команда");
        return true;
    }
}

public sealed class ClanCommander
{
    private readonly IClanMediator _mediator;
    private readonly ICommandHandler _commandChain;

    public ClanCommander(IClanMediator mediator, ICommandHandler commandChain)
    {
        _mediator = mediator;
        _commandChain = commandChain;
    }

    public bool IssueCommandToSquad(Guid leaderId, Clan clan, Squad squad, SquadCommandType commandType, int steps = 1)
    {
        if (clan.LeaderId != leaderId)
        {
            return false;
        }

        var request = new SquadCommandRequest
        {
            ClanName = clan.Name,
            SquadId = squad.SquadId,
            CommandType = commandType,
            CreatedAt = DateTime.UtcNow,
            Steps = Math.Max(1, steps),
            Note = "Команда від глави"
        };

        _mediator.SendCommandToSquad(request);
        return true;
    }

    public bool IssueGeneratedCommand(CommandContext context)
    {
        if (context.Clan.LeaderId != context.Leader.Id)
        {
            return false;
        }

        if (!_commandChain.TryHandle(context, out var request))
        {
            return false;
        }

        _mediator.SendCommandToSquad(request);
        return true;
    }
}
