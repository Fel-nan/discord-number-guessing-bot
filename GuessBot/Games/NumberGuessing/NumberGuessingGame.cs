using Discord;
using Discord.WebSocket;

public class NumberGuessingGame : IGame
{
    public string Name => "Number Guessing";

    private readonly Random random = new Random();

    private readonly ulong guildId;
    private readonly GameManager gameManager;

    private int secretNumber;
    private int attempts;

    private const int MaxAttempts = 10;

    private bool won;

    public NumberGuessingGame(
        ulong guildId,
        GameManager gameManager)
    {
        this.guildId = guildId;
        this.gameManager = gameManager;
    }

    public Task StartAsync(SocketSlashCommand command)
    {
        secretNumber = random.Next(1, 101);
        attempts = 0;
        won = false;

        return command.Channel.SendMessageAsync(
            "🎮 **New game started!**\n\n" +
            "I'm thinking of a number between **1 and 100**.\n" +
            "Everyone can play!\n\n" +
            "You have **10 attempts**. Just type a number."
        );
    }

    public async Task HandleMessageAsync(SocketMessage message)
    {
        if (won)
            return;

        if (!int.TryParse(message.Content, out int guess))
        {
            return;
        }

        if (guess < 1 || guess > 100)
        {
            await message.Channel.SendMessageAsync(
                "❌ Your guess must be between 1 and 100."
            );

            return;
        }

        attempts++;

        if (guess > secretNumber)
        {
            await HandleWrongGuess(
                message,
                $"📈 {message.Author.Mention} guessed **{guess}** — **Too high!**"
            );
        }
        else if (guess < secretNumber)
        {
            await HandleWrongGuess(
                message,
                $"📉 {message.Author.Mention} guessed **{guess}** — **Too low!**"
            );
        }
        else
        {
            won = true;

            await message.Channel.SendMessageAsync(
                $"🎉 **Correct!** {message.Author.Mention} guessed the number!\n" +
                $"The number was **{secretNumber}**.\n" +
                $"They got it in **{attempts} attempts**!"
            );

            await EndAsync();
        }
    }

    private async Task HandleWrongGuess(
        SocketMessage message,
        string response)
    {
        int remaining = MaxAttempts - attempts;

        if (remaining <= 0)
        {
            await message.Channel.SendMessageAsync(
                $"💀 **Game over!**\n" +
                $"The number was **{secretNumber}**.\n" +
                $"Nobody guessed it in {MaxAttempts} attempts."
            );

            await EndAsync();
        }
        else
        {
            await message.Channel.SendMessageAsync(
                $"{response}\n" +
                $"Attempts remaining: **{remaining}**"
            );
        }
    }

    public Task EndAsync()
    {
        won = true;

        gameManager.RemoveGame(guildId);

        return Task.CompletedTask;
    }
}