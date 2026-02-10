using ClanBattleGame.Models;
using ClanBattleGame.Services;

namespace ClanBattleGame.Rendering;

// Сумісний обгортковий рендерер на основі мосту.
public sealed class AsciiClanRenderer
{
    private readonly ClanAsciiReport _report = new(new ConsoleAsciiDevice());

    public void Render(Clan clan, AppConfig config, IEnumerable<IPlayerView> views)
    {
        _report.ShowClan(clan, views, config);
    }
}
