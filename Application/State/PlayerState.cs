using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Application.State;

public interface IPlayerState
{
    PlayerStateType Type { get; }
    bool CanMoveForward { get; }
    bool IsActive { get; }
    void OnForward(PlayerContext ctx);
    void OnBackward(PlayerContext ctx);
    void OnFight(PlayerContext ctx);
}

public sealed class PlayerContext
{
    public PlayerContext(Player player, IRandomProvider randomProvider, AppConfig config)
    {
        Player = player;
        RandomProvider = randomProvider;
        Config = config;
        CurrentState = PlayerStateFactory.Create(player.StateType);
    }

    public Player Player { get; }
    public IRandomProvider RandomProvider { get; }
    public AppConfig Config { get; }
    public IPlayerState CurrentState { get; private set; }

    public void TransitionTo(IPlayerState state)
    {
        CurrentState = state;
        Player.StateType = state.Type;
    }

    public bool IsHit()
    {
        return RandomProvider.NextInt(0, 100) < Config.HitChancePercent;
    }

    public void MoveForward()
    {
        if (!CurrentState.CanMoveForward)
        {
            return;
        }

        Player.Position = Player.Position with
        {
            X = Math.Clamp(Player.Position.X + 1, 0, Config.FieldWidth - 1)
        };
    }

    public void MoveBackward()
    {
        Player.Position = Player.Position with
        {
            X = Math.Clamp(Player.Position.X - 1, 0, Config.FieldWidth - 1)
        };
    }
}

public static class PlayerStateFactory
{
    public static IPlayerState Create(PlayerStateType stateType)
    {
        return stateType switch
        {
            PlayerStateType.Healthy => new HealthyPlayerState(),
            PlayerStateType.Wounded => new WoundedPlayerState(),
            PlayerStateType.OutOfBattle => new OutOfBattlePlayerState(),
            _ => new HealthyPlayerState()
        };
    }
}

public sealed class HealthyPlayerState : IPlayerState
{
    public PlayerStateType Type => PlayerStateType.Healthy;
    public bool CanMoveForward => true;
    public bool IsActive => true;

    public void OnForward(PlayerContext ctx)
    {
        ctx.MoveForward();
    }

    public void OnBackward(PlayerContext ctx)
    {
        ctx.MoveBackward();
    }

    public void OnFight(PlayerContext ctx)
    {
        ctx.Player.ActionsPerformed += 1;
        if (ctx.IsHit())
        {
            ctx.TransitionTo(new WoundedPlayerState());
        }
    }
}

public sealed class WoundedPlayerState : IPlayerState
{
    public PlayerStateType Type => PlayerStateType.Wounded;
    public bool CanMoveForward => false;
    public bool IsActive => true;

    public void OnForward(PlayerContext ctx)
    {
    }

    public void OnBackward(PlayerContext ctx)
    {
        ctx.MoveBackward();
        ctx.TransitionTo(new HealthyPlayerState());
    }

    public void OnFight(PlayerContext ctx)
    {
        ctx.Player.ActionsPerformed += 1;
        if (ctx.IsHit())
        {
            ctx.TransitionTo(new OutOfBattlePlayerState());
        }
    }
}

public sealed class OutOfBattlePlayerState : IPlayerState
{
    public PlayerStateType Type => PlayerStateType.OutOfBattle;
    public bool CanMoveForward => false;
    public bool IsActive => false;

    public void OnForward(PlayerContext ctx)
    {
    }

    public void OnBackward(PlayerContext ctx)
    {
    }

    public void OnFight(PlayerContext ctx)
    {
    }
}
