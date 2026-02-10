using System.Text.Json;
using ClanBattleGame.Application.Battle;
using ClanBattleGame.Domain.Battle;

namespace ClanBattleGame.Infrastructure.Repositories;

// Робота з JSON для збереження та завантаження журналу бою.
public sealed class JsonBattleLogRepository : IBattleLogRepository
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void Save(string path, BattleLog log)
    {
        var json = JsonSerializer.Serialize(log, _options);
        File.WriteAllText(path, json);
    }

    public BattleLog? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BattleLog>(json, _options);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
