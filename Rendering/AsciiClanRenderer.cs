using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Rendering;

public sealed class AsciiClanRenderer : IClanRenderer
{
    public void Render(Clan clan, AppConfig config)
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
            var squadSymbol = GetSquadSymbol(squad.SquadType);
            PlaceSymbol(grid, width, height, squad.Position, squadSymbol);

            foreach (var player in squad.Players)
            {
                var playerSymbol = GetPlayerSymbol(player.RaceType);
                PlaceSymbol(grid, width, height, player.Position, playerSymbol);
            }
        }

        PrintBorder(width);
        for (var y = 0; y < height; y++)
        {
            Console.Write("|");
            for (var x = 0; x < width; x++)
            {
                Console.Write(grid[y, x]);
            }
            Console.WriteLine("|");
        }
        PrintBorder(width);

        Console.WriteLine("Легенда: W/E/G — загін, w/e/g — гравець, * — накладення, . — порожньо");
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

    private static void PrintBorder(int width)
    {
        Console.Write("+");
        Console.Write(new string('-', width));
        Console.WriteLine("+");
    }
}
