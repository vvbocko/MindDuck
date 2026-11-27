using MindDuck.Server.Models;
using MindDuck.Contracts;
using MindDuck.Server.Services;
using MindDuck.Persistence;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules;
using MindDuck.Rules.Models;
using MindDuck.Rules.Commands;
using Player = MindDuck.Rules.Models.Player;
using System.Collections;

namespace MindDuck.Server.WebSockets
{
    public class GameWebSocketHandler{
        private readonly RoomManager _roomManager;
        private readonly IGameService _gameService;
        private readonly MatchManager _matchManager;
        private readonly GameStateViewBuilder _viewBuilder;
        private readonly SocketMessageSender _messageSender;
        private readonly IPlayerRepository _playerRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly ICardService _cardService;
        private readonly IGameEngine _gameEngine;

        public GameWebSocketHandler(
            RoomManager roomManager,
            IGameService gameService,
            MatchManager matchManager,
            GameStateViewBuilder viewBuilder,
            SocketMessageSender messageSender,
            IPlayerRepository playerRepository,
            IMatchRepository matchRepository,
            ICardService cardService,
            IGameEngine gameEngine)
        {
            _roomManager = roomManager;
            _gameService = gameService;
            _matchManager = matchManager;
            _viewBuilder = viewBuilder;
            _messageSender = messageSender;
            _playerRepository = playerRepository;
            _matchRepository = matchRepository;
            _cardService = cardService;
            _gameEngine = gameEngine;
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
                    receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
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

            await targetRoom.JoinLock.WaitAsync();
            try
            {
                if (targetRoom.IsRoomFull)
                {
                    await _messageSender.SendAsync(webSocket, new Error
                    {
                        Code = ErrorCode.RoomFull,
                        Message = "Room is full."
                    });
                    return null;
                }

                var player = await _playerRepository.CreatePlayerAsync(joinData.Nickname);
                assignedSeat = targetRoom.AddPlayer(player.Name, player.Id, out generatedToken);

                if (assignedSeat == PlayerSeat.P1)
                {
                    targetRoom.Player1Socket = webSocket;
                    targetRoom.Player1 = new Player(player.Id, player.Name);
                }
                else
                {
                    targetRoom.Player2Socket = webSocket;
                    targetRoom.Player2 = new Player(player.Id, player.Name);
                }
            }
            finally
            {
                targetRoom.JoinLock.Release();
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
                await _cardService.PrepareDecksAsync(targetRoom.Player1, targetRoom.Player2);
                var match = await _gameService.StartMatchAsync(targetRoom.Player1, targetRoom.Player2);
                bool matchSaved = false;
                try
                {
                    await _matchRepository.CreateMatchAsync(
                        match.MatchId.ToString(),
                        targetRoom.Player1.Id,
                        targetRoom.Player2.Id);
                    matchSaved = true;
                }
                finally
                {
                    if (!matchSaved)
                    {
                        _matchManager.RemoveMatch(match.MatchId);
                    }
                }

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
                await SendDecisionToDecidingPlayerAsync(targetRoom, match);
            }
            return (targetRoom, assignedSeat);
        }

        private async Task HandleGameMessageAsync(WebSocket webSocket, Room room, PlayerSeat seat, string messageType, object? rawPayload)
        {
            Player? actingPlayer = room.GetPlayer(seat);
            if (actingPlayer == null || room.MatchId == null || rawPayload is not JsonElement payloadElement) return;

            var currentMatch = _matchManager.GetMatch(room.MatchId.Value);
            if (currentMatch == null) return;

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
                
                case nameof(SubmitDecision):
                    SubmitDecision? submitDecision = payloadElement.Deserialize<SubmitDecision>(SocketMessageSender.JsonOptions);
                    if (submitDecision != null && !string.IsNullOrWhiteSpace(submitDecision.OptionId))
                    {
                        command = CreateSubmitDecisionCommand(currentMatch, actingPlayer, submitDecision);
                    }
                    break;

                default:
                    command = null;
                    break;
            }

            if (command == null && messageType != nameof(PlayCard) && messageType != nameof(Attack) && messageType != nameof(SubmitDecision))
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

            if ((currentMatch.GameState.Phase == MindDuck.Rules.Models.GamePhase.AwaitingDecision || currentMatch.GameState.Phase == MindDuck.Rules.Models.GamePhase.AwaitingBlock) && command is not SubmitDecisionCommand)
            {
                string decisionMessage;
                switch (currentMatch.GameState.PendingDecision?.Type)
                {
                    case DecisionType.Block:
                        decisionMessage = "blokowanie ataku";
                        break;

                    case DecisionType.MindDuck:
                        decisionMessage = "użycie Mindbuga";
                        break;

                    default:
                        decisionMessage = "decyzja w toku";
                        break;
                }
                
                await _messageSender.SendAsync(webSocket, new ActionRejected
                {
                    Code = ErrorCode.IllegalMove,
                    Message = $"Oczekiwanie na decyzję {decisionMessage}" 
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

            JsonElement commandPayload = JsonSerializer.SerializeToElement(
                command,
                command.GetType(),
                SocketMessageSender.JsonOptions);
            string matchEvent = JsonSerializer.Serialize(new
            {
                type = command.GetType().Name,
                payload = commandPayload
            }, SocketMessageSender.JsonOptions);
            await _matchRepository.AppendEventsAsync(room.MatchId.Value.ToString(), actingPlayer.Id, new List<string> { matchEvent });

            var match = _matchManager.GetMatch(room.MatchId.Value);
            await SendGameStateAsync(room, match);
            await SendDecisionToDecidingPlayerAsync(room, match);
        }

        private SubmitDecisionCommand? CreateSubmitDecisionCommand(Match match, Player actingPlayer, SubmitDecision submitDecision)
        {
            IReadOnlyList<GameCommand> legalActions = _gameEngine.GetLegalActions(match.GameState, actingPlayer);
            for (int i = 0; i < legalActions.Count; i++)
            {
                if (legalActions[i] is SubmitDecisionCommand)
                {
                    SubmitDecisionCommand legalDecision = (SubmitDecisionCommand)legalActions[i];
                    string legalOptionId = FormatOptionId(legalDecision);
                    if (legalDecision.DecisionId == submitDecision.DecisionId && legalOptionId == submitDecision.OptionId)
                    {
                        return new SubmitDecisionCommand(
                            submitDecision.DecisionId,
                            legalDecision.DecisionType,
                            legalDecision.Accepted,
                            legalDecision.DefenderCardId);
                    }
                }
            }

            return null;
        }

        private async Task SendDecisionToDecidingPlayerAsync(Room room, Match match)
        {
            GameState state = match.GameState;
            if ((state.Phase != MindDuck.Rules.Models.GamePhase.AwaitingDecision && state.Phase != MindDuck.Rules.Models.GamePhase.AwaitingBlock) || state.PendingDecision == null)
            {
                return;
            }

            PendingDecision pendingDecision = state.PendingDecision;
            Player decidingPlayer = pendingDecision.RespondingPlayer;

            WebSocket? targetSocket = null;

            if (room.Player1 != null && room.Player1.Id == decidingPlayer.Id)
            {
                targetSocket = room.Player1Socket;
            }
            else if (room.Player2 != null && room.Player2.Id == decidingPlayer.Id)
            {
                targetSocket = room.Player2Socket;
            }

            if (targetSocket == null || targetSocket.State != WebSocketState.Open)
            {
                return;
            }

            DecisionKind kind;
            switch (pendingDecision.Type)
            {
                case DecisionType.Block:
                    kind = DecisionKind.Block;
                    break;

                case DecisionType.MindDuck:
                default:
                    kind = DecisionKind.UseMindbug;
                    break;
            }

            List<DecisionOptionDto> options = new List<DecisionOptionDto>();
            IReadOnlyList<GameCommand> legalActions = _gameEngine.GetLegalActions(state, decidingPlayer);
            for (int i = 0; i < legalActions.Count; i++)
            {
                if (legalActions[i] is SubmitDecisionCommand legalDecision && legalDecision.DecisionId == pendingDecision.DecisionId)
                {
                    string optionId = FormatOptionId(legalDecision);
                    options.Add(new DecisionOptionDto
                    {
                        OptionId = optionId,
                        TargetInstanceId = legalDecision.DefenderCardId,
                        Label = optionId
                    });
                }
            }

            DecisionRequired message = new DecisionRequired
            {
                DecisionId = pendingDecision.DecisionId,
                Kind = kind,
                SubjectCardInstanceId = pendingDecision.Card.InstanceId,
                Prompt = string.Empty,
                Options = options
            };

            await _messageSender.SendAsync(targetSocket, message);
        }

        private static string FormatOptionId(SubmitDecisionCommand command)
        {
            if (!command.Accepted)
            {
                return "decline";
            }

            if (command.DefenderCardId.HasValue)
            {
                return $"block:{command.DefenderCardId.Value}";
            }

            return "accept";
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

