using ClanBattleGame.Application.Battle;
using ClanBattleGame.Application.Commands;
using ClanBattleGame.Domain.Battle;
using ClanBattleGame.Domain.Commands;
using ClanBattleGame.Infrastructure.Repositories;
using ClanBattleGame.Models;
using ClanBattleGame.Rendering;
using ClanBattleGame.Repositories;
using ClanBattleGame.Services;

var configPath = "config.json";
var clanPath = "clan.json";
var battleLogPath = "battle_log.json";
var commandLogPath = "command_log.json";

IConfigRepository configRepository = new JsonConfigRepository();
IClanRepository clanRepository = new JsonClanRepository();
IBattleLogRepository battleLogRepository = new JsonBattleLogRepository();
ICommandLogRepository commandLogRepository = new JsonCommandLogRepository();

var config = configRepository.LoadOrCreate(configPath);

IDamageCalculator damageCalculator = new SimpleDamageCalculator();
var (randomProvider, clanGenerator, battleSimulator, playerViewFactory) = BuildServices(config, damageCalculator);

ClanReport textReport = new ClanTextReport(new ConsoleTextDevice());
ClanReport asciiReport = new ClanAsciiReport(new ConsoleAsciiDevice());

Clan? clan = null;
BattleLog? lastBattleLog = null;
CommandLog commandLog = new() { StartedAt = DateTime.UtcNow };

IClanMediator mediator = new ClanMediator(commandLog);
IPlayerActionExecutor actionExecutor = new PlayerActionExecutor();
ICommandHandler commandChain = BuildCommandChain();
var commander = new ClanCommander(mediator, commandChain);

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
    Console.WriteLine("8) Глава віддає випадкові команди (N кроків)");
    Console.WriteLine("9) Показати останній лог команд");
    Console.WriteLine("10) Зберегти лог у JSON (command_log.json)");
    Console.WriteLine("11) Завантажити лог з JSON (command_log.json)");
    Console.WriteLine("12) Симуляція бою двох кланів (створити 2 клани і провести бій)");
    Console.WriteLine("13) Показати останній журнал бою (з JSON)");
    Console.WriteLine("14) Зберегти журнал бою у JSON (battle_log.json)");
    Console.WriteLine("15) Завантажити журнал бою з JSON (battle_log.json)");
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
            commandLog = new CommandLog { StartedAt = DateTime.UtcNow, ClanName = clan.Name };
            mediator = BuildMediator(clan, config, actionExecutor, commandLog);
            commander = new ClanCommander(mediator, commandChain);
            Console.WriteLine("Клан створено.");
            break;
        case "2":
            if (EnsureClan(clan))
            {
                var views = BuildViews(clan!, playerViewFactory);
                textReport.ShowClan(clan!, views, config);
            }
            break;
        case "3":
            if (EnsureClan(clan))
            {
                var views = BuildViews(clan!, playerViewFactory);
                asciiReport.ShowClan(clan!, views, config);
            }
            break;
        case "4":
            if (EnsureClan(clan))
            {
                ShowLeader(textReport, playerViewFactory);
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
                commandLog = new CommandLog { StartedAt = DateTime.UtcNow, ClanName = clan.Name };
                mediator = BuildMediator(clan, config, actionExecutor, commandLog);
                commander = new ClanCommander(mediator, commandChain);
                Console.WriteLine("Клан завантажено.");
            }
            break;
        case "7":
            EditConfig(config, configRepository, configPath);
            config = configRepository.LoadOrCreate(configPath);
            (randomProvider, clanGenerator, battleSimulator, playerViewFactory) = BuildServices(config, damageCalculator);
            if (clan is not null)
            {
                mediator = BuildMediator(clan, config, actionExecutor, commandLog);
                commander = new ClanCommander(mediator, commandChain);
            }
            Console.WriteLine("Налаштування оновлено.");
            break;
        case "8":
            if (!EnsureClan(clan))
            {
                break;
            }

            var leader = LeaderManager.Instance.Leader;
            if (leader is null)
            {
                Console.WriteLine("Глава клану не визначений.");
                break;
            }

            Console.Write("Введи кількість кроків N: ");
            if (!int.TryParse(Console.ReadLine(), out var stepsCount) || stepsCount <= 0)
            {
                Console.WriteLine("Некоректне N.");
                break;
            }

            for (var step = 1; step <= stepsCount; step++)
            {
                foreach (var squad in clan!.Squads)
                {
                    var context = new CommandContext
                    {
                        Clan = clan!,
                        TargetSquad = squad,
                        Leader = leader,
                        Random = randomProvider,
                        Config = config,
                        IsEnemyNear = randomProvider.NextInt(0, 100) < 35,
                        IsInDanger = randomProvider.NextInt(0, 100) < 20,
                        IsTooFarForward = squad.Position.X > config.FieldWidth * 3 / 4
                    };

                    commander.IssueGeneratedCommand(context);
                }
            }

            PrintCommandSeriesSummary(clan!, commandLog);
            Console.Write("Показати лог зараз? (y/n): ");
            if (string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase))
            {
                ShowCommandLog(commandLog);
            }
            break;
        case "9":
            ShowCommandLog(commandLog);
            break;
        case "10":
            commandLogRepository.Save(commandLogPath, commandLog);
            Console.WriteLine("Лог команд збережено.");
            break;
        case "11":
            commandLog = commandLogRepository.Load(commandLogPath);
            Console.WriteLine("Лог команд завантажено.");
            ShowCommandLog(commandLog);
            break;
        case "12":
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
        case "13":
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
        case "14":
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
        case "15":
            var loadedBattleLog = battleLogRepository.Load(battleLogPath);
            if (loadedBattleLog is null)
            {
                Console.WriteLine("Не вдалося завантажити журнал бою.");
            }
            else
            {
                lastBattleLog = loadedBattleLog;
                Console.WriteLine("Журнал бою завантажено.");
                ShowBattleLogShort(lastBattleLog);
            }
            break;
        case "0":
            return;
        default:
            Console.WriteLine("Невідомий пункт меню.");
            break;
    }
}

static IClanMediator BuildMediator(Clan clan, AppConfig config, IPlayerActionExecutor actionExecutor, CommandLog commandLog)
{
    var mediator = new ClanMediator(commandLog);
    mediator.RegisterClan(clan);
    foreach (var squad in clan.Squads)
    {
        mediator.RegisterSquad(new SquadParticipant(squad, config, actionExecutor));
    }

    return mediator;
}

static ICommandHandler BuildCommandChain()
{
    var fight = new FightCommandHandler();
    var backward = new BackwardCommandHandler();
    var forward = new ForwardCommandHandler();
    var fallback = new DefaultCommandHandler();

    fight.SetNext(backward).SetNext(forward).SetNext(fallback);
    return fight;
}

static List<IPlayerView> BuildViews(Clan clan, IPlayerViewFactory playerViewFactory)
{
    return clan.Squads
        .SelectMany(s => s.Players)
        .Select(playerViewFactory.Create)
        .ToList();
}

static bool EnsureClan(Clan? clan)
{
    if (clan is null)
    {
        Console.WriteLine("Спочатку створи або завантаж клан.");
        return false;
    }

    return true;
}

static void ShowLeader(ClanReport report, IPlayerViewFactory playerViewFactory)
{
    var leader = LeaderManager.Instance.Leader;
    if (leader is null)
    {
        Console.WriteLine("Глава клану не визначений.");
        return;
    }

    var leaderView = playerViewFactory.Create(leader);
    report.ShowLeader(leader, leaderView);
}

static (IRandomProvider, IClanGenerator, IBattleSimulator, IPlayerViewFactory) BuildServices(AppConfig config, IDamageCalculator damageCalculator)
{
    var randomProvider = new DefaultRandomProvider(config.RandomSeed);
    var featureGenerator = new PlayerFeatureGenerator(randomProvider);
    var factories = new IPlayerFactory[]
    {
        new WarriorPlayerFactory(randomProvider),
        new ElfPlayerFactory(randomProvider),
        new DwarfPlayerFactory(randomProvider)
    };

    var generator = new ClanGenerator(randomProvider, factories, featureGenerator);
    var battleSimulator = new BattleSimulator(randomProvider, damageCalculator);
    var playerViewFactory = new PlayerViewFactory();
    return (randomProvider, generator, battleSimulator, playerViewFactory);
}

static void PrintCommandSeriesSummary(Clan clan, CommandLog log)
{
    Console.WriteLine($"Виконано команд: {log.Entries.Count}");
    foreach (var squad in clan.Squads)
    {
        var actions = squad.Players.Sum(player => player.ActionsPerformed);
        Console.WriteLine($"Загін #{squad.SquadId}: позиція=({squad.Position.X},{squad.Position.Y}), дій Fight={actions}");
    }
}

static void ShowCommandLog(CommandLog log)
{
    Console.WriteLine($"Клан: {log.ClanName}");
    Console.WriteLine($"Початок: {log.StartedAt:yyyy-MM-dd HH:mm:ss}");
    if (log.Entries.Count == 0)
    {
        Console.WriteLine("Лог порожній.");
        return;
    }

    foreach (var entry in log.Entries)
    {
        Console.WriteLine($"[{entry.Timestamp:HH:mm:ss}] Загін #{entry.SquadId} ({entry.SquadType}) -> {entry.CommandType}; Гравців: {entry.PlayersAffected}; {entry.Summary}");
    }
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
    config.MinHeightCm = ReadInt("Мінімальний зріст (см)", config.MinHeightCm);
    config.MaxHeightCm = ReadInt("Максимальний зріст (см)", config.MaxHeightCm);
    config.FeatureChancePercent = ReadInt("Ймовірність ознаки (%)", config.FeatureChancePercent);

    Console.Write($"Кольори через кому (поточні: {string.Join(", ", config.AvailableColors)}), Enter щоб лишити: ");
    var colorsInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(colorsInput))
    {
        config.AvailableColors = colorsInput
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    Console.Write($"Типи одягу через кому (поточні: {string.Join(", ", config.AvailableClothingTypes)}), Enter щоб лишити: ");
    var clothingInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(clothingInput))
    {
        config.AvailableClothingTypes = clothingInput
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

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
    Console.WriteLine($"  Зріст: {config.MinHeightCm}-{config.MaxHeightCm} см");
    Console.WriteLine($"  Шанс ознаки: {config.FeatureChancePercent}%");
    Console.WriteLine($"  Кольори: {string.Join(", ", config.AvailableColors)}");
    Console.WriteLine($"  Типи одягу: {string.Join(", ", config.AvailableClothingTypes)}");
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
