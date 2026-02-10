using System.Text.Json;
using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Repositories;

public sealed class JsonConfigRepository : IConfigRepository
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public AppConfig LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var created = CreateDefault();
            Save(path, created);
            return created;
        }

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _options);
            if (config is null)
            {
                config = CreateDefault();
            }
            Normalize(config);
            return config;
        }
        catch (JsonException)
        {
            var fallback = CreateDefault();
            Save(path, fallback);
            return fallback;
        }
        catch (IOException)
        {
            return CreateDefault();
        }
    }

    public void Save(string path, AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, _options);
        File.WriteAllText(path, json);
    }

    private static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            FieldWidth = 40,
            FieldHeight = 15,
            SquadCount = 4,
            MinPlayersPerSquad = 3,
            MaxPlayersPerSquad = 8,
            RandomSeed = null,
            StatRanges = new RaceStatRanges
            {
                Warrior = new StatsRange
                {
                    MinAttack = 8,
                    MaxAttack = 12,
                    MinDefense = 6,
                    MaxDefense = 10,
                    MinSpeed = 4,
                    MaxSpeed = 7,
                    MinHealth = 18,
                    MaxHealth = 24
                },
                Elf = new StatsRange
                {
                    MinAttack = 6,
                    MaxAttack = 10,
                    MinDefense = 4,
                    MaxDefense = 7,
                    MinSpeed = 8,
                    MaxSpeed = 12,
                    MinHealth = 12,
                    MaxHealth = 18
                },
                Dwarf = new StatsRange
                {
                    MinAttack = 7,
                    MaxAttack = 11,
                    MinDefense = 8,
                    MaxDefense = 12,
                    MinSpeed = 3,
                    MaxSpeed = 6,
                    MinHealth = 20,
                    MaxHealth = 26
                }
            }
        };
    }

    private static void Normalize(AppConfig config)
    {
        config.FieldWidth = Math.Max(10, config.FieldWidth);
        config.FieldHeight = Math.Max(5, config.FieldHeight);
        config.SquadCount = Math.Clamp(config.SquadCount, 1, 10);
        config.MinPlayersPerSquad = Math.Clamp(config.MinPlayersPerSquad, 1, 20);
        config.MaxPlayersPerSquad = Math.Clamp(config.MaxPlayersPerSquad, config.MinPlayersPerSquad, 30);
        config.StatRanges ??= new RaceStatRanges();
        config.StatRanges.Warrior ??= new StatsRange();
        config.StatRanges.Elf ??= new StatsRange();
        config.StatRanges.Dwarf ??= new StatsRange();
    }
}
