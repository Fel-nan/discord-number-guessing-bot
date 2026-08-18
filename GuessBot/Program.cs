using System;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using Discord.WebSocket;
using DotNetEnv;

class Program
{
    private static DiscordSocketClient _client = new DiscordSocketClient(
        new DiscordSocketConfig
        {
            GatewayIntents =
                GatewayIntents.Guilds |
                GatewayIntents.GuildMessages |
                GatewayIntents.MessageContent
        }
    );

    private static GameManager _gameManager = new GameManager();

    private static GameFactory _gameFactory;

    private static ulong _guildId;

    static async Task Main()
    {
        Env.Load();

        string? botToken =
            Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

        string? serverId =
            Environment.GetEnvironmentVariable("DISCORD_SERVER_ID");

        if (string.IsNullOrEmpty(botToken))
        {
            Console.WriteLine("DISCORD_BOT_TOKEN is missing.");
            return;
        }

        if (string.IsNullOrEmpty(serverId))
        {
            Console.WriteLine("DISCORD_SERVER_ID is missing.");
            return;
        }

        if (!ulong.TryParse(serverId, out _guildId))
        {
            Console.WriteLine(
                "DISCORD_SERVER_ID is not a valid number."
            );

            return;
        }

        _gameFactory = new GameFactory(_gameManager);

        _client.Log += Log;

        _client.Ready += RegisterCommands;

        _client.SlashCommandExecuted += SlashCommandHandler;

        _client.MessageReceived += MessageReceived;

        await _client.LoginAsync(
            TokenType.Bot,
            botToken
        );

        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private static Task Log(LogMessage message)
    {
        Console.WriteLine(message.ToString());

        return Task.CompletedTask;
    }

    private static async Task RegisterCommands()
    {
        var guild = _client.GetGuild(_guildId);

        if (guild == null)
        {
            Console.WriteLine(
                "Could not find the Discord server."
            );

            return;
        }

        // /ping command
        var pingCommand = new SlashCommandBuilder()
            .WithName("ping")
            .WithDescription(
                "Check if the bot is online."
            );

        await guild.CreateApplicationCommandAsync(
            pingCommand.Build()
        );

        // /start command
        var startCommand = new SlashCommandBuilder()
            .WithName("start")
            .WithDescription("Start a game.")
            .AddOption(
                "game",
                ApplicationCommandOptionType.String,
                "Choose a game to play.",
                isRequired: true,
                choices: new[]
                {
                    new ApplicationCommandOptionChoiceProperties
                    {
                        Name = "Number Guessing",
                        Value = "number"
                    }
                }
            );

        await guild.CreateApplicationCommandAsync(
            startCommand.Build()
        );

        Console.WriteLine(
            "Slash commands registered successfully!"
        );
    }

    private static async Task SlashCommandHandler(
        SocketSlashCommand command)
    {
        // /ping
        if (command.Data.Name == "ping")
        {
            await command.RespondAsync("Pong! 🏓");

            return;
        }

        // /start
        if (command.Data.Name == "start")
        {
            if (command.GuildId == null)
            {
                await command.RespondAsync(
                    "❌ This command can only be used inside a server."
                );

                return;
            }

            ulong guildId = command.GuildId.Value;

            // Check if a game is already running
            if (_gameManager.HasGame(guildId))
            {
                await command.RespondAsync(
                    "⚠️ There is already a game running in this server!"
                );

                return;
            }

            // Get the selected game
            string selectedGame =
                command.Data.Options.First().Value.ToString()!;

            IGame? game = _gameFactory.CreateGame(
                selectedGame,
                guildId
            );

            // Make sure the selected game exists
            if (game == null)
            {
                await command.RespondAsync(
                    "❌ That game is not available yet."
                );

                return;
            }

            // Add the game to the GameManager
            _gameManager.AddGame(
                guildId,
                game
            );

            // Start the game
            await game.StartAsync(command);
        }
    }

    private static async Task MessageReceived(
        SocketMessage message)
    {
        // Ignore messages from bots
        if (message.Author.IsBot)
            return;

        // Make sure the message came from a server
        if (message.Channel is not SocketGuildChannel guildChannel)
            return;

        ulong guildId = guildChannel.Guild.Id;

        // Get the active game
        IGame? game = _gameManager.GetGame(guildId);

        if (game == null)
            return;

        // Give the message to the active game
        await game.HandleMessageAsync(message);
    }
}