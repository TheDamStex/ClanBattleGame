namespace ClanBattleGame.Models;

public sealed class Squad
{
    public int SquadId { get; set; }
    public RaceType SquadType { get; set; }
    public Position Position { get; set; }
    public List<Player> Players { get; set; } = new();
}

public sealed class Clan
{
    public string Name { get; set; } = string.Empty;
    public List<Squad> Squads { get; set; } = new();
    public Guid? LeaderId { get; set; }
    public LeaderSnapshot? LeaderSnapshot { get; set; }
}
