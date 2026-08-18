public class GameFactory
{
    private readonly GameManager _gameManager;

    public GameFactory(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public IGame? CreateGame(
        string gameType,
        ulong guildId)
    {
        if (gameType == "number")
        {
            return new NumberGuessingGame(
                guildId,
                _gameManager
            );
        }

        return null;
    }
}