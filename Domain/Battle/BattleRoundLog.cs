namespace ClanBattleGame.Domain.Battle;

// Описує один хід у раунді бою.
public sealed class BattleRoundLog
{
    public int RoundNumber { get; set; }
    public Guid AttackerId { get; set; }
    public string AttackerName { get; set; } = string.Empty;
    public string AttackerClan { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string TargetClan { get; set; } = string.Empty;
    public int Damage { get; set; }
    public int TargetHealthAfter { get; set; }
    public bool WasKilled { get; set; }
}
