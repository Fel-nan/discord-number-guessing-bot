using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
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

    // Stores one game for each Discord server
    private static Dictionary<ulong, Game> Games = new Dictionary<ulong, Game>();

    // Discord server ID loaded from .env
    private static ulong _guildId;

    static async Task Main()
    {
        // Load variables from .env
        Env.Load();

        Console.WriteLine("---ENV DUMP---");
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            Console.WriteLine($"{e.Key} = {e.Value}");
        Console.WriteLine("---END DUMP---");

        string? botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        string? serverId = Environment.GetEnvironmentVariable("DISCORD_SERVER_ID");

        // Check bot token
        if (string.IsNullOrEmpty(botToken))
        {
            Console.WriteLine("DISCORD_BOT_TOKEN is missing.");
            return;
        }

        // Check server ID
        if (string.IsNullOrEmpty(serverId))
        {
            Console.WriteLine("DISCORD_SERVER_ID is missing.");
            return;
        }

        // Convert server ID from string to ulong
        if (!ulong.TryParse(serverId, out _guildId))
        {
            Console.WriteLine("DISCORD_SERVER_ID is not a valid number.");
            return;
        }

        // Discord events
        _client.Log += Log;

        // Runs when the bot successfully connects
        _client.Ready += RegisterCommands;

        // Runs whenever someone uses a slash command
        _client.SlashCommandExecuted += SlashCommandHandler;

        // Runs whenever someone sends a message
        _client.MessageReceived += MessageReceived;

        // Login and start the bot
        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();

        // Keep the program running
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
            Console.WriteLine("Could not find the Discord server.");
            return;
        }

        // /ping command
        var pingCommand = new SlashCommandBuilder()
            .WithName("ping")
            .WithDescription("Check if the bot is online.");

        await guild.CreateApplicationCommandAsync(
            pingCommand.Build()
        );

        // /start command
        var startCommand = new SlashCommandBuilder()
            .WithName("start")
            .WithDescription("Start a new number guessing game.");

        await guild.CreateApplicationCommandAsync(
            startCommand.Build()
        );

        Console.WriteLine("Slash commands registered successfully!");
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
            if (Games.ContainsKey(guildId))
            {
                await command.RespondAsync(
                    "⚠️ There is already a game running in this server!"
                );

                return;
            }

            // Create a new game
            Game game = new Game();

            // Store the game using the server ID
            Games[guildId] = game;

            await command.RespondAsync(
                "🎮 **New game started!**\n\n" +
                "I'm thinking of a number between **1 and 100**.\n" +
                "Everyone in this server can guess!\n\n" +
                "You have **10 attempts**.\n" +
                "Just type a number between 1 and 100."
            );
        }
    }

    private static async Task MessageReceived(SocketMessage message)
    {
        // Ignore messages sent by bots
        if (message.Author.IsBot)
            return;

        // Make sure the message came from a Discord server
        if (message.Channel is not SocketGuildChannel guildChannel)
            return;

        // Get the server ID
        ulong guildId = guildChannel.Guild.Id;

        // Check if this server has an active game
        if (!Games.ContainsKey(guildId))
            return;

        // Get the active game
        Game game = Games[guildId];

        // Ignore messages that aren't numbers
        if (!int.TryParse(message.Content, out int guess))
            return;

        // Check if the number is between 1 and 100
        if (guess < 1 || guess > 100)
        {
            await message.Channel.SendMessageAsync(
                "❌ Your guess must be between 1 and 100."
            );

            return;
        }

        // Increase attempt count
        game.Attempts++;

        // Guess is too high
        if (guess > game.SecretNumber)
        {
            int remaining =
                game.MaxAttempts - game.Attempts;

            if (remaining <= 0)
            {
                await message.Channel.SendMessageAsync(
                    $"💀 **Game over!**\n" +
                    $"The number was **{game.SecretNumber}**.\n" +
                    $"Nobody guessed it in {game.MaxAttempts} attempts."
                );

                Games.Remove(guildId);
            }
            else
            {
                await message.Channel.SendMessageAsync(
                    $"📈 {message.Author.Mention} guessed **{guess}** — **Too high!**\n" +
                    $"Attempts remaining: **{remaining}**"
                );
            }
        }

        // Guess is too low
        else if (guess < game.SecretNumber)
        {
            int remaining =
                game.MaxAttempts - game.Attempts;

            if (remaining <= 0)
            {
                await message.Channel.SendMessageAsync(
                    $"💀 **Game over!**\n" +
                    $"The number was **{game.SecretNumber}**.\n" +
                    $"Nobody guessed it in {game.MaxAttempts} attempts."
                );

                Games.Remove(guildId);
            }
            else
            {
                await message.Channel.SendMessageAsync(
                    $"📉 {message.Author.Mention} guessed **{guess}** — **Too low!**\n" +
                    $"Attempts remaining: **{remaining}**"
                );
            }
        }

        // Correct guess
        else
        {
            game.Won = true;

            await message.Channel.SendMessageAsync(
                $"🎉 **Correct!** {message.Author.Mention} guessed the number!\n" +
                $"The number was **{game.SecretNumber}**.\n" +
                $"They got it in **{game.Attempts} attempts**! 🏆"
            );

            // Remove the completed game
            Games.Remove(guildId);
        }
    }
}