using ClanBattleGame.Models;

namespace ClanBattleGame.Services;

// Представлення гравця з можливістю декорування.
public interface IPlayerView
{
    Guid Id { get; }
    string Name { get; }
    RaceType RaceType { get; }
    Stats Stats { get; }
    Position Position { get; }
    string Weapon { get; }
    string Movement { get; }
    IReadOnlyDictionary<string, string> Features { get; }
    string GetDisplayName();
    string GetFullDescription();
}

// Базова обгортка над доменним гравцем.
public sealed class PlayerCore : IPlayerView
{
    private readonly Player _player;

    public PlayerCore(Player player)
    {
        _player = player;
    }

    public Guid Id => _player.Id;
    public string Name => _player.Name;
    public RaceType RaceType => _player.RaceType;
    public Stats Stats => _player.Stats;
    public Position Position => _player.Position;
    public string Weapon => _player.WeaponType.ToString();
    public string Movement => _player.MovementType.ToString();
    public IReadOnlyDictionary<string, string> Features => _player.Features;

    public string GetDisplayName()
    {
        return Name;
    }

    public string GetFullDescription()
    {
        return $"{Name} ({RaceType})";
    }
}

// Базовий декоратор гравця.
public abstract class PlayerDecoratorBase : IPlayerView
{
    protected readonly IPlayerView Inner;

    protected PlayerDecoratorBase(IPlayerView inner)
    {
        Inner = inner;
    }

    public virtual Guid Id => Inner.Id;
    public virtual string Name => Inner.Name;
    public virtual RaceType RaceType => Inner.RaceType;
    public virtual Stats Stats => Inner.Stats;
    public virtual Position Position => Inner.Position;
    public virtual string Weapon => Inner.Weapon;
    public virtual string Movement => Inner.Movement;
    public virtual IReadOnlyDictionary<string, string> Features => Inner.Features;

    public virtual string GetDisplayName() => Inner.GetDisplayName();

    public virtual string GetFullDescription() => Inner.GetFullDescription();
}

// Декоратор кольору.
public sealed class ColorDecorator : PlayerDecoratorBase
{
    private readonly string _color;

    public ColorDecorator(IPlayerView inner, string color) : base(inner)
    {
        _color = color;
    }

    public override string GetDisplayName()
    {
        return $"{Inner.GetDisplayName()} [{_color}]";
    }

    public override string GetFullDescription()
    {
        return $"{Inner.GetFullDescription()}, Колір: {_color}";
    }
}

// Декоратор зросту.
public sealed class HeightDecorator : PlayerDecoratorBase
{
    private readonly string _height;

    public HeightDecorator(IPlayerView inner, string height) : base(inner)
    {
        _height = height;
    }

    public override string GetDisplayName()
    {
        return $"{Inner.GetDisplayName()} [{_height}]";
    }

    public override string GetFullDescription()
    {
        return $"{Inner.GetFullDescription()}, Зріст: {_height}";
    }
}

// Декоратор одягу.
public sealed class ClothingDecorator : PlayerDecoratorBase
{
    private readonly string _clothing;

    public ClothingDecorator(IPlayerView inner, string clothing) : base(inner)
    {
        _clothing = clothing;
    }

    public override string GetDisplayName()
    {
        return $"{Inner.GetDisplayName()} [{_clothing}]";
    }

    public override string GetFullDescription()
    {
        return $"{Inner.GetFullDescription()}, Одяг: {_clothing}";
    }
}
