using System.Text.Json;
using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Repositories;

// Робота з JSON для збереження та завантаження клану.
public sealed class JsonClanRepository : IClanRepository
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void Save(string path, Clan clan)
    {
        var json = JsonSerializer.Serialize(clan, _options);
        File.WriteAllText(path, json);
    }

    public Clan? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Clan>(json, _options);
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
