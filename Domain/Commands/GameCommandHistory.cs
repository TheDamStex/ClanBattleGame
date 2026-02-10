namespace ClanBattleGame.Domain.Commands;

public sealed class GameCommandHistory
{
    public List<GameCommandHistoryEntry> Entries { get; set; } = new();
}

public sealed class GameCommandHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string ClanName { get; set; } = string.Empty;
    public int SquadId { get; set; }
    public string CommandType { get; set; } = string.Empty;
    public int PlayersAffected { get; set; }
    public string Summary { get; set; } = string.Empty;
}
