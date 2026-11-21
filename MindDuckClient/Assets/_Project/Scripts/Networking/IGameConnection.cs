// IGameConnection - the "plug" between the game screen and the server.
//
// WHY IT EXISTS
//   The game screen (MatchController) must not care HOW it talks to the server. It only calls the
//   methods here ("play card 17") and listens to the events ("a new game state arrived").
//   Two classes implement it:
//     - FakeGameConnection      : pretends to be a server inside Unity (works today, no server needed)
//     - WebSocketGameConnection : the real one over WebSocket (networking owner, later)
//   Switching fake -> real is one line where the connection is created. The game screen doesn't change.
//
// WHERE IT COMES FROM
//   Exactly the interface agreed in team-and-interfaces.md, section 2. Change it only together with
//   the networking owner.
//
// HOW TO READ IT
//   - methods = what the CLIENT tells the server; each becomes one WebSocket message (protocol.md 4)
//   - events  = what the SERVER tells the client; raised when a message arrives (protocol.md 5).
//     "event Action<T>" = a list of methods that get called with a T when the event is raised.

using System;
using System.Threading.Tasks;
using MindDuck.Contracts;

namespace MindDuck.Client.Networking
{
    public interface IGameConnection
    {
        // ---- Client -> Server ----------------------------------------------------------

        /// <summary>Open the connection. "Task" = it finishes later; use "await".</summary>
        Task ConnectAsync(string url);

        void JoinRoom(string roomCode, string nickname);
        void Rejoin(string roomCode, string playerToken);
        void PlayCard(int cardInstanceId);
        void Attack(int creatureInstanceId);
        void SubmitDecision(int decisionId, string optionId);
        void LeaveRoom();

        // ---- Server -> Client ----------------------------------------------------------

        /// <summary>You are in the room. Keep PlayerToken for Rejoin.</summary>
        event Action<RoomJoined> RoomJoined;

        /// <summary>The complete game as YOU may see it. The game screen redraws from it.</summary>
        event Action<StateUpdate> StateUpdated;

        /// <summary>You must choose something (use a Mindbug? block? which target? attack again?).</summary>
        event Action<DecisionRequired> DecisionRequired;

        /// <summary>Your last command broke a rule. Nothing changed.</summary>
        event Action<ActionRejected> ActionRejected;

        /// <summary>Opponent dropped (false, seconds left) or came back (true, 0).</summary>
        event Action<bool, int> OpponentConnectionChanged;

        event Action<MatchEnded> MatchEnded;

        /// <summary>OUR connection to the server broke.</summary>
        event Action ConnectionLost;
    }
}
