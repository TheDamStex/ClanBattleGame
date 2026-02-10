using System.Text.Json;
using ClanBattleGame.Application.Commands;
using ClanBattleGame.Domain.Commands;

namespace ClanBattleGame.Infrastructure.Repositories;

// Робота з JSON для журналу команд.
public sealed class JsonCommandLogRepository : ICommandLogRepository
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void Save(string path, CommandLog log)
    {
        var json = JsonSerializer.Serialize(log, _options);
        File.WriteAllText(path, json);
    }

    public CommandLog Load(string path)
    {
        if (!File.Exists(path))
        {
            return new CommandLog();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CommandLog>(json, _options) ?? new CommandLog();
        }
        catch (JsonException)
        {
            return new CommandLog();
        }
        catch (IOException)
        {
            return new CommandLog();
        }
    }
}
