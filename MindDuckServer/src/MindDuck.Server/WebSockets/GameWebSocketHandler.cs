using MindDuck.Server.Models;
using MindDuck.Contracts;
using MindDuck.Server.Services;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules;
using MindDuck.Rules.Models;
using MindDuck.Rules.Commands;

namespace MindDuck.Server.WebSockets
{
    public class GameWebSocketHandler{
        private readonly RoomManager _roomManager;
        private readonly IGameService _gameService;
        private readonly MatchManager _matchManager;
        private readonly GameStateViewBuilder _viewBuilder;
        private readonly SocketMessageSender _messageSender;

        public GameWebSocketHandler(RoomManager roomManager, IGameService gameService, MatchManager matchManager, GameStateViewBuilder viewBuilder, SocketMessageSender messageSender) 
        {
            _roomManager = roomManager;
            _gameService = gameService;
            _matchManager = matchManager;
            _viewBuilder = viewBuilder;
            _messageSender = messageSender;
        }

        public async Task HandleAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            Room? currentRoom = null;
            PlayerSeat? currentSeat = null;

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult receiveResult;
                try
                {
                    // odbieram wiadomość od klienta
                    receiveResult = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    break;
                }

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    if (webSocket.State == WebSocketState.CloseReceived || webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure, "Połączenie zakończone.", CancellationToken.None);
                    }
                    break;
                }

                if (receiveResult.MessageType != WebSocketMessageType.Text) continue;

                // zamiana bytów na string w formacie json
                string jsonString = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                Envelope? envelope;
                try
                {
                    // rozpakowuję wiadomość 
                    envelope = JsonSerializer.Deserialize<Envelope>(jsonString, SocketMessageSender.JsonOptions);
                }
                catch (JsonException)
                {
                    await _messageSender.SendAsync(webSocket, new Error
                    {
                        Code = ErrorCode.BadJson,
                        Message = "Niepoprawny format JSON."
                    });
                    continue;
                }

                if (envelope == null || string.IsNullOrWhiteSpace(envelope.Type))
                {
                    await _messageSender.SendAsync(webSocket, new Error
                    {
                        Code = ErrorCode.UnknownMessage,
                        Message = "Brak typu wiadomości."
                    });
                    continue;
                }

                if (envelope.Type == nameof(JoinRoom))
                {
                    var joined = await HandleJoinRoomAsync(webSocket, envelope.Payload);
                    if (joined.HasValue)
                    {
                        currentRoom = joined.Value.Room;
                        currentSeat = joined.Value.Seat;
                    }
                    continue;
                }

                if (currentRoom == null || currentSeat == null || currentRoom.MatchId == null)
                {
                    await _messageSender.SendAsync(webSocket, new Error
                    {
                        Code = ErrorCode.NotInRoom,
                        Message = "Najpierw dołącz do pokoju przez JoinRoom."
                    });
                    continue;
                }

                await HandleGameMessageAsync(webSocket, currentRoom, currentSeat.Value, envelope.Type, envelope.Payload);
            }
        }

        private async Task<(Room Room, PlayerSeat Seat)?> HandleJoinRoomAsync(WebSocket webSocket, object? rawPayload)
        {
            if (rawPayload is not JsonElement payloadElement)
            {
                await _messageSender.SendAsync(webSocket, new Error
                {
                    Code = ErrorCode.BadJson,
                    Message = "Niepoprawny payload dla JoinRoom."
                });
                return null;
            }

            var joinData = payloadElement.Deserialize<JoinRoom>(SocketMessageSender.JsonOptions);
            if (joinData == null || string.IsNullOrWhiteSpace(joinData.RoomCode))
            {
                await _messageSender.SendAsync(webSocket, new Error
                {
                    Code = ErrorCode.BadJson,
                    Message = "Brak kodu pokoju."
                });
                return null;
            }

            if (string.IsNullOrWhiteSpace(joinData.Nickname) || joinData.Nickname.Length > Protocol.MaxNicknameLength)
            {
                await _messageSender.SendAsync(webSocket, new Error
                {
                    Code = ErrorCode.InvalidNickname,
                    Message = "Niepoprawny nick gracza."
                });
                return null;
            }

            var targetRoom = _roomManager.GetRoom(joinData.RoomCode);
            if (targetRoom == null)
            {
                await _messageSender.SendAsync(webSocket, new Error
                {
                    Code = ErrorCode.RoomNotFound,
                    Message = "Brak pokoju o takim kodzie."
                });
                return null;
            }

            PlayerSeat assignedSeat;
            string generatedToken;

            try
            {
                assignedSeat = targetRoom.AddPlayer(joinData.Nickname, out generatedToken);
            }
            catch (InvalidOperationException ex)
            {
                await _messageSender.SendAsync(webSocket, new Error
                {
                    Code = ErrorCode.RoomFull,
                    Message = ex.Message
                });
                return null;
            }

            if (assignedSeat == PlayerSeat.P1)
            {
                targetRoom.Player1Socket = webSocket;
                targetRoom.Player1 = new Player(1, joinData.Nickname);
            }
            else
            {
                targetRoom.Player2Socket = webSocket;
                targetRoom.Player2 = new Player(2, joinData.Nickname);
            }

            await _messageSender.SendAsync(webSocket, new RoomJoined
            {
                RoomCode = targetRoom.RoomCode,
                Seat = assignedSeat,
                PlayerToken = generatedToken,
                OpponentNickname = assignedSeat == PlayerSeat.P2 ? targetRoom.Player1Nickname : null
            });

            if (targetRoom.IsRoomFull && targetRoom.Player1 != null && targetRoom.Player2 != null && targetRoom.MatchId == null)
            {
                var match = _gameService.StartMatch(targetRoom.Player1, targetRoom.Player2);
                targetRoom.MatchId = match.MatchId;

                var matchStarted = new MatchStarted
                {
                    MatchId = match.MatchId.ToString(),
                    StartingSeat = PlayerSeat.P1
                };

                if (targetRoom.Player1Socket != null && targetRoom.Player1Socket.State == WebSocketState.Open)
                {
                    await _messageSender.SendAsync(targetRoom.Player1Socket, matchStarted);
                }

                if (targetRoom.Player2Socket != null && targetRoom.Player2Socket.State == WebSocketState.Open)
                {
                    await _messageSender.SendAsync(targetRoom.Player2Socket, matchStarted);
                }
                await SendGameStateAsync(targetRoom, match);
            }
            return (targetRoom, assignedSeat);

        }

        private async Task HandleGameMessageAsync(WebSocket webSocket, Room room, PlayerSeat seat, string messageType, object? rawPayload)
        {
            Player? actingPlayer = room.GetPlayer(seat);
            if (actingPlayer == null || room.MatchId == null || rawPayload is not JsonElement payloadElement) return;

            GameCommand? command = null;
            switch (messageType)
            {
                case nameof(PlayCard):
                    PlayCard? play = payloadElement.Deserialize<PlayCard>(SocketMessageSender.JsonOptions);
                    if (play != null)
                    {
                        command = new PlayCardCommand(play.CardInstanceId);
                    }
                    break;

                case nameof(Attack):
                    Attack? attack = payloadElement.Deserialize<Attack>(SocketMessageSender.JsonOptions);
                    if (attack != null)
                    {
                        command = new AttackCommand(attack.CreatureInstanceId);
                    }
                    break;

                default:
                    command = null;
                    break;
            }

            if (command == null && messageType != nameof(PlayCard) && messageType != nameof(Attack))
            {
                await _messageSender.SendAsync(webSocket, new Error
                {
                    Code = ErrorCode.UnknownMessage,
                    Message = $"Nieznany typ wiadomości: {messageType}"
                });
                return;
            }

            if (command == null)
            {
                await _messageSender.SendAsync(webSocket, new Error
                {
                    Code = ErrorCode.BadJson,
                    Message = $"Niepoprawny payload dla {messageType}."
                });
                return;
            }

            var result = _gameService.ExecuteCommand(room.MatchId.Value, actingPlayer, command);
            if (!result.IsSuccess)
            {
                await _messageSender.SendAsync(webSocket, new ActionRejected
                {
                    Code = ErrorCode.IllegalMove,
                    Message = result.ErrorMessage ?? "Niepoprawny ruch."
                });
                return;
            }

            var match = _matchManager.GetMatch(room.MatchId.Value);
            await SendGameStateAsync(room, match);
        }

        private async Task SendGameStateAsync(Room room, Match match)
        {
            int sequence = room.NextSequence();
            GameState state = match.GameState;

            if (room.Player1 != null && room.Player1Socket != null && room.Player1Socket.State == WebSocketState.Open)
            {
                await _messageSender.SendAsync(room.Player1Socket, new StateUpdate
                {
                    Sequence = sequence,
                    State = _viewBuilder.Build(match.MatchId, state, room.Player1, PlayerSeat.P1)
                });
            }

            if (room.Player2 != null && room.Player2Socket != null && room.Player2Socket.State == WebSocketState.Open)
            {
                await _messageSender.SendAsync(room.Player2Socket, new StateUpdate
                {
                    Sequence = sequence,
                    State = _viewBuilder.Build(match.MatchId, state, room.Player2, PlayerSeat.P2)
                });
            }
        }
    }
}

            
