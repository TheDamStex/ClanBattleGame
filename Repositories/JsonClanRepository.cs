using System.Text.Json;
using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Repositories;

// Робота з JSON для збереження та завантаження клану.
public sealed class JsonClanRepository : IClanRepository
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public void Save(string path, Clan clan)
    {
        var dto = ClanDto.FromDomain(clan);
        var json = JsonSerializer.Serialize(dto, _options);
        File.WriteAllText(path, json);
    }

    public Clan? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<ClanDto>(json, _options);
            return dto?.ToDomain();
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}

// DTO клану для простого JSON-представлення.
public sealed class ClanDto
{
    public string Name { get; set; } = string.Empty;
    public List<SquadDto> Squads { get; set; } = new();
    public Guid? LeaderId { get; set; }
    public LeaderSnapshot? LeaderSnapshot { get; set; }

    public static ClanDto FromDomain(Clan clan)
    {
        return new ClanDto
        {
            Name = clan.Name,
            LeaderId = clan.LeaderId,
            LeaderSnapshot = clan.LeaderSnapshot,
            Squads = clan.Squads.Select(SquadDto.FromDomain).ToList()
        };
    }

    public Clan ToDomain()
    {
        return new Clan
        {
            Name = Name,
            LeaderId = LeaderId,
            LeaderSnapshot = LeaderSnapshot,
            Squads = Squads.Select(s => s.ToDomain()).ToList()
        };
    }
}

// DTO загону для простого JSON-представлення.
public sealed class SquadDto
{
    public int SquadId { get; set; }
    public RaceType SquadType { get; set; }
    public Position Position { get; set; }
    public List<PlayerDto> Players { get; set; } = new();

    public static SquadDto FromDomain(Squad squad)
    {
        return new SquadDto
        {
            SquadId = squad.SquadId,
            SquadType = squad.SquadType,
            Position = squad.Position,
            Players = squad.Players.Select(PlayerDto.FromDomain).ToList()
        };
    }

    public Squad ToDomain()
    {
        return new Squad
        {
            SquadId = SquadId,
            SquadType = SquadType,
            Position = Position,
            Players = Players.Select(p => p.ToDomain()).ToList()
        };
    }
}

// DTO гравця для простого JSON-представлення.
public sealed class PlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RaceType RaceType { get; set; }
    public WeaponType WeaponType { get; set; }
    public MovementType MovementType { get; set; }
    public Stats Stats { get; set; } = new();
    public Position Position { get; set; }
    public Dictionary<string, string> Features { get; set; } = new();

    public static PlayerDto FromDomain(Player player)
    {
        return new PlayerDto
        {
            Id = player.Id,
            Name = player.Name,
            RaceType = player.RaceType,
            WeaponType = player.WeaponType,
            MovementType = player.MovementType,
            Stats = player.Stats,
            Position = player.Position,
            Features = new Dictionary<string, string>(player.Features)
        };
    }

    public Player ToDomain()
    {
        return new Player
        {
            Id = Id,
            Name = Name,
            RaceType = RaceType,
            WeaponType = WeaponType,
            MovementType = MovementType,
            Stats = Stats,
            Position = Position,
            Features = new Dictionary<string, string>(Features)
        };
    }
}
