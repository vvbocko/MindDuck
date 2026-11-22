using MindDuck.Contracts;
using System.Net.WebSockets;
using System.Threading;
using MindDuck.Rules.Models;

namespace MindDuck.Server.Models
{
    public class Room
    {
        private int _sequence;
        public string RoomCode { get; }
        public RoomStatus Status {get; private set; } = RoomStatus.WaitingForPlayers;
        public SemaphoreSlim JoinLock { get; } = new(1, 1);

        public string? Player1Nickname { get; private set; }
        public string? Player2Nickname { get; private set; }

        public string? Player1Token { get; private set; }
        public string? Player2Token { get; private set; }

        public WebSocket? Player1Socket { get; set; }
        public WebSocket? Player2Socket { get; set; }

        public Player? Player1 { get; set; }
        public Player? Player2 { get; set; }

        public Guid? MatchId { get; set; }

        public Room(string roomCode)
        {
            RoomCode = roomCode ?? throw new ArgumentNullException(nameof(roomCode));
        }

        public bool IsRoomFull => Player1Nickname != null && Player2Nickname != null;

        public int NextSequence() => Interlocked.Increment(ref _sequence);

        public PlayerSeat AddPlayer(string nickname, int playerId, out string playerToken)
        {
            if (IsRoomFull)
            {
                throw new InvalidOperationException("Room is full."); 
            }

            playerToken = Guid.NewGuid().ToString();

            if (Player1Nickname == null)
            {
                Player1Nickname = nickname;
                Player1Token = playerToken;
                Player1 = new Player(playerId, nickname);
                return PlayerSeat.P1;
            }

            Player2Nickname = nickname;
            Player2Token = playerToken;
            Player2 = new Player(playerId, nickname);
            Status = RoomStatus.InProgress;
            return PlayerSeat.P2;
        }

        public Player? GetPlayer(PlayerSeat seat)
        {
            if (seat == PlayerSeat.P1)
            {
                return Player1;
            }
            return Player2;
        }

        public WebSocket? GetSocket(PlayerSeat seat)
        {
            if (seat == PlayerSeat.P1)
            {
                return Player1Socket;
            }
            return Player2Socket;
        }
    }
}