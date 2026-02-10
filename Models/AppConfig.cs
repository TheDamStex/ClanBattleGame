namespace ClanBattleGame.Models;

public sealed class RaceStatRanges
{
    public StatsRange Warrior { get; set; } = new();
    public StatsRange Elf { get; set; } = new();
    public StatsRange Dwarf { get; set; } = new();
}

public sealed class AppConfig
{
    public int FieldWidth { get; set; } = 40;
    public int FieldHeight { get; set; } = 15;
    public int SquadCount { get; set; } = 4;
    public int MinPlayersPerSquad { get; set; } = 3;
    public int MaxPlayersPerSquad { get; set; } = 8;
    public int MaxRounds { get; set; } = 50;
    public int? RandomSeed { get; set; }
    public RaceStatRanges StatRanges { get; set; } = new();
    public List<string> AvailableColors { get; set; } = new();
    public List<string> AvailableClothingTypes { get; set; } = new();
    public int MinHeightCm { get; set; } = 150;
    public int MaxHeightCm { get; set; } = 210;
    public int FeatureChancePercent { get; set; } = 50;
}
