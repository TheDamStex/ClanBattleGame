using System.Text.Json;
using ClanBattleGame.Domain.Commands;

namespace ClanBattleGame.Infrastructure.Repositories;

public interface IGameCommandHistoryRepository
{
    void Save(string path, GameCommandHistory history);
    GameCommandHistory Load(string path);
}

public sealed class JsonGameCommandHistoryRepository : IGameCommandHistoryRepository
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void Save(string path, GameCommandHistory history)
    {
        var json = JsonSerializer.Serialize(history, _options);
        File.WriteAllText(path, json);
    }

    public GameCommandHistory Load(string path)
    {
        if (!File.Exists(path))
        {
            return new GameCommandHistory();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GameCommandHistory>(json, _options) ?? new GameCommandHistory();
        }
        catch (JsonException)
        {
            return new GameCommandHistory();
        }
        catch (IOException)
        {
            return new GameCommandHistory();
        }
    }
}
