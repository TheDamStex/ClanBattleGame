using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Rendering;

public sealed class TextClanRenderer : IClanRenderer
{
    public void Render(Clan clan, AppConfig config)
    {
        Console.WriteLine($"Клан: {clan.Name}");
        Console.WriteLine($"Загонів: {clan.Squads.Count}");
        Console.WriteLine(new string('-', 40));

        foreach (var squad in clan.Squads)
        {
            Console.WriteLine($"Загін #{squad.SquadId} | Тип: {squad.SquadType} | Позиція: ({squad.Position.X},{squad.Position.Y})");
            foreach (var player in squad.Players)
            {
                Console.WriteLine($"  Гравець {player.Name} ({player.RaceType}) | Id: {player.Id}");
                Console.WriteLine($"    Зброя: {player.WeaponType}, Пересування: {player.MovementType}");
                Console.WriteLine($"    Стати: Атк {player.Stats.Attack}, Зах {player.Stats.Defense}, Шв {player.Stats.Speed}, HP {player.Stats.Health}");
                Console.WriteLine($"    Позиція: ({player.Position.X},{player.Position.Y})");
            }

            Console.WriteLine(new string('-', 40));
        }
    }
}
