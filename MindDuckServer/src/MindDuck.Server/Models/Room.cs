using MindDuck.Contracts;

namespace MindDuck.Server.Models
{
    public class Room
    {
        public string RoomCode { get; }
        public RoomStatus Status {get; private set; } = RoomStatus.WaitingForPlayers;

        public string? Player1Nickname { get; private set; }
        public string? Player2Nickname { get; private set; }

        public string? Player1Token { get; private set; }
        public string? Player2Token { get; private set; }

        public Room(string roomCode)
        {
            RoomCode = roomCode ?? throw new ArgumentNullException(nameof(roomCode));
        }

        public bool IsRoomFull => Player1Nickname != null && Player2Nickname != null;

        public PlayerSeat AddPlayer(string nickname, out string playerToken)
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
                return PlayerSeat.P1;
            }

            Player2Nickname = nickname;
            Player2Token = playerToken;
            Status = RoomStatus.InProgress;
            return PlayerSeat.P2;
        }
    }
}