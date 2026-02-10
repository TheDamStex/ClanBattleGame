using ClanBattleGame.Models;

namespace ClanBattleGame.Services;

// Фабрика складання представлення гравця з декораторами.
public sealed class PlayerViewFactory : IPlayerViewFactory
{
    public IPlayerView Create(Player player)
    {
        IPlayerView view = new PlayerCore(player);

        if (player.Features.TryGetValue("Колір", out var color) && !string.IsNullOrWhiteSpace(color))
        {
            view = new ColorDecorator(view, color);
        }

        if (player.Features.TryGetValue("Зріст", out var height) && !string.IsNullOrWhiteSpace(height))
        {
            view = new HeightDecorator(view, height);
        }

        if (player.Features.TryGetValue("Одяг", out var clothing) && !string.IsNullOrWhiteSpace(clothing))
        {
            view = new ClothingDecorator(view, clothing);
        }

        return view;
    }
}

// Генератор випадкових додаткових ознак гравця.
public sealed class PlayerFeatureGenerator : IPlayerFeatureGenerator
{
    private readonly IRandomProvider _randomProvider;

    public PlayerFeatureGenerator(IRandomProvider randomProvider)
    {
        _randomProvider = randomProvider;
    }

    public Dictionary<string, string> Generate(AppConfig config)
    {
        var features = new Dictionary<string, string>();

        if (ShouldAdd(config.FeatureChancePercent) && config.AvailableColors.Count > 0)
        {
            var color = config.AvailableColors[_randomProvider.NextInt(0, config.AvailableColors.Count)];
            features["Колір"] = color;
        }

        if (ShouldAdd(config.FeatureChancePercent) && config.MaxHeightCm >= config.MinHeightCm)
        {
            var height = _randomProvider.NextInt(config.MinHeightCm, config.MaxHeightCm + 1);
            features["Зріст"] = $"{height} см";
        }

        if (ShouldAdd(config.FeatureChancePercent) && config.AvailableClothingTypes.Count > 0)
        {
            var clothing = config.AvailableClothingTypes[_randomProvider.NextInt(0, config.AvailableClothingTypes.Count)];
            features["Одяг"] = clothing;
        }

        return features;
    }

    private bool ShouldAdd(int chancePercent)
    {
        var normalized = Math.Clamp(chancePercent, 0, 100);
        return _randomProvider.NextInt(1, 101) <= normalized;
    }
}
