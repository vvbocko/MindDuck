namespace MindDuck.Rules.Results
{
    public enum ApplyErrorCode
    {
        None,
        InvalidCommand,
        InvalidPlayer,
        IllegalAction,
        NotPlayersTurn,
        InvalidGamePhase,
        MissingDecision
    }
}