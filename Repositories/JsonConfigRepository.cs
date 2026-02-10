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
            MaxRounds = 50,
            RandomSeed = null,
            AvailableColors = new List<string> { "червоний", "синій", "зелений", "чорний", "білий" },
            AvailableClothingTypes = new List<string> { "броня", "мантія", "шкіра" },
            MinHeightCm = 150,
            MaxHeightCm = 210,
            FeatureChancePercent = 50,
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
        // Мінімальний розмір поля.
        config.FieldWidth = Math.Max(10, config.FieldWidth);
        config.FieldHeight = Math.Max(5, config.FieldHeight);
        // Мінімум 3 загони, щоб у клані були воїни/ельфи/гноми.
        config.SquadCount = Math.Clamp(config.SquadCount, 3, 10);
        // Мінімум 1 гравець у загоні.
        config.MinPlayersPerSquad = Math.Clamp(config.MinPlayersPerSquad, 1, 20);
        // Максимум не менше мінімуму.
        config.MaxPlayersPerSquad = Math.Clamp(config.MaxPlayersPerSquad, config.MinPlayersPerSquad, 30);
        // Мінімум 1 раунд бою.
        config.MaxRounds = Math.Max(1, config.MaxRounds);
        config.StatRanges ??= new RaceStatRanges();
        config.StatRanges.Warrior ??= new StatsRange();
        config.StatRanges.Elf ??= new StatsRange();
        config.StatRanges.Dwarf ??= new StatsRange();
        config.AvailableColors ??= new List<string>();
        config.AvailableClothingTypes ??= new List<string>();
        config.MinHeightCm = Math.Clamp(config.MinHeightCm, 100, 250);
        config.MaxHeightCm = Math.Clamp(config.MaxHeightCm, config.MinHeightCm, 260);
        config.FeatureChancePercent = Math.Clamp(config.FeatureChancePercent, 0, 100);
    }
}
