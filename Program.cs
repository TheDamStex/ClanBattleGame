using ClanBattleGame.Application.Battle;
using ClanBattleGame.Application.Commands;
using ClanBattleGame.Application.Memento;
using ClanBattleGame.Domain.Commands;
using ClanBattleGame.Infrastructure.Repositories;
using ClanBattleGame.Models;
using ClanBattleGame.Rendering;
using ClanBattleGame.Repositories;
using ClanBattleGame.Services;

var configPath = "config.json";
var clanPath = "clan.json";
var commandLogPath = "command_log.json";
var commandHistoryPath = "command_history.json";
var checkpointPath = "checkpoint.json";

IConfigRepository configRepository = new JsonConfigRepository();
IClanRepository clanRepository = new JsonClanRepository();
ICommandLogRepository commandLogRepository = new JsonCommandLogRepository();
IGameCommandHistoryRepository commandHistoryRepository = new JsonGameCommandHistoryRepository();

var config = configRepository.LoadOrCreate(configPath);

IDamageCalculator damageCalculator = new SimpleDamageCalculator();
var (randomProvider, clanGenerator, _, playerViewFactory) = BuildServices(config, damageCalculator);

ClanReport textReport = new ClanTextReport(new ConsoleTextDevice());
ClanReport asciiReport = new ClanAsciiReport(new ConsoleAsciiDevice());

Clan? clan = null;
CommandLog commandLog = new() { StartedAt = DateTime.UtcNow };
GameCommandHistory commandHistory = new();
var checkpointManager = new CheckpointManager();
var gameSession = new GameSession
{
    CurrentClan = "A",
    RandomSeed = config.RandomSeed,
    CommandHistory = commandHistory,
    Config = config
};

IClanMediator mediator = new ClanMediator(commandLog);
IPlayerActionExecutor actionExecutor = new PlayerActionExecutor();
ICommandHandler commandChain = BuildCommandChain();
var commander = new ClanCommander(mediator, commandChain);

var invoker = new CommandInvoker();

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
    Console.WriteLine("12) Грати: почергові команди двох кланів (N ходів)");
    Console.WriteLine("13) Undo останньої команди");
    Console.WriteLine("14) Показати історію команд (останні 20)");
    Console.WriteLine("15) Зберегти історію команд у JSON");
    Console.WriteLine("16) Завантажити історію команд з JSON");
    Console.WriteLine("17) Створити контрольну точку (checkpoint)");
    Console.WriteLine("18) Відновити контрольну точку");
    Console.WriteLine("19) Зберегти контрольні точки у JSON (checkpoint.json)");
    Console.WriteLine("20) Завантажити контрольні точки з JSON (checkpoint.json)");
    Console.WriteLine("21) Показати список контрольних точок");
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
            (randomProvider, clanGenerator, _, playerViewFactory) = BuildServices(config, damageCalculator);
            gameSession.Config = config;
            gameSession.RandomSeed = config.RandomSeed;
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
            PlayTurns();
            break;
        case "13":
            if (!invoker.UndoLast())
            {
                Console.WriteLine("Немає команди для скасування.");
            }
            else
            {
                if (commandHistory.Entries.Count > 0)
                {
                    commandHistory.Entries.RemoveAt(commandHistory.Entries.Count - 1);
                }

                Console.WriteLine("Останню команду скасовано.");
                if (gameSession.ClanA is not null && gameSession.ClanB is not null)
                {
                    ShowTwoClansMap(gameSession.ClanA, gameSession.ClanB, config);
                }
            }

            break;
        case "14":
            ShowLastHistory(commandHistory);
            break;
        case "15":
            commandHistoryRepository.Save(commandHistoryPath, commandHistory);
            Console.WriteLine("Історію команд збережено.");
            break;
        case "16":
            commandHistory = commandHistoryRepository.Load(commandHistoryPath);
            gameSession.CommandHistory = commandHistory;
            Console.WriteLine("Історію команд завантажено.");
            ShowLastHistory(commandHistory);
            break;
        case "17":
            CreateCheckpoint();
            break;
        case "18":
            RestoreCheckpoint();
            break;
        case "19":
            checkpointManager.SaveToFile(checkpointPath);
            Console.WriteLine("Контрольні точки збережено.");
            break;
        case "20":
            checkpointManager.LoadFromFile(checkpointPath);
            Console.WriteLine("Контрольні точки завантажено.");
            break;
        case "21":
            ShowCheckpoints();
            break;
        case "0":
            return;
        default:
            Console.WriteLine("Невідомий пункт меню.");
            break;
    }
}

void PlayTurns()
{
    if (gameSession.ClanA is null || gameSession.ClanB is null)
    {
        gameSession.ClanA = clanGenerator.CreateClan("Клан A", config);
        gameSession.ClanB = clanGenerator.CreateClan("Клан B", config);
        PositionClan(gameSession.ClanA, config.FieldWidth / 2 - 2, config.FieldHeight / 2, config, randomProvider);
        PositionClan(gameSession.ClanB, config.FieldWidth / 2 + 2, config.FieldHeight / 2, config, randomProvider);
        gameSession.TurnIndex = 0;
        gameSession.CurrentClan = "A";
        Console.WriteLine("Створено два клани для почергової гри.");
    }

    var defaultTurns = config.MaxTurns;
    Console.Write($"Введи кількість ходів N (Enter = {defaultTurns}): ");
    var turnsInput = Console.ReadLine();
    var turns = defaultTurns;
    if (!string.IsNullOrWhiteSpace(turnsInput) && (!int.TryParse(turnsInput, out turns) || turns <= 0))
    {
        Console.WriteLine("Некоректне N.");
        return;
    }

    for (var turn = 1; turn <= turns; turn++)
    {
        var clanLabel = gameSession.CurrentClan == "B" ? "B" : "A";
        var clanToPlay = clanLabel == "A" ? gameSession.ClanA : gameSession.ClanB;
        if (clanToPlay is null)
        {
            Console.WriteLine("Клан для ходу не знайдено.");
            return;
        }

        Console.WriteLine($"Хід {turn}/{turns}, зараз грає клан {clanLabel}");
        ExecuteClanTurn(clanToPlay, clanLabel);
        gameSession.TurnIndex += 1;
        gameSession.CurrentClan = clanLabel == "A" ? "B" : "A";

        if (gameSession.ClanA is not null && gameSession.ClanB is not null)
        {
            ShowTwoClansMap(gameSession.ClanA, gameSession.ClanB, config);
        }
    }
}

void ExecuteClanTurn(Clan clanToPlay, string label)
{
    Console.WriteLine($"Клан {label}: {clanToPlay.Name}");
    Console.WriteLine("Оберіть загін (номер) або R для випадкового:");
    foreach (var squad in clanToPlay.Squads)
    {
        Console.WriteLine($"  {squad.SquadId}) Загін {squad.SquadType}, позиція=({squad.Position.X},{squad.Position.Y})");
    }

    Console.Write("Вибір: ");
    var squadInput = Console.ReadLine();
    Squad squadToCommand;
    if (string.Equals(squadInput, "R", StringComparison.OrdinalIgnoreCase))
    {
        squadToCommand = clanToPlay.Squads[randomProvider.NextInt(0, clanToPlay.Squads.Count)];
    }
    else if (!int.TryParse(squadInput, out var squadId))
    {
        Console.WriteLine("Некоректний загін.");
        return;
    }
    else
    {
        squadToCommand = clanToPlay.Squads.FirstOrDefault(squad => squad.SquadId == squadId) ?? clanToPlay.Squads[0];
    }

    Console.WriteLine("Команда: 1-Вперед, 2-Назад, 3-Битися, R-випадково");
    Console.Write("Вибір: ");
    var cmdInput = Console.ReadLine();
    var commandType = cmdInput switch
    {
        "1" => SquadCommandType.Forward,
        "2" => SquadCommandType.Backward,
        "3" => SquadCommandType.Fight,
        _ => (SquadCommandType)randomProvider.NextInt(0, 3)
    };

    var receiver = new SquadCommandReceiver(clanToPlay, squadToCommand, config, randomProvider);
    IGameCommand command = commandType switch
    {
        SquadCommandType.Forward => new ForwardCommand(clanToPlay.Name, squadToCommand.SquadId, receiver),
        SquadCommandType.Backward => new BackwardCommand(clanToPlay.Name, squadToCommand.SquadId, receiver),
        _ => new FightCommand(clanToPlay.Name, squadToCommand.SquadId, receiver)
    };

    invoker.Enqueue(command);
    invoker.ExecuteNext();

    if (command is GameCommandBase gameCommand)
    {
        commandHistory.Entries.Add(new GameCommandHistoryEntry
        {
            Timestamp = gameCommand.CreatedAt,
            ClanName = gameCommand.ClanName,
            SquadId = gameCommand.SquadId,
            CommandType = gameCommand.Name,
            PlayersAffected = gameCommand.LastResult.PlayersAffected,
            Summary = gameCommand.LastResult.BuildSummary()
        });
    }
}

static void PositionClan(Clan clanToPosition, int centerX, int centerY, AppConfig config, IRandomProvider randomProvider)
{
    var offsets = new[]
    {
        new Position(0, 0),
        new Position(1, 0),
        new Position(-1, 0),
        new Position(0, 1),
        new Position(0, -1),
        new Position(1, 1),
        new Position(-1, -1),
        new Position(1, -1),
        new Position(-1, 1)
    };

    for (var i = 0; i < clanToPosition.Squads.Count; i++)
    {
        var offset = offsets[i % offsets.Length];
        var x = Math.Clamp(centerX + offset.X, 0, config.FieldWidth - 1);
        var y = Math.Clamp(centerY + offset.Y, 0, config.FieldHeight - 1);
        var squadPosition = new Position(x, y);
        clanToPosition.Squads[i].Position = squadPosition;

        for (var p = 0; p < clanToPosition.Squads[i].Players.Count; p++)
        {
            var px = Math.Clamp(x + randomProvider.NextInt(-1, 2), 0, config.FieldWidth - 1);
            var py = Math.Clamp(y + randomProvider.NextInt(-1, 2), 0, config.FieldHeight - 1);
            clanToPosition.Squads[i].Players[p].Position = new Position(px, py);
            clanToPosition.Squads[i].Players[p].StateType = PlayerStateType.Healthy;
        }
    }
}

static void ShowTwoClansMap(Clan clanA, Clan clanB, AppConfig config)
{
    var grid = new char[config.FieldHeight, config.FieldWidth];
    for (var y = 0; y < config.FieldHeight; y++)
    {
        for (var x = 0; x < config.FieldWidth; x++)
        {
            grid[y, x] = '.';
        }
    }

    PlaceClan(clanA, grid, config, true);
    PlaceClan(clanB, grid, config, false);

    var device = new ConsoleAsciiDevice();
    device.DrawMap(grid, "Легенда: A=w/e/g, B=W/E/G, x/X=поранений, .=порожньо, *=накладення");
}

static void PlaceClan(Clan clanToDraw, char[,] grid, AppConfig config, bool isClanA)
{
    foreach (var squad in clanToDraw.Squads)
    {
        foreach (var player in squad.Players)
        {
            if (player.StateType == PlayerStateType.OutOfBattle)
            {
                continue;
            }

            var x = Math.Clamp(player.Position.X, 0, config.FieldWidth - 1);
            var y = Math.Clamp(player.Position.Y, 0, config.FieldHeight - 1);
            var symbol = GetPlayerSymbol(player, isClanA);
            grid[y, x] = grid[y, x] == '.' ? symbol : '*';
        }
    }
}

static char GetPlayerSymbol(Player player, bool isClanA)
{
    if (player.StateType == PlayerStateType.Wounded)
    {
        return isClanA ? 'x' : 'X';
    }

    return (player.RaceType, isClanA) switch
    {
        (RaceType.Warrior, true) => 'w',
        (RaceType.Elf, true) => 'e',
        (RaceType.Dwarf, true) => 'g',
        (RaceType.Warrior, false) => 'W',
        (RaceType.Elf, false) => 'E',
        (RaceType.Dwarf, false) => 'G',
        _ => '?'
    };
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

static void ShowLastHistory(GameCommandHistory history)
{
    if (history.Entries.Count == 0)
    {
        Console.WriteLine("Історія команд порожня.");
        return;
    }

    foreach (var entry in history.Entries.TakeLast(20))
    {
        Console.WriteLine($"[{entry.Timestamp:HH:mm:ss}] {entry.ClanName}, загін #{entry.SquadId}, {entry.CommandType}, гравців: {entry.PlayersAffected}, {entry.Summary}");
    }
}

void CreateCheckpoint()
{
    if (gameSession.ClanA is null || gameSession.ClanB is null)
    {
        Console.WriteLine("Немає активної гри для створення контрольної точки.");
        return;
    }

    gameSession.Config = config;
    gameSession.RandomSeed = config.RandomSeed;
    gameSession.CommandHistory = commandHistory;

    Console.Write("Введи назву контрольної точки: ");
    var checkpointName = Console.ReadLine() ?? string.Empty;
    var checkpoint = checkpointManager.CreateCheckpoint(gameSession, checkpointName);
    Console.WriteLine($"Створено контрольну точку: {checkpoint.Name}.");
}

void RestoreCheckpoint()
{
    var checkpoints = checkpointManager.GetAll();
    if (checkpoints.Count == 0)
    {
        Console.WriteLine("Контрольних точок немає.");
        return;
    }

    for (var i = 0; i < checkpoints.Count; i++)
    {
        var point = checkpoints[i];
        Console.WriteLine($"{i + 1}) {point.Name} ({point.CreatedAt:yyyy-MM-dd HH:mm:ss})");
    }

    Console.Write("Вибери номер контрольної точки: ");
    var indexInput = Console.ReadLine();
    if (!int.TryParse(indexInput, out var selected) || selected < 1 || selected > checkpoints.Count)
    {
        Console.WriteLine("Некоректний номер.");
        return;
    }

    var chosen = checkpoints[selected - 1];
    checkpointManager.RestoreCheckpoint(gameSession, chosen);

    if (gameSession.Config is not null)
    {
        config = gameSession.Config;
        (randomProvider, clanGenerator, _, playerViewFactory) = BuildServices(config, damageCalculator);
    }

    commandHistory = gameSession.CommandHistory;
    invoker.Clear();

    Console.WriteLine($"Відновлено контрольну точку: {chosen.Name}.");
    Console.WriteLine($"Поточний хід: {gameSession.TurnIndex}, наступний клан: {gameSession.CurrentClan}.");

    if (gameSession.ClanA is not null && gameSession.ClanB is not null)
    {
        ShowTwoClansMap(gameSession.ClanA, gameSession.ClanB, config);
    }
}

void ShowCheckpoints()
{
    var checkpoints = checkpointManager.GetAll();
    if (checkpoints.Count == 0)
    {
        Console.WriteLine("Контрольних точок немає.");
        return;
    }

    for (var i = 0; i < checkpoints.Count; i++)
    {
        var point = checkpoints[i];
        Console.WriteLine($"{i + 1}) {point.Name} — {point.CreatedAt:yyyy-MM-dd HH:mm:ss}");
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
    config.HitChancePercent = ReadInt("Ймовірність влучання (%)", config.HitChancePercent);
    config.MaxTurns = ReadInt("Типова кількість ходів", config.MaxTurns);

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
    Console.WriteLine($"  Шанс влучання: {config.HitChancePercent}%");
    Console.WriteLine($"  Типові ходи: {config.MaxTurns}");
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
