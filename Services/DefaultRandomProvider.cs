namespace ClanBattleGame.Services;

public sealed class DefaultRandomProvider : IRandomProvider
{
    private readonly Random _random;

    public DefaultRandomProvider(int? seed)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        return _random.Next(minInclusive, maxExclusive);
    }
}
