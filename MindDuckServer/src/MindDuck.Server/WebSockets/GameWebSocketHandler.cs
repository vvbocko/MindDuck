using MindDuck.Server.Models;
using MindDuck.Contracts;
using MindDuck.Server.Services;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MindDuck.Server.WebSockets
{
    public class GameWebSocketHandler{
        private readonly RoomManager _roomManager;

        public GameWebSocketHandler(RoomManager roomManager)
        {
            _roomManager = roomManager;
        }

        public async Task HandleAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            // odbieram wiadomość od klienta
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (receiveResult.MessageType != WebSocketMessageType.Text)
            {
                return; 
            }

            // zamiana bytów na string w formacie json
            string jsonString = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

            // ignorowanie wielkości liter
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true};

            // rozpakowuję wiadomość 
            var envelope = JsonSerializer.Deserialize<Envelope>(jsonString, jsonOptions);

            if (envelope == null || envelope.Type != nameof(JoinRoom))
            {
                return;
            }

            // w Envelope Payload to typ "object", czyli ogólny JsonElement, więc tzreba go przekształcić w obiekt JoinRoom
            var payloadElement = (JsonElement)envelope.Payload;
            var joinRoomData = payloadElement.Deserialize<JoinRoom>(jsonOptions);

            if (joinRoomData == null)
            {
                return;
            }


            string targetRoomCode = joinRoomData.RoomCode;
            string playerNickname = joinRoomData.Nickname;

            var targetRoom = _roomManager.GetRoom(targetRoomCode);

            if (targetRoom == null)
            {
                var errorResponse = new Error
                {
                    Code = ErrorCode.RoomNotFound, Message = "Brak pokoju o takim kodzie."
                };

                await SendMessageAsync(webSocket, errorResponse);
                return;
            }

            try
            {
                var assignedSeat = targetRoom.AddPlayer(playerNickname, out string generatedToken);

                string? opponentName = null;
                if (assignedSeat == PlayerSeat.P2)
                {
                    opponentName = targetRoom.Player1Nickname;
                }

                var roomJoinedMessage = new RoomJoined
                {
                    RoomCode = targetRoom.RoomCode,
                    Seat = assignedSeat,
                    PlayerToken = generatedToken,
                    OpponentNickname = opponentName
                };
                await SendMessageAsync(webSocket, roomJoinedMessage);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Zakończono dołączanie", CancellationToken.None);
            }
            catch (Exception ex)
            {
                var fullRoomError = new Error
                {
                    Code = ErrorCode.RoomFull,
                    Message = ex.Message
                };
                await SendMessageAsync(webSocket, fullRoomError);
            }
            await Task.Delay(500);
        }

        // metoda pomocnicza żeby zapakować i wysłać wiadomość
        private async Task SendMessageAsync(WebSocket websocket, ServerMessage messageToSend)
        {
            var envelope = Envelope.For(messageToSend);
            string jsonResponse = JsonSerializer.Serialize(envelope);

            byte[] bytesToSend = Encoding.UTF8.GetBytes(jsonResponse);

            await websocket.SendAsync(
                new ArraySegment<byte>(bytesToSend),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }
}