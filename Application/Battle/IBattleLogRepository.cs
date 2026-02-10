using ClanBattleGame.Domain.Battle;

namespace ClanBattleGame.Application.Battle;

// Контракт для збереження та завантаження журналу бою.
public interface IBattleLogRepository
{
    void Save(string path, BattleLog log);
    BattleLog? Load(string path);
}
