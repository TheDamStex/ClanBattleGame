using ClanBattleGame.Models;

namespace ClanBattleGame.Services;

public abstract class PlayerFactoryBase : IPlayerFactory
{
    private readonly IRandomProvider _randomProvider;
    private readonly PlayerPrototype _prototype;

    protected PlayerFactoryBase(IRandomProvider randomProvider, PlayerPrototype prototype)
    {
        _randomProvider = randomProvider;
        _prototype = prototype;
    }

    public abstract RaceType RaceType { get; }

    public Player CreatePlayer(int index, Position squadPosition, AppConfig config)
    {
        var player = _prototype.Clone();
        player.Id = Guid.NewGuid();
        player.Name = $"{RaceType}-{index}";
        player.Stats = CreateStats(config);
        player.Position = CreatePosition(squadPosition, config);
        return player;
    }

    protected abstract StatsRange GetRange(AppConfig config);

    private Stats CreateStats(AppConfig config)
    {
        var range = GetRange(config);
        return new Stats
        {
            Attack = _randomProvider.NextInt(range.MinAttack, range.MaxAttack + 1),
            Defense = _randomProvider.NextInt(range.MinDefense, range.MaxDefense + 1),
            Speed = _randomProvider.NextInt(range.MinSpeed, range.MaxSpeed + 1),
            Health = _randomProvider.NextInt(range.MinHealth, range.MaxHealth + 1)
        };
    }

    private Position CreatePosition(Position squadPosition, AppConfig config)
    {
        var offsetX = _randomProvider.NextInt(-1, 2);
        var offsetY = _randomProvider.NextInt(-1, 2);
        var x = Math.Clamp(squadPosition.X + offsetX, 0, config.FieldWidth - 1);
        var y = Math.Clamp(squadPosition.Y + offsetY, 0, config.FieldHeight - 1);
        return new Position(x, y);
    }
}

public sealed class WarriorPlayerFactory : PlayerFactoryBase
{
    public WarriorPlayerFactory(IRandomProvider randomProvider)
        : base(randomProvider, new PlayerPrototype(new Player
        {
            RaceType = RaceType.Warrior,
            WeaponType = WeaponType.Sword,
            MovementType = MovementType.Foot,
            Stats = new Stats { Attack = 10, Defense = 8, Speed = 5, Health = 20 }
        }))
    {
    }

    public override RaceType RaceType => RaceType.Warrior;

    protected override StatsRange GetRange(AppConfig config) => config.StatRanges.Warrior;
}

public sealed class ElfPlayerFactory : PlayerFactoryBase
{
    public ElfPlayerFactory(IRandomProvider randomProvider)
        : base(randomProvider, new PlayerPrototype(new Player
        {
            RaceType = RaceType.Elf,
            WeaponType = WeaponType.Bow,
            MovementType = MovementType.Fly,
            Stats = new Stats { Attack = 8, Defense = 5, Speed = 10, Health = 15 }
        }))
    {
    }

    public override RaceType RaceType => RaceType.Elf;

    protected override StatsRange GetRange(AppConfig config) => config.StatRanges.Elf;
}

public sealed class DwarfPlayerFactory : PlayerFactoryBase
{
    public DwarfPlayerFactory(IRandomProvider randomProvider)
        : base(randomProvider, new PlayerPrototype(new Player
        {
            RaceType = RaceType.Dwarf,
            WeaponType = WeaponType.Axe,
            MovementType = MovementType.Foot,
            Stats = new Stats { Attack = 9, Defense = 10, Speed = 4, Health = 22 }
        }))
    {
    }

    public override RaceType RaceType => RaceType.Dwarf;

    protected override StatsRange GetRange(AppConfig config) => config.StatRanges.Dwarf;
}
