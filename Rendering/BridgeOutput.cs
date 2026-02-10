namespace ClanBattleGame.Rendering;

// Пристрій виводу для мосту.
public interface IOutputDevice
{
    void WriteLine(string text);
    void Write(string text);
    void Clear();
    void DrawMap(char[,] grid, string legend);
}

// Текстовий пристрій виводу в консоль.
public sealed class ConsoleTextDevice : IOutputDevice
{
    public void WriteLine(string text) => Console.WriteLine(text);

    public void Write(string text) => Console.Write(text);

    public void Clear() => Console.Clear();

    public void DrawMap(char[,] grid, string legend)
    {
        for (var y = 0; y < grid.GetLength(0); y++)
        {
            for (var x = 0; x < grid.GetLength(1); x++)
            {
                Console.Write(grid[y, x]);
            }

            Console.WriteLine();
        }

        Console.WriteLine(legend);
    }
}

// ASCII-пристрій виводу в консоль.
public sealed class ConsoleAsciiDevice : IOutputDevice
{
    public void WriteLine(string text) => Console.WriteLine(text);

    public void Write(string text) => Console.Write(text);

    public void Clear() => Console.Clear();

    public void DrawMap(char[,] grid, string legend)
    {
        var width = grid.GetLength(1);

        Console.Write("+");
        Console.Write(new string('-', width));
        Console.WriteLine("+");

        for (var y = 0; y < grid.GetLength(0); y++)
        {
            Console.Write("|");
            for (var x = 0; x < width; x++)
            {
                Console.Write(grid[y, x]);
            }

            Console.WriteLine("|");
        }

        Console.Write("+");
        Console.Write(new string('-', width));
        Console.WriteLine("+");
        Console.WriteLine(legend);
    }
}
