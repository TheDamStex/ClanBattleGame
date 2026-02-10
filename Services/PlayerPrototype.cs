using ClanBattleGame.Models;

namespace ClanBattleGame.Services;

// Prototype для швидкого копіювання гравця.
public sealed class PlayerPrototype
{
    private readonly Player _template;

    public PlayerPrototype(Player template)
    {
        _template = template;
    }

    public Player Clone()
    {
        return new Player
        {
            Id = Guid.Empty,
            Name = string.Empty,
            RaceType = _template.RaceType,
            WeaponType = _template.WeaponType,
            MovementType = _template.MovementType,
            Stats = new Stats
            {
                Attack = _template.Stats.Attack,
                Defense = _template.Stats.Defense,
                Speed = _template.Stats.Speed,
                Health = _template.Stats.Health
            },
            Position = _template.Position
        };
    }
}
