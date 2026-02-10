using ClanBattleGame.Application.Battle;
using ClanBattleGame.Domain.Battle;
using ClanBattleGame.Infrastructure.Repositories;
using ClanBattleGame.Models;
using ClanBattleGame.Rendering;
using ClanBattleGame.Repositories;
using ClanBattleGame.Services;

var configPath = "config.json";
var clanPath = "clan.json";
var battleLogPath = "battle_log.json";

IConfigRepository configRepository = new JsonConfigRepository();
IClanRepository clanRepository = new JsonClanRepository();
IBattleLogRepository battleLogRepository = new JsonBattleLogRepository();

var config = configRepository.LoadOrCreate(configPath);

IDamageCalculator damageCalculator = new SimpleDamageCalculator();
var (randomProvider, clanGenerator, battleSimulator) = BuildServices(config, damageCalculator);
IClanRenderer textRenderer = new TextClanRenderer();
IClanRenderer asciiRenderer = new AsciiClanRenderer();

Clan? clan = null;
BattleLog? lastBattleLog = null;

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
    Console.WriteLine("8) Симуляція бою двох кланів (створити 2 клани і провести бій)");
    Console.WriteLine("9) Показати останній журнал бою (з JSON)");
    Console.WriteLine("10) Зберегти журнал бою у JSON (battle_log.json)");
    Console.WriteLine("11) Завантажити журнал бою з JSON (battle_log.json)");
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
            (randomProvider, clanGenerator, battleSimulator) = BuildServices(config, damageCalculator);
            Console.WriteLine("Налаштування оновлено.");
            break;
        case "8":
            Console.Write("Назва клану A (Enter для стандартної): ");
            var clanAName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(clanAName))
            {
                clanAName = "Клан A";
            }

            Console.Write("Назва клану B (Enter для стандартної): ");
            var clanBName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(clanBName))
            {
                clanBName = "Клан B";
            }

            var clanA = clanGenerator.CreateClan(clanAName, config);
            var clanB = clanGenerator.CreateClan(clanBName, config);
            lastBattleLog = battleSimulator.Simulate(clanA, clanB, config);
            ShowBattleSummary(lastBattleLog);
            break;
        case "9":
            var loadedLog = battleLogRepository.Load(battleLogPath);
            if (loadedLog is null)
            {
                Console.WriteLine("Журнал бою не знайдено.");
            }
            else
            {
                lastBattleLog = loadedLog;
                ShowBattleLogShort(lastBattleLog);
            }
            break;
        case "10":
            if (lastBattleLog is null)
            {
                Console.WriteLine("Немає журналу бою для збереження.");
            }
            else
            {
                battleLogRepository.Save(battleLogPath, lastBattleLog);
                Console.WriteLine("Журнал бою збережено.");
            }
            break;
        case "11":
            var loadedBattleLog = battleLogRepository.Load(battleLogPath);
            if (loadedBattleLog is null)
            {
                Console.WriteLine("Не вдалося завантажити журнал бою.");
            }
            else
            {
                lastBattleLog = loadedBattleLog;
                Console.WriteLine("Журнал бою завантажено.");
            }
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

static (IRandomProvider, IClanGenerator, IBattleSimulator) BuildServices(AppConfig config, IDamageCalculator damageCalculator)
{
    var randomProvider = new DefaultRandomProvider(config.RandomSeed);
    var factories = new IPlayerFactory[]
    {
        new WarriorPlayerFactory(randomProvider),
        new ElfPlayerFactory(randomProvider),
        new DwarfPlayerFactory(randomProvider)
    };

    var generator = new ClanGenerator(randomProvider, factories);
    var battleSimulator = new BattleSimulator(randomProvider, damageCalculator);
    return (randomProvider, generator, battleSimulator);
}

static void EditConfig(AppConfig config, IConfigRepository repository, string path)
{
    PrintConfig(config);

    config.FieldWidth = ReadInt("Ширина поля", config.FieldWidth);
    config.FieldHeight = ReadInt("Висота поля", config.FieldHeight);
    config.SquadCount = ReadInt("Кількість загонів", config.SquadCount);
    config.MinPlayersPerSquad = ReadInt("Мінімум гравців у загоні", config.MinPlayersPerSquad);
    config.MaxPlayersPerSquad = ReadInt("Максимум гравців у загоні", config.MaxPlayersPerSquad);
    config.MaxRounds = ReadInt("Максимум раундів бою", config.MaxRounds);
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
    Console.WriteLine($"  Максимум раундів бою: {config.MaxRounds}");
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

static void ShowBattleSummary(BattleLog log)
{
    Console.WriteLine("Підсумок бою:");
    Console.WriteLine($"  Клан A: {log.ClanAName}, живих: {log.AliveCountA}");
    Console.WriteLine($"  Клан B: {log.ClanBName}, живих: {log.AliveCountB}");
    if (log.Result == "Нічия")
    {
        Console.WriteLine("  Результат: Нічия");
    }
    else
    {
        Console.WriteLine($"  Переможець: {log.Result}");
    }

    var topPlayers = GetTopDamagePlayers(log, 3);
    if (topPlayers.Count == 0)
    {
        Console.WriteLine("  Немає даних про урон.");
        return;
    }

    Console.WriteLine("  Топ-3 гравці за уроном:");
    for (var i = 0; i < topPlayers.Count; i++)
    {
        var player = topPlayers[i];
        Console.WriteLine($"    {i + 1}) {player.Name} ({player.Clan}) - {player.Damage}");
    }
}

static void ShowBattleLogShort(BattleLog log)
{
    Console.WriteLine("Журнал бою:");
    Console.WriteLine($"  Початок: {log.StartedAt:g}");
    Console.WriteLine($"  Завершення: {log.FinishedAt:g}");
    Console.WriteLine($"  Клани: {log.ClanAName} vs {log.ClanBName}");
    Console.WriteLine($"  Результат: {log.Result}");
    Console.WriteLine($"  Раундів: {log.Rounds.Count}");

    var lastRounds = log.Rounds.TakeLast(5).ToList();
    if (lastRounds.Count == 0)
    {
        Console.WriteLine("  Раундів не було.");
        return;
    }

    Console.WriteLine("  Останні раунди:");
    foreach (var round in lastRounds)
    {
        var killedText = round.WasKilled ? " (вбитий)" : string.Empty;
        Console.WriteLine($"    [{round.RoundNumber}] {round.AttackerName} -> {round.TargetName} | урон {round.Damage}, HP {round.TargetHealthAfter}{killedText}");
    }
}

static List<(string Name, string Clan, int Damage)> GetTopDamagePlayers(BattleLog log, int count)
{
    var totals = new Dictionary<Guid, (string Name, string Clan, int Damage)>();

    foreach (var round in log.Rounds)
    {
        if (!totals.TryGetValue(round.AttackerId, out var current))
        {
            totals[round.AttackerId] = (round.AttackerName, round.AttackerClan, round.Damage);
        }
        else
        {
            totals[round.AttackerId] = (current.Name, current.Clan, current.Damage + round.Damage);
        }
    }

    return totals.Values
        .OrderByDescending(item => item.Damage)
        .Take(count)
        .ToList();
}
