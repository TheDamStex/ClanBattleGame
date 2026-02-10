namespace ClanBattleGame.Domain.Battle;

// Журнал бою між двома кланами.
public sealed class BattleLog
{
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public string ClanAName { get; set; } = string.Empty;
    public string ClanBName { get; set; } = string.Empty;
    public List<BattleRoundLog> Rounds { get; set; } = new();
    public string Result { get; set; } = string.Empty;
    public int AliveCountA { get; set; }
    public int AliveCountB { get; set; }
}
