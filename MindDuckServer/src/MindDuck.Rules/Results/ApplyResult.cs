namespace MindDuck.Rules.Results
{
    public class ApplyResult
    {
        public bool IsSuccessfull { get; private set; }
        public ApplyErrorCode ErrorCode { get; private set; }

        public string ErrorMessage { get; private set; }

        public GameState UpdatedState { get; private set; }

        private ApplyResult(bool isSuccessfull, ApplyErrorCode errorCode, string errorMessage, GameState updatedState)
        {
            IsSuccessfull = isSuccessfull;
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
            return new ApplyResult(false, errorCode, errorMessage);
        }
    }
}