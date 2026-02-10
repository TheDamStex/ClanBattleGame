using ClanBattleGame.Models;

namespace ClanBattleGame.Application.Battle;

// Контракт для розрахунку урону.
public interface IDamageCalculator
{
    int CalculateDamage(Player attacker, Player target);
}
