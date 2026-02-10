using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Rendering;

// Сумісний обгортковий рендерер на основі мосту.
public sealed class TextClanRenderer
{
    private readonly ClanTextReport _report = new(new ConsoleTextDevice());

    public void Render(Clan clan, AppConfig config, IEnumerable<IPlayerView> views)
    {
        _report.ShowClan(clan, views, config);
    }
}
