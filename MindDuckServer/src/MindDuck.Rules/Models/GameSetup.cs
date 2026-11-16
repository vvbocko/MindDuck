namespace MindDuck.Rules.Models;

public sealed class GameSetup
{
    public Player PlayerOne { get; }

    public Player PlayerTwo { get; }

    public int StartingLife { get; set; } = 3;

    public int StartingHandSize { get; set; } = 5;

    public GameSetup(Player playerOne, Player playerTwo)
    {
        PlayerOne = playerOne;
        PlayerTwo = playerTwo;
    }
}