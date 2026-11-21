namespace MindDuck.Rules.Models;

public sealed class Card
{
    public int InstanceId { get; }

    public CardDefinition Definition { get; }

    // number of attacks performed in this turn
    public int AttacksThisTurn { get; private set; }

    // whether Tough has already protected this creature
    public bool ToughUsed { get; private set; }

    // the number of the turn in which the card was played
    public int EnteredPlayTurn { get; private set; } = -1;

    private int MaxAttacksPerTurn => HasKeyword(Keyword.Frenzy) ? 2 : 1;

    public int Power => Definition.Power;

    public Card(int instanceId, CardDefinition definition)
    {
        InstanceId = instanceId;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public bool HasKeyword(Keyword keyword)
    {
        return Definition.HasKeyword(keyword);
    }

    public bool CanAttack(int currentTurn)
    {
        return EnteredPlayTurn < currentTurn && AttacksThisTurn < MaxAttacksPerTurn;
    }

    public void PrepareForPlay(int currentTurn)
    {
        AttacksThisTurn = 0;
        ToughUsed = false;
        EnteredPlayTurn = currentTurn;
    }

    public void RegisterAttack(int currentTurn)
    {
        if (!CanAttack(currentTurn))
            throw new InvalidOperationException("This creature cannot attack.");

        AttacksThisTurn++;
    }

    public bool TryPreventDefeat()
    {
        if (!HasKeyword(Keyword.Tough) || ToughUsed) return false;

        ToughUsed = true;
        return true;
    }

    public void StartTurn()
    {
        AttacksThisTurn = 0;
    }
}