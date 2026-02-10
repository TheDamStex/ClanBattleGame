using ClanBattleGame.Models;

namespace ClanBattleGame.Services;

// Одинак (Singleton) для збереження глави клану.
public sealed class LeaderManager
{
    private static readonly Lazy<LeaderManager> LazyInstance = new(() => new LeaderManager());
    private Player? _leader;

    private LeaderManager()
    {
    }

    public static LeaderManager Instance => LazyInstance.Value;

    public Player? Leader => _leader;

    public void SetLeader(Player leader, Clan clan)
    {
        _leader = leader;
        clan.LeaderId = leader.Id;
        clan.LeaderSnapshot = new LeaderSnapshot
        {
            Id = leader.Id,
            Name = leader.Name,
            RaceType = leader.RaceType,
            WeaponType = leader.WeaponType,
            MovementType = leader.MovementType,
            Stats = new Stats
            {
                Attack = leader.Stats.Attack,
                Defense = leader.Stats.Defense,
                Speed = leader.Stats.Speed,
                Health = leader.Stats.Health
            }
        };
    }

    public void RestoreLeader(Clan clan, IRandomProvider randomProvider)
    {
        if (clan.LeaderId.HasValue)
        {
            var existing = clan.Squads
                .SelectMany(s => s.Players)
                .FirstOrDefault(p => p.Id == clan.LeaderId.Value);
            if (existing is not null)
            {
                _leader = existing;
                return;
            }
        }

        var allPlayers = clan.Squads.SelectMany(s => s.Players).ToList();
        if (allPlayers.Count == 0)
        {
            _leader = null;
            clan.LeaderId = null;
            clan.LeaderSnapshot = null;
            return;
        }

        var leader = allPlayers[randomProvider.NextInt(0, allPlayers.Count)];
        SetLeader(leader, clan);
    }
}
