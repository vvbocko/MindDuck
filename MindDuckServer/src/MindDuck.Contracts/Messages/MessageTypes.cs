using System;
using System.Collections.Generic;

namespace MindDuck.Contracts
{
    /// <summary>
    /// "type" string -> C# class. The receiver reads "type" from the envelope, looks the class up here,
    /// then deserializes "payload" into it.
    ///
    /// NEW MESSAGE? Add the class AND add it to the right list below.
    /// (Explicit list on purpose: Unity's WebGL code stripping can't remove classes referenced with typeof.)
    /// </summary>
    public static class MessageTypes
    {
        public static readonly IReadOnlyDictionary<string, Type> ClientToServer = Build(
            typeof(JoinRoom),
            typeof(Rejoin),
            typeof(PlayCard),
            typeof(Attack),
            typeof(SubmitDecision),
            typeof(LeaveRoom)
        );

        public static readonly IReadOnlyDictionary<string, Type> ServerToClient = Build(
            typeof(RoomJoined),
            typeof(MatchStarted),
            typeof(StateUpdate),
            typeof(DecisionRequired),
            typeof(ActionRejected),
            typeof(OpponentDisconnected),
            typeof(OpponentReconnected),
            typeof(MatchEnded),
            typeof(Error)
        );

        private static IReadOnlyDictionary<string, Type> Build(params Type[] types)
        {
            var map = new Dictionary<string, Type>();
            foreach (var type in types)
            {
                map.Add(type.Name, type);
            }
            return map;
        }
    }
}
