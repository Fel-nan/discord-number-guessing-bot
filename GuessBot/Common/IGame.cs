using Discord.WebSocket;

public interface IGame
{
    string Name { get; }

    Task StartAsync(SocketSlashCommand command);

    Task HandleMessageAsync(SocketMessage message);

    Task EndAsync();
}
