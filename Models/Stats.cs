namespace ClanBattleGame.Models;

public sealed class Stats
{
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Health { get; set; }
}

public sealed class StatsRange
{
    public int MinAttack { get; set; }
    public int MaxAttack { get; set; }
    public int MinDefense { get; set; }
    public int MaxDefense { get; set; }
    public int MinSpeed { get; set; }
    public int MaxSpeed { get; set; }
    public int MinHealth { get; set; }
    public int MaxHealth { get; set; }
}
