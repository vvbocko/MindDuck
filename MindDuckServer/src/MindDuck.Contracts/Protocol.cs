namespace MindDuck.Contracts
{
    /// <summary>Fixed values both sides must agree on.</summary>
    public static class Protocol
    {
        /// <summary>Bump when a message is renamed, removed or changes meaning (breaks old clients).</summary>
        public const int Version = 1;

        public const string WebSocketPath = "/ws";
        public const string ApiPrefix = "/api";

        public const int ReconnectSeconds = 30;

        public const int RoomCodeLength = 4;
        public const string RoomCodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>Longer (or empty) nickname -> Error with ErrorCode.InvalidNickname.</summary>
        public const int MaxNicknameLength = 16;
    }
}
