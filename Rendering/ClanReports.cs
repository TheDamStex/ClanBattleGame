using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Rendering;

// Абстракція звіту клану.
public abstract class ClanReport
{
    protected readonly IOutputDevice Device;

    protected ClanReport(IOutputDevice device)
    {
        Device = device;
    }

    public abstract void ShowClan(Clan clan, IEnumerable<IPlayerView> playersView, AppConfig config);

    public virtual void ShowLeader(Player leader, IPlayerView leaderView)
    {
        Device.WriteLine("Глава клану:");
        Device.WriteLine($"  Ім'я: {leaderView.GetDisplayName()}");
        Device.WriteLine($"  Id: {leader.Id}");
        Device.WriteLine($"  Раса: {leader.RaceType}");
        Device.WriteLine($"  Зброя: {leader.WeaponType}");
        Device.WriteLine($"  Пересування: {leader.MovementType}");
        Device.WriteLine($"  Стати: Атк {leader.Stats.Attack}, Зах {leader.Stats.Defense}, Шв {leader.Stats.Speed}, HP {leader.Stats.Health}");
    }
}

// Текстовий звіт клану.
public sealed class ClanTextReport : ClanReport
{
    public ClanTextReport(IOutputDevice device) : base(device)
    {
    }

    public override void ShowClan(Clan clan, IEnumerable<IPlayerView> playersView, AppConfig config)
    {
        var viewById = playersView.ToDictionary(v => v.Id, v => v);

        Device.WriteLine($"Клан: {clan.Name}");
        Device.WriteLine($"Загонів: {clan.Squads.Count}");
        Device.WriteLine(new string('-', 40));

        foreach (var squad in clan.Squads)
        {
            Device.WriteLine($"Загін #{squad.SquadId} | Тип: {squad.SquadType} | Позиція: ({squad.Position.X},{squad.Position.Y})");
            foreach (var player in squad.Players)
            {
                var view = viewById[player.Id];
                Device.WriteLine($"  Гравець {view.GetDisplayName()} ({view.RaceType}) | Id: {view.Id}");
                Device.WriteLine($"    Зброя: {view.Weapon}, Пересування: {view.Movement}");
                Device.WriteLine($"    Стати: Атк {view.Stats.Attack}, Зах {view.Stats.Defense}, Шв {view.Stats.Speed}, HP {view.Stats.Health}");
                Device.WriteLine($"    Позиція: ({view.Position.X},{view.Position.Y})");
                Device.WriteLine($"    Виконано дій: {player.ActionsPerformed}");
                Device.WriteLine($"    Стан: {player.StateType}");
                if (view.Features.Count > 0)
                {
                    var featureText = string.Join(", ", view.Features.Select(item => $"{item.Key}={item.Value}"));
                    Device.WriteLine($"    Ознаки: {featureText}");
                }
                else
                {
                    Device.WriteLine("    Ознаки: немає");
                }
            }

            Device.WriteLine(new string('-', 40));
        }
    }
}

// ASCII-звіт клану.
public sealed class ClanAsciiReport : ClanReport
{
    public ClanAsciiReport(IOutputDevice device) : base(device)
    {
    }

    public override void ShowClan(Clan clan, IEnumerable<IPlayerView> playersView, AppConfig config)
    {
        var width = config.FieldWidth;
        var height = config.FieldHeight;
        var grid = new char[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                grid[y, x] = '.';
            }
        }

        foreach (var squad in clan.Squads)
        {
            PlaceSymbol(grid, width, height, squad.Position, GetSquadSymbol(squad.SquadType));
            foreach (var player in squad.Players)
            {
                if (player.StateType == PlayerStateType.OutOfBattle)
                {
                    continue;
                }

                var playerSymbol = player.StateType == PlayerStateType.Wounded ? 'x' : GetPlayerSymbol(player.RaceType);
                PlaceSymbol(grid, width, height, player.Position, playerSymbol);
            }
        }

        Device.DrawMap(grid, "Легенда: W/E/G — загін, w/e/g — гравець, x — поранений, * — накладення, . — порожньо");

        var views = playersView.ToList();
        if (views.Count == 0)
        {
            return;
        }

        Device.WriteLine("Коротко про ознаки гравців:");
        foreach (var view in views)
        {
            var featureText = view.Features.Count == 0
                ? "немає"
                : string.Join(", ", view.Features.Select(item => $"{item.Key}={item.Value}"));
            Device.WriteLine($"  {view.Name}: {featureText}");
        }
    }

    private static char GetSquadSymbol(RaceType raceType)
    {
        return raceType switch
        {
            RaceType.Warrior => 'W',
            RaceType.Elf => 'E',
            RaceType.Dwarf => 'G',
            _ => '?'
        };
    }

    private static char GetPlayerSymbol(RaceType raceType)
    {
        return raceType switch
        {
            RaceType.Warrior => 'w',
            RaceType.Elf => 'e',
            RaceType.Dwarf => 'g',
            _ => '?'
        };
    }

    private static void PlaceSymbol(char[,] grid, int width, int height, Position position, char symbol)
    {
        var x = Math.Clamp(position.X, 0, width - 1);
        var y = Math.Clamp(position.Y, 0, height - 1);
        grid[y, x] = grid[y, x] == '.' ? symbol : '*';
    }
}
