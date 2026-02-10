using System.Text.Json;
using ClanBattleGame.Domain.Commands;
using ClanBattleGame.Models;

namespace ClanBattleGame.Application.Memento;

public sealed class GameSession
{
    public Clan? ClanA { get; set; }
    public Clan? ClanB { get; set; }
    public int TurnIndex { get; set; }
    public string CurrentClan { get; set; } = "A";
    public int? RandomSeed { get; set; }
    public GameCommandHistory CommandHistory { get; set; } = new();
    public AppConfig Config { get; set; } = new();

    public GameCheckpointMemento CreateMemento(string name)
    {
        return new GameCheckpointMemento
        {
            CreatedAt = DateTime.UtcNow,
            Name = name,
            Snapshot = GameSessionSnapshot.FromSession(this)
        };
    }

    public void RestoreFromMemento(GameCheckpointMemento memento)
    {
        if (memento.Snapshot is null)
        {
            return;
        }

        memento.Snapshot.ApplyTo(this);
    }
}

public sealed class GameCheckpointMemento
{
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public GameSessionSnapshot Snapshot { get; set; } = new();
}

public sealed class GameSessionSnapshot
{
    public ClanSnapshot? ClanA { get; set; }
    public ClanSnapshot? ClanB { get; set; }
    public int TurnIndex { get; set; }
    public string CurrentClan { get; set; } = "A";
    public int? RandomSeed { get; set; }
    public List<GameCommandHistoryEntry> CommandHistoryEntries { get; set; } = new();
    public AppConfig Config { get; set; } = new();

    public static GameSessionSnapshot FromSession(GameSession session)
    {
        return new GameSessionSnapshot
        {
            ClanA = session.ClanA is null ? null : ClanSnapshot.FromClan(session.ClanA),
            ClanB = session.ClanB is null ? null : ClanSnapshot.FromClan(session.ClanB),
            TurnIndex = session.TurnIndex,
            CurrentClan = session.CurrentClan,
            RandomSeed = session.RandomSeed,
            CommandHistoryEntries = session.CommandHistory.Entries.TakeLast(50).Select(CloneHistoryEntry).ToList(),
            Config = DeepCopy(session.Config)
        };
    }

    public void ApplyTo(GameSession session)
    {
        session.ClanA = ClanA?.ToClan();
        session.ClanB = ClanB?.ToClan();
        session.TurnIndex = TurnIndex;
        session.CurrentClan = CurrentClan;
        session.RandomSeed = RandomSeed;
        session.Config = DeepCopy(Config);
        session.CommandHistory = new GameCommandHistory
        {
            Entries = CommandHistoryEntries.Select(CloneHistoryEntry).ToList()
        };
    }

    private static T DeepCopy<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Не вдалося клонувати стан.");
    }

    private static GameCommandHistoryEntry CloneHistoryEntry(GameCommandHistoryEntry entry)
    {
        return new GameCommandHistoryEntry
        {
            Timestamp = entry.Timestamp,
            ClanName = entry.ClanName,
            SquadId = entry.SquadId,
            CommandType = entry.CommandType,
            PlayersAffected = entry.PlayersAffected,
            Summary = entry.Summary
        };
    }
}

public sealed class ClanSnapshot
{
    public string Name { get; set; } = string.Empty;
    public Guid? LeaderId { get; set; }
    public LeaderSnapshot? LeaderSnapshot { get; set; }
    public List<SquadSnapshot> Squads { get; set; } = new();

    public static ClanSnapshot FromClan(Clan clan)
    {
        return new ClanSnapshot
        {
            Name = clan.Name,
            LeaderId = clan.LeaderId,
            LeaderSnapshot = clan.LeaderSnapshot,
            Squads = clan.Squads.Select(SquadSnapshot.FromSquad).ToList()
        };
    }

    public Clan ToClan()
    {
        return new Clan
        {
            Name = Name,
            LeaderId = LeaderId,
            LeaderSnapshot = LeaderSnapshot,
            Squads = Squads.Select(squad => squad.ToSquad()).ToList()
        };
    }
}

public sealed class SquadSnapshot
{
    public int SquadId { get; set; }
    public RaceType SquadType { get; set; }
    public Position Position { get; set; }
    public List<PlayerSnapshotDto> Players { get; set; } = new();

    public static SquadSnapshot FromSquad(Squad squad)
    {
        return new SquadSnapshot
        {
            SquadId = squad.SquadId,
            SquadType = squad.SquadType,
            Position = squad.Position,
            Players = squad.Players.Select(PlayerSnapshotDto.FromPlayer).ToList()
        };
    }

    public Squad ToSquad()
    {
        return new Squad
        {
            SquadId = SquadId,
            SquadType = SquadType,
            Position = Position,
            Players = Players.Select(player => player.ToPlayer()).ToList()
        };
    }
}

public sealed class PlayerSnapshotDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RaceType RaceType { get; set; }
    public WeaponType WeaponType { get; set; }
    public MovementType MovementType { get; set; }
    public Stats Stats { get; set; } = new();
    public Position Position { get; set; }
    public Dictionary<string, string> Features { get; set; } = new();
    public int ActionsPerformed { get; set; }
    public PlayerStateType StateType { get; set; }

    public static PlayerSnapshotDto FromPlayer(Player player)
    {
        return new PlayerSnapshotDto
        {
            Id = player.Id,
            Name = player.Name,
            RaceType = player.RaceType,
            WeaponType = player.WeaponType,
            MovementType = player.MovementType,
            Stats = new Stats
            {
                Strength = player.Stats.Strength,
                Dexterity = player.Stats.Dexterity,
                Intelligence = player.Stats.Intelligence,
                MaxHealth = player.Stats.MaxHealth,
                Health = player.Stats.Health,
                Armor = player.Stats.Armor,
                Luck = player.Stats.Luck,
                HeightCm = player.Stats.HeightCm
            },
            Position = player.Position,
            Features = new Dictionary<string, string>(player.Features),
            ActionsPerformed = player.ActionsPerformed,
            StateType = player.StateType
        };
    }

    public Player ToPlayer()
    {
        return new Player
        {
            Id = Id,
            Name = Name,
            RaceType = RaceType,
            WeaponType = WeaponType,
            MovementType = MovementType,
            Stats = new Stats
            {
                Strength = Stats.Strength,
                Dexterity = Stats.Dexterity,
                Intelligence = Stats.Intelligence,
                MaxHealth = Stats.MaxHealth,
                Health = Stats.Health,
                Armor = Stats.Armor,
                Luck = Stats.Luck,
                HeightCm = Stats.HeightCm
            },
            Position = Position,
            Features = new Dictionary<string, string>(Features),
            ActionsPerformed = ActionsPerformed,
            StateType = StateType
        };
    }
}
