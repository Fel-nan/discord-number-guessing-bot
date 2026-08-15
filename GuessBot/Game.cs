using System;

public class Game
{
    public int SecretNumber { get; set; }

    public int Attempts { get; set; }

    public int MaxAttempts { get; set; }

    public bool Won { get; set; }

    public Game()
    {
        Random random = new Random();

        SecretNumber = random.Next(1, 101);
        Attempts = 0;
        MaxAttempts = 10;
        Won = false;
    }
}