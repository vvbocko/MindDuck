using MindDuck.Rules.Models;

namespace MindDuck.Rules.Results;
public class ApplyResult
{
    public bool Success { get; }
    public ApplyErrorCode ErrorCode { get; }

    public string? ErrorMessage { get; }

    public GameState UpdatedState { get; private set; }

    private ApplyResult(bool success, ApplyErrorCode errorCode, string? errorMessage, GameState updatedState)
    {
        Success = success;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        UpdatedState = updatedState;
    }

    public static ApplyResult Success(GameState updatedState)
    {
        return new ApplyResult(true, ApplyErrorCode.None, null, updatedState);
    }

    public static ApplyResult Failure(ApplyErrorCode errorCode, string errorMessage)
    {
        return new ApplyResult(false, errorCode, errorMessage, null);
    }
}