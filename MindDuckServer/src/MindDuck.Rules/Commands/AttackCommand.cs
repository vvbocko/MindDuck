namespace MindDuck.Rules.Commands;

public sealed class AttackCommand(int attackerInstanceId, int? targetCreatureInstanceId = null) : GameCommand
{
    public int AttackerInstanceId { get; } = attackerInstanceId;

    public int? TargetCreatureInstanceId { get; } = targetCreatureInstanceId;
}