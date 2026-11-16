namespace MindDuck.Rules.Commands;
public class AttackCommand : GameCommand
{
        public int AttackerInstanceId { get; }
        
        public int? TargetCreatureInstanceId { get; }

        public AttackCommand(int attackerInstanceId, int? targetCreatureInstaceId = null)
        {
                AttackerInstanceId = attackerInstanceId;
                TargetCreatureInstanceId = targetCreatureInstaceId;
        }
}