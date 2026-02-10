namespace ClanBattleGame.Models;

public sealed class Player
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RaceType RaceType { get; set; }
    public WeaponType WeaponType { get; set; }
    public MovementType MovementType { get; set; }
    public Stats Stats { get; set; } = new();
    public Position Position { get; set; }
    public Dictionary<string, string> Features { get; set; } = new();
}

public sealed class LeaderSnapshot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RaceType RaceType { get; set; }
    public WeaponType WeaponType { get; set; }
    public MovementType MovementType { get; set; }
    public Stats Stats { get; set; } = new();
}
