using ClanBattleGame.Models;
using ClanBattleGame.Rendering;
using ClanBattleGame.Repositories;
using ClanBattleGame.Services;

var configPath = "config.json";
var clanPath = "clan.json";

IConfigRepository configRepository = new JsonConfigRepository();
IClanRepository clanRepository = new JsonClanRepository();

var config = configRepository.LoadOrCreate(configPath);

var (randomProvider, clanGenerator) = BuildServices(config);
IClanRenderer textRenderer = new TextClanRenderer();
IClanRenderer asciiRenderer = new AsciiClanRenderer();

Clan? clan = null;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1) Створити новий клан (рандомно)");
    Console.WriteLine("2) Показати склад клану (текст)");
    Console.WriteLine("3) Показати склад клану (псевдографіка)");
    Console.WriteLine("4) Показати главу клану");
    Console.WriteLine("5) Зберегти клан у JSON (clan.json)");
    Console.WriteLine("6) Завантажити клан з JSON (clan.json)");
    Console.WriteLine("7) Переглянути/змінити налаштування (config.json)");
    Console.WriteLine("0) Вихід");
    Console.Write("Обери пункт: ");

    var input = Console.ReadLine();
    Console.WriteLine();

    switch (input)
    {
        case "1":
            Console.Write("Введи назву клану: ");
            var name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Клан-{DateTime.Now:HHmmss}";
            }

            clan = clanGenerator.CreateClan(name, config);
            LeaderManager.Instance.RestoreLeader(clan, randomProvider);
            Console.WriteLine("Клан створено.");
            break;
        case "2":
            if (EnsureClan(clan))
            {
                textRenderer.Render(clan!, config);
            }
            break;
        case "3":
            if (EnsureClan(clan))
            {
                asciiRenderer.Render(clan!, config);
            }
            break;
        case "4":
            if (EnsureClan(clan))
            {
                ShowLeader(clan!);
            }
            break;
        case "5":
            if (EnsureClan(clan))
            {
                clanRepository.Save(clanPath, clan!);
                Console.WriteLine("Клан збережено.");
            }
            break;
        case "6":
            var loaded = clanRepository.Load(clanPath);
            if (loaded is null)
            {
                Console.WriteLine("Не вдалося завантажити клан.");
            }
            else
            {
                clan = loaded;
                LeaderManager.Instance.RestoreLeader(clan, randomProvider);
                Console.WriteLine("Клан завантажено.");
            }
            break;
        case "7":
            EditConfig(config, configRepository, configPath);
            config = configRepository.LoadOrCreate(configPath);
            (randomProvider, clanGenerator) = BuildServices(config);
            Console.WriteLine("Налаштування оновлено.");
            break;
        case "0":
            return;
        default:
            Console.WriteLine("Невідомий пункт меню.");
            break;
    }
}

static bool EnsureClan(Clan? clan)
{
    if (clan is null)
    {
        Console.WriteLine("Клан ще не створено або не завантажено.");
        return false;
    }

    return true;
}

static void ShowLeader(Clan clan)
{
    var leader = LeaderManager.Instance.Leader;
    if (leader is null)
    {
        Console.WriteLine("Глава клану не визначений.");
        return;
    }

    Console.WriteLine("Глава клану:");
    Console.WriteLine($"  Ім'я: {leader.Name}");
    Console.WriteLine($"  Id: {leader.Id}");
    Console.WriteLine($"  Раса: {leader.RaceType}");
    Console.WriteLine($"  Зброя: {leader.WeaponType}");
    Console.WriteLine($"  Пересування: {leader.MovementType}");
    Console.WriteLine($"  Стати: Атк {leader.Stats.Attack}, Зах {leader.Stats.Defense}, Шв {leader.Stats.Speed}, HP {leader.Stats.Health}");
}

static (IRandomProvider, IClanGenerator) BuildServices(AppConfig config)
{
    var randomProvider = new DefaultRandomProvider(config.RandomSeed);
    var factories = new IPlayerFactory[]
    {
        new WarriorPlayerFactory(randomProvider),
        new ElfPlayerFactory(randomProvider),
        new DwarfPlayerFactory(randomProvider)
    };

    var generator = new ClanGenerator(randomProvider, factories);
    return (randomProvider, generator);
}

static void EditConfig(AppConfig config, IConfigRepository repository, string path)
{
    PrintConfig(config);

    config.FieldWidth = ReadInt("Ширина поля", config.FieldWidth);
    config.FieldHeight = ReadInt("Висота поля", config.FieldHeight);
    config.SquadCount = ReadInt("Кількість загонів", config.SquadCount);
    config.MinPlayersPerSquad = ReadInt("Мінімум гравців у загоні", config.MinPlayersPerSquad);
    config.MaxPlayersPerSquad = ReadInt("Максимум гравців у загоні", config.MaxPlayersPerSquad);
    Console.Write($"RandomSeed (поточне {(config.RandomSeed.HasValue ? config.RandomSeed.Value.ToString() : "null")}) нове значення або Enter: ");
    var randomSeedInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(randomSeedInput))
    {
        config.RandomSeed = null;
    }
    else if (int.TryParse(randomSeedInput, out var seedValue))
    {
        config.RandomSeed = seedValue;
    }
    else
    {
        Console.WriteLine("Некоректне значення RandomSeed, залишено попереднє.");
    }

    repository.Save(path, config);
}

static void PrintConfig(AppConfig config)
{
    Console.WriteLine("Поточні налаштування:");
    Console.WriteLine($"  Поле: {config.FieldWidth}x{config.FieldHeight}");
    Console.WriteLine($"  Загонів: {config.SquadCount}");
    Console.WriteLine($"  Гравців у загоні: {config.MinPlayersPerSquad}-{config.MaxPlayersPerSquad}");
    Console.WriteLine($"  Seed: {(config.RandomSeed.HasValue ? config.RandomSeed.Value : "випадковий")}");
}

static int ReadInt(string label, int current)
{
    Console.Write($"{label} (поточне {current}) нове значення або Enter: ");
    var input = Console.ReadLine();
    if (int.TryParse(input, out var value))
    {
        return value;
    }

    return current;
}
