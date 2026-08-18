using Discord.WebSocket;

public class GameManager
{
    private readonly Dictionary<ulong, IGame> activeGames = new();

    public bool HasGame(ulong guildId)
    {
        return activeGames.ContainsKey(guildId);
    }

    public IGame? GetGame(ulong guildId)
    {
        activeGames.TryGetValue(guildId, out IGame? game);

        return game;
    }

    public void AddGame(ulong guildId, IGame game)
    {
        activeGames[guildId] = game;
    }

    public void RemoveGame(ulong guildId)
    {
        activeGames.Remove(guildId);
    }
}