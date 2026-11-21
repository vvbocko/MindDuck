using MindDuck.Rules.Commands;

namespace MindDuck.Rules.Commands;
public sealed class PlayCardCommand(int cardInstanceId) : GameCommand
{
    public int CardInstanceId { get; } = cardInstanceId;
}