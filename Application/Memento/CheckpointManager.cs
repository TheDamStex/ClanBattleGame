using System.Text.Json;

namespace ClanBattleGame.Application.Memento;

public sealed class CheckpointManager
{
    private readonly List<GameCheckpointMemento> _checkpoints = new();

    public GameCheckpointMemento CreateCheckpoint(GameSession session, string name)
    {
        var checkpointName = string.IsNullOrWhiteSpace(name)
            ? $"Контрольна точка {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            : name;

        var memento = session.CreateMemento(checkpointName);
        _checkpoints.Add(memento);
        return memento;
    }

    public void RestoreCheckpoint(GameSession session, GameCheckpointMemento memento)
    {
        session.RestoreFromMemento(memento);
    }

    public List<GameCheckpointMemento> GetAll()
    {
        return _checkpoints;
    }

    public void SaveToFile(string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(_checkpoints, options);
        File.WriteAllText(path, json);
    }

    public void LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            _checkpoints.Clear();
            File.WriteAllText(path, "[]");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<List<GameCheckpointMemento>>(json) ?? new List<GameCheckpointMemento>();
            _checkpoints.Clear();
            _checkpoints.AddRange(loaded);
        }
        catch (JsonException)
        {
            Console.WriteLine("Файл контрольних точок пошкоджений. Завантаження пропущено.");
        }
    }
}
