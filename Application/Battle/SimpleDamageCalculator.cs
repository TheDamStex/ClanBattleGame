using ClanBattleGame.Models;

namespace ClanBattleGame.Application.Battle;

// Простий розрахунок урону за формулою з умов.
public sealed class SimpleDamageCalculator : IDamageCalculator
{
    public int CalculateDamage(Player attacker, Player target)
    {
        var damage = attacker.Stats.Attack - target.Stats.Defense / 2;
        return Math.Max(1, damage);
    }
}
