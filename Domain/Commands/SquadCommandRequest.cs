using ClanBattleGame.Models;

namespace ClanBattleGame.Domain.Commands;

public sealed class SquadCommandRequest
{
    public string ClanName { get; set; } = string.Empty;
    public int SquadId { get; set; }
    public SquadCommandType CommandType { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Note { get; set; }
    public int Steps { get; set; } = 1;
}
