using ClanBattleGame.Domain.Battle;
using ClanBattleGame.Models;

namespace ClanBattleGame.Application.Battle;

// Контракт для запуску симуляції бою.
public interface IBattleSimulator
{
    BattleLog Simulate(Clan clanA, Clan clanB, AppConfig config);
}
