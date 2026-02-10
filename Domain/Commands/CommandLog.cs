using ClanBattleGame.Models;

namespace ClanBattleGame.Domain.Commands;

public sealed class CommandLog
{
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public string ClanName { get; set; } = string.Empty;
    public List<CommandLogEntry> Entries { get; set; } = new();
}

public sealed class CommandLogEntry
{
    public DateTime Timestamp { get; set; }
    public int SquadId { get; set; }
    public string SquadType { get; set; } = string.Empty;
    public SquadCommandType CommandType { get; set; }
    public int PlayersAffected { get; set; }
    public string Summary { get; set; } = string.Empty;
}
