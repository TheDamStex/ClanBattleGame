using System.Text.Json;
using ClanBattleGame.Domain.Battle;
using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Application.Battle;

// Запускає просту симуляцію бою двох кланів.
public sealed class BattleSimulator : IBattleSimulator
{
    private readonly IRandomProvider _randomProvider;
    private readonly IDamageCalculator _damageCalculator;
    private readonly JsonSerializerOptions _cloneOptions = new();

    public BattleSimulator(IRandomProvider randomProvider, IDamageCalculator damageCalculator)
    {
        _randomProvider = randomProvider;
        _damageCalculator = damageCalculator;
    }

    public BattleLog Simulate(Clan clanA, Clan clanB, AppConfig config)
    {
        var workingClanA = CloneClan(clanA);
        var workingClanB = CloneClan(clanB);

        var log = new BattleLog
        {
            StartedAt = DateTime.Now,
            ClanAName = workingClanA.Name,
            ClanBName = workingClanB.Name
        };

        var roundNumber = 1;
        while (roundNumber <= config.MaxRounds && HasAlivePlayers(workingClanA) && HasAlivePlayers(workingClanB))
        {
            ExecuteAttack(workingClanA, workingClanB, log, roundNumber);

            if (!HasAlivePlayers(workingClanB))
            {
                break;
            }

            ExecuteAttack(workingClanB, workingClanA, log, roundNumber);
            roundNumber++;
        }

        log.AliveCountA = CountAlivePlayers(workingClanA);
        log.AliveCountB = CountAlivePlayers(workingClanB);
        log.Result = DetermineResult(log.AliveCountA, log.AliveCountB, log.ClanAName, log.ClanBName);
        log.FinishedAt = DateTime.Now;
        return log;
    }

    private void ExecuteAttack(Clan attackerClan, Clan targetClan, BattleLog log, int roundNumber)
    {
        var attackers = GetAlivePlayers(attackerClan);
        var targets = GetAlivePlayers(targetClan);

        if (attackers.Count == 0 || targets.Count == 0)
        {
            return;
        }

        var attacker = attackers[_randomProvider.NextInt(0, attackers.Count)];
        var target = targets[_randomProvider.NextInt(0, targets.Count)];
        var damage = _damageCalculator.CalculateDamage(attacker, target);

        target.Stats.Health -= damage;
        if (target.Stats.Health <= 0)
        {
            target.Stats.Health = 0;
        }

        var wasKilled = target.Stats.Health == 0;

        log.Rounds.Add(new BattleRoundLog
        {
            RoundNumber = roundNumber,
            AttackerId = attacker.Id,
            AttackerName = attacker.Name,
            AttackerClan = attackerClan.Name,
            TargetId = target.Id,
            TargetName = target.Name,
            TargetClan = targetClan.Name,
            Damage = damage,
            TargetHealthAfter = target.Stats.Health,
            WasKilled = wasKilled
        });
    }

    private static string DetermineResult(int aliveA, int aliveB, string clanAName, string clanBName)
    {
        if (aliveA == 0 && aliveB == 0)
        {
            return "Нічия";
        }

        if (aliveA == 0)
        {
            return clanBName;
        }

        if (aliveB == 0)
        {
            return clanAName;
        }

        if (aliveA > aliveB)
        {
            return clanAName;
        }

        if (aliveB > aliveA)
        {
            return clanBName;
        }

        return "Нічия";
    }

    private static bool HasAlivePlayers(Clan clan)
    {
        return clan.Squads.SelectMany(squad => squad.Players).Any(player => player.Stats.Health > 0);
    }

    private static int CountAlivePlayers(Clan clan)
    {
        return clan.Squads.SelectMany(squad => squad.Players).Count(player => player.Stats.Health > 0);
    }

    private static List<Player> GetAlivePlayers(Clan clan)
    {
        return clan.Squads.SelectMany(squad => squad.Players)
            .Where(player => player.Stats.Health > 0)
            .ToList();
    }

    private Clan CloneClan(Clan clan)
    {
        var json = JsonSerializer.Serialize(clan, _cloneOptions);
        var clone = JsonSerializer.Deserialize<Clan>(json, _cloneOptions);
        return clone ?? new Clan { Name = clan.Name };
    }
}
