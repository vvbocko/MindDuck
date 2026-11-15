using System.Collections.Concurrent;
using System.Security.Cryptography;
using MindDuck.Contracts;
using MindDuck.Server.Models;

namespace MindDuck.Server.Services
{
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new(StringComparer.OrdinalIgnoreCase);

        public string CreateRoom()
        {
            string roomCode = GenerateRoomCode();

            while(!_rooms.TryAdd(roomCode, new Room(roomCode)))
            {
                roomCode = GenerateRoomCode();
            }
            return roomCode;
        }


        public Room? GetRoom(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                return null;
            }

            _rooms.TryGetValue(roomCode, out var room);
            return room;
        }

        private string GenerateRoomCode()
        {
            Span<char> codeChars = stackalloc char[Protocol.RoomCodeLength];
            string alphabet = Protocol.RoomCodeAlphabet;

            for (int i = 0; i < Protocol.RoomCodeLength; i++)
            {
                int index = RandomNumberGenerator.GetInt32(alphabet.Length);
                codeChars[i] = alphabet[index];
            }

            return new string(codeChars);
        }
    }
}