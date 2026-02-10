using ClanBattleGame.Models;

namespace ClanBattleGame.Services;

public interface IRandomProvider
{
    int NextInt(int minInclusive, int maxExclusive);
}

public interface IClanGenerator
{
    Clan CreateClan(string name, AppConfig config);
}

public interface IClanRepository
{
    void Save(string path, Clan clan);
    Clan? Load(string path);
}

public interface IConfigRepository
{
    AppConfig LoadOrCreate(string path);
    void Save(string path, AppConfig config);
}

public interface IPlayerFactory
{
    Player CreatePlayer(int index, Position squadPosition, AppConfig config);
    RaceType RaceType { get; }
}

public interface IPlayerViewFactory
{
    IPlayerView Create(Player player);
}

public interface IPlayerFeatureGenerator
{
    Dictionary<string, string> Generate(AppConfig config);
}
