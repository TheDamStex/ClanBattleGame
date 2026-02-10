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
            var symbol = GetSymbol(squad.SquadType);
            var x = Math.Clamp(squad.Position.X, 0, width - 1);
            var y = Math.Clamp(squad.Position.Y, 0, height - 1);

            grid[y, x] = grid[y, x] == '.' ? symbol : '*';
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

        Console.WriteLine("Легенда: W - воїни, E - ельфи, G - гноми, * - накладення, . - порожньо");
    }

    private static char GetSymbol(RaceType raceType)
    {
        return raceType switch
        {
            RaceType.Warrior => 'W',
            RaceType.Elf => 'E',
            RaceType.Dwarf => 'G',
            _ => '?'
        };
    }

    private static void PrintBorder(int width)
    {
        Console.Write("+");
        Console.Write(new string('-', width));
        Console.WriteLine("+");
    }
}
