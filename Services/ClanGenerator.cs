using ClanBattleGame.Models;

namespace ClanBattleGame.Services;

// Відповідає за створення структури клану.
public sealed class ClanGenerator : IClanGenerator
{
    private readonly IRandomProvider _randomProvider;
    private readonly IReadOnlyList<IPlayerFactory> _factories;

    public ClanGenerator(IRandomProvider randomProvider, IEnumerable<IPlayerFactory> factories)
    {
        _randomProvider = randomProvider;
        _factories = factories.ToList();
    }

    public Clan CreateClan(string name, AppConfig config)
    {
        var clan = new Clan
        {
            Name = name
        };

        var requiredTypes = new[] { RaceType.Warrior, RaceType.Elf, RaceType.Dwarf };
        var totalSquads = config.SquadCount;

        for (var squadIndex = 0; squadIndex < totalSquads; squadIndex++)
        {
            var squadType = squadIndex < requiredTypes.Length
                ? requiredTypes[squadIndex]
                : _factories[_randomProvider.NextInt(0, _factories.Count)].RaceType;
            var squadPosition = new Position(
                _randomProvider.NextInt(0, config.FieldWidth),
                _randomProvider.NextInt(0, config.FieldHeight));

            var squad = new Squad
            {
                SquadId = squadIndex + 1,
                SquadType = squadType,
                Position = squadPosition
            };

            var factory = _factories.First(f => f.RaceType == squadType);
            var playersCount = _randomProvider.NextInt(config.MinPlayersPerSquad, config.MaxPlayersPerSquad + 1);

            for (var playerIndex = 0; playerIndex < playersCount; playerIndex++)
            {
                squad.Players.Add(factory.CreatePlayer(playerIndex + 1, squadPosition, config));
            }

            clan.Squads.Add(squad);
        }

        return clan;
    }
}
