// FakeGameConnection - pretends to be the server, so the Unity screens can be built WITHOUT a server.
//
// WHY IT EXISTS
//   The real WebSocketGameConnection and the server's game loop aren't ready. This class implements
//   the same IGameConnection, but answers by itself, inside Unity, like a small scripted match vs "Bot":
//     - JoinRoom    -> a starting game: 5 cards in your hand, 5 hidden cards in Bot's hand
//     - PlayCard    -> your card goes to your board, you draw back to 5, then it's Bot's turn
//     - Attack      -> Bot loses 1 life (no blocking yet), then it's Bot's turn
//     - Bot's turn  -> after 1 second Bot plays a card or attacks you (it alternates)
//     - every 2nd card Bot plays asks YOU "use a Mindbug?" (DecisionRequired) - answer with
//       SubmitDecision(decisionId, "use" or "pass"). This lets the DecisionPanel be built and tested.
//   It is NOT the real rules (no blocking, no keywords, no abilities). The server will do that.
//
// WHAT IT SENDS
//   Exactly what the real server will send: every change = a NEW StateUpdate with a higher Sequence
//   and the complete GameStateView; questions = DecisionRequired; errors = ActionRejected.
//   The card ids (definitionId) exist in data/cards/cards.json, so a card catalog can show them.
//
// WHO OWNS IT
//   The networking owner. Extend it freely (blocking, reconnect...) - the game screen won't change.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MindDuck.Contracts;
using UnityEngine;

namespace MindDuck.Client.Networking
{
    public sealed class FakeGameConnection : IGameConnection
    {
        // Some events are never raised by this fake. Without this line Unity shows
        // "warning CS0067: The event ... is never used". Harmless.
#pragma warning disable CS0067
        public event Action<RoomJoined> RoomJoined;
        public event Action<StateUpdate> StateUpdated;
        public event Action<DecisionRequired> DecisionRequired;
        public event Action<ActionRejected> ActionRejected;
        public event Action<bool, int> OpponentConnectionChanged;
        public event Action<MatchEnded> MatchEnded;
        public event Action ConnectionLost;
#pragma warning restore CS0067

        private const int HandSize = 5;
        private const float BotThinkSeconds = 1f;
        private const string BotName = "Bot";

        // The fake deals the cards in this order. All ids exist in data/cards/cards.json.
        private static readonly string[] YourDeck =
        {
            "quackoon", "duckobra", "quacksmith", "wereduck", "mallardillo",
            "quacken", "beakgeist", "quackini", "honk-kong", "ducktopus"
        };
        private static readonly string[] BotDeck =
        {
            "quacktus", "quackrobat", "quackula", "eggsploder", "duckydra",
            "drakenstein", "pteroducktyl", "quackodile", "ducklops", "duckasaurus-rex"
        };

        private GameStateView _state;     // the fake "server's" current game
        private int _sequence;            // increases with every StateUpdate (protocol.md 9.2)
        private int _yourNextCard;        // index into YourDeck
        private int _botNextCard;         // index into BotDeck
        private int _botCardsPlayed;      // every 2nd one asks you about a Mindbug
        private bool _botAttacksNext;     // Bot alternates: play, attack, play, ...
        private int _decisionId;
        private CardRefDto _cardWaitingForMindbug; // Bot's card you're being asked about
        private bool _stopped;            // LeaveRoom was called - stop the Bot

        // ---- Client -> "Server" -----------------------------------------------------------

        public Task ConnectAsync(string url)
        {
            Debug.Log($"[FakeGameConnection] Pretending to connect to {url}");
            return Task.CompletedTask; // "already finished", nothing to wait for
        }

        public void JoinRoom(string roomCode, string nickname)
        {
            Debug.Log($"[FakeGameConnection] JoinRoom {roomCode} as {nickname}");

            RoomJoined?.Invoke(new RoomJoined
            {
                RoomCode = roomCode,
                Seat = PlayerSeat.P1,
                PlayerToken = "fake-token",
                OpponentNickname = BotName
            });

            _state = BuildStartingState(nickname);
            UpdateLegalActions();
            SendState(Event(GameEventType.TurnStarted, PlayerSeat.P1));
        }

        public void Rejoin(string roomCode, string playerToken)
        {
            // A real server would send the latest state again - so does the fake.
            if (_state != null) SendState(Event(GameEventType.TurnStarted, _state.ActiveSeat));
        }

        public void PlayCard(int cardInstanceId)
        {
            if (!IsYourAction()) { Reject(ErrorCode.NotYourTurn, "It is not your turn."); return; }

            CardRefDto card = _state.You.Hand.Find(c => c.InstanceId == cardInstanceId);
            if (card == null) { Reject(ErrorCode.IllegalMove, $"Card {cardInstanceId} is not in your hand."); return; }

            _state.You.Hand.Remove(card);
            _state.You.Board.Add(new CreatureDto { InstanceId = card.InstanceId, DefinitionId = card.DefinitionId });
            DrawYourCards();

            SendState(Event(GameEventType.CardPlayed, PlayerSeat.P1, card.InstanceId),
                      Event(GameEventType.CreatureEntered, PlayerSeat.P1, card.InstanceId));
            StartBotTurn();
        }

        public void Attack(int creatureInstanceId)
        {
            if (!IsYourAction()) { Reject(ErrorCode.NotYourTurn, "It is not your turn."); return; }

            if (!_state.You.Board.Exists(c => c.InstanceId == creatureInstanceId))
            {
                Reject(ErrorCode.IllegalMove, $"Creature {creatureInstanceId} is not on your board.");
                return;
            }

            _state.Opponent.Life = Math.Max(0, _state.Opponent.Life - 1);
            SendState(Event(GameEventType.AttackUnblocked, PlayerSeat.P1, creatureInstanceId),
                      Event(GameEventType.LifeChanged, PlayerSeat.P2, amount: -1));

            if (_state.Opponent.Life == 0) { EndMatch(PlayerSeat.P1); return; }
            StartBotTurn();
        }

        public void SubmitDecision(int decisionId, string optionId)
        {
            if (_cardWaitingForMindbug == null || decisionId != _decisionId)
            {
                Reject(ErrorCode.InvalidDecision, "There is no such question open.");
                return;
            }

            CardRefDto card = _cardWaitingForMindbug;
            var creature = new CreatureDto { InstanceId = card.InstanceId, DefinitionId = card.DefinitionId };

            if (optionId == "use")
            {
                // Rules 4.2: the creature joins YOUR board, and Bot takes another turn.
                _cardWaitingForMindbug = null;
                _state.You.MindbugsLeft--;
                _state.You.Board.Add(creature);
                _state.Phase = GamePhase.AwaitingAction;
                _state.WaitingForSeat = PlayerSeat.P2;
                SendState(Event(GameEventType.CardMindbugged, PlayerSeat.P1, card.InstanceId));
                RunBotTurnLater();
            }
            else if (optionId == "pass")
            {
                _cardWaitingForMindbug = null;
                _state.Opponent.Board.Add(creature);
                SendState(Event(GameEventType.CreatureEntered, PlayerSeat.P2, card.InstanceId));
                StartYourTurn();
            }
            else
            {
                Reject(ErrorCode.InvalidDecision, $"Unknown option '{optionId}'. Use 'use' or 'pass'.");
            }
        }

        public void LeaveRoom()
        {
            Debug.Log("[FakeGameConnection] LeaveRoom");
            _stopped = true;
        }

        // ---- Bot ------------------------------------------------------------------------------

        private void StartBotTurn()
        {
            _state.ActiveSeat = PlayerSeat.P2;
            _state.WaitingForSeat = PlayerSeat.P2;
            _state.Phase = GamePhase.AwaitingAction;
            _state.TurnNumber++;
            UpdateLegalActions();
            SendState(Event(GameEventType.TurnStarted, PlayerSeat.P2));
            RunBotTurnLater();
        }

        // "async void" = start it and don't wait. Awaitable.WaitForSecondsAsync is Unity's way to wait
        // without blocking the game; it also works in WebGL (Task.Delay doesn't).
        private async void RunBotTurnLater()
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(BotThinkSeconds);
                if (_stopped || _state == null || _state.Phase == GamePhase.GameOver) return;
                BotActs();
            }
            catch (OperationCanceledException)
            {
                // Play mode was stopped while Bot was "thinking" - nothing to do.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void BotActs()
        {
            bool canAttack = _state.Opponent.Board.Count > 0;
            bool canPlay = _state.Opponent.HandCount > 0 && _botNextCard < BotDeck.Length;

            if (canAttack && (_botAttacksNext || !canPlay))
            {
                _botAttacksNext = false;
                BotAttacks();
            }
            else if (canPlay)
            {
                _botAttacksNext = true;
                BotPlaysCard();
            }
            else
            {
                StartYourTurn(); // Bot can't do anything
            }
        }

        private void BotAttacks()
        {
            CreatureDto attacker = _state.Opponent.Board[0];
            _state.You.Life = Math.Max(0, _state.You.Life - 1);

            SendState(Event(GameEventType.AttackUnblocked, PlayerSeat.P2, attacker.InstanceId),
                      Event(GameEventType.LifeChanged, PlayerSeat.P1, amount: -1));

            if (_state.You.Life == 0) { EndMatch(PlayerSeat.P2); return; }
            StartYourTurn();
        }

        private void BotPlaysCard()
        {
            // Bot's cards are hidden: only HandCount / DrawPileCount change, then the card appears.
            var card = new CardRefDto { InstanceId = 21 + _botNextCard, DefinitionId = BotDeck[_botNextCard] };
            _botNextCard++;
            _state.Opponent.HandCount--;
            if (_state.Opponent.DrawPileCount > 0)
            {
                _state.Opponent.DrawPileCount--;
                _state.Opponent.HandCount++;
            }

            _botCardsPlayed++;
            bool askAboutMindbug = _botCardsPlayed % 2 == 0 && _state.You.MindbugsLeft > 0;

            if (askAboutMindbug)
            {
                _cardWaitingForMindbug = card;
                _state.Phase = GamePhase.AwaitingDecision;
                _state.WaitingForSeat = PlayerSeat.P1;
                UpdateLegalActions();
                SendState(Event(GameEventType.CardPlayed, PlayerSeat.P2, card.InstanceId));
                AskAboutMindbug(card);
                return;
            }

            _state.Opponent.Board.Add(new CreatureDto { InstanceId = card.InstanceId, DefinitionId = card.DefinitionId });
            SendState(Event(GameEventType.CardPlayed, PlayerSeat.P2, card.InstanceId),
                      Event(GameEventType.CreatureEntered, PlayerSeat.P2, card.InstanceId));
            StartYourTurn();
        }

        private void AskAboutMindbug(CardRefDto card)
        {
            _decisionId++;
            DecisionRequired?.Invoke(new DecisionRequired
            {
                DecisionId = _decisionId,
                Kind = DecisionKind.UseMindbug,
                SubjectCardInstanceId = card.InstanceId,
                Prompt = $"{BotName} played {card.DefinitionId}. Use one of your Mindbugs to take it?",
                Options = new List<DecisionOptionDto>
                {
                    new DecisionOptionDto { OptionId = "use", Label = "Use a Mindbug" },
                    new DecisionOptionDto { OptionId = "pass", Label = $"Let {BotName} have it" }
                }
            });
        }

        // ---- helpers --------------------------------------------------------------------------

        private void StartYourTurn()
        {
            _state.ActiveSeat = PlayerSeat.P1;
            _state.WaitingForSeat = PlayerSeat.P1;
            _state.Phase = GamePhase.AwaitingAction;
            _state.TurnNumber++;
            UpdateLegalActions();
            SendState(Event(GameEventType.TurnStarted, PlayerSeat.P1));
        }

        private void EndMatch(PlayerSeat winner)
        {
            _state.Phase = GamePhase.GameOver;
            _state.WaitingForSeat = null;
            UpdateLegalActions();
            SendState(Event(GameEventType.MatchEnded, winner));
            MatchEnded?.Invoke(new MatchEnded { Winner = winner, Reason = MatchEndReason.LifeZero });
        }

        private bool IsYourAction()
        {
            return _state != null
                && _state.Phase == GamePhase.AwaitingAction
                && _state.ActiveSeat == PlayerSeat.P1;
        }

        /// <summary>Draw back up to 5 (rules section 3) from YourDeck while it has cards.</summary>
        private void DrawYourCards()
        {
            while (_state.You.Hand.Count < HandSize && _yourNextCard < YourDeck.Length)
            {
                _state.You.Hand.Add(new CardRefDto { InstanceId = 1 + _yourNextCard, DefinitionId = YourDeck[_yourNextCard] });
                _yourNextCard++;
                _state.You.DrawPileCount = YourDeck.Length - _yourNextCard;
            }
        }

        /// <summary>On your turn you may play every hand card and attack with every creature; otherwise nothing.</summary>
        private void UpdateLegalActions()
        {
            if (IsYourAction())
            {
                _state.LegalActions.PlayableCardIds = _state.You.Hand.ConvertAll(c => c.InstanceId);
                _state.LegalActions.AttackerIds = _state.You.Board.ConvertAll(c => c.InstanceId);
            }
            else
            {
                _state.LegalActions.PlayableCardIds = new List<int>();
                _state.LegalActions.AttackerIds = new List<int>();
            }
        }

        private void SendState(params GameEventDto[] whatHappened)
        {
            _sequence++;
            StateUpdated?.Invoke(new StateUpdate
            {
                Sequence = _sequence,
                State = _state,
                Events = new List<GameEventDto>(whatHappened)
            });
        }

        private void Reject(ErrorCode code, string message)
        {
            ActionRejected?.Invoke(new ActionRejected { Code = code, Message = message });
        }

        private static GameEventDto Event(GameEventType type, PlayerSeat seat, int? cardInstanceId = null, int? amount = null)
        {
            return new GameEventDto { Type = type, Seat = seat, CardInstanceId = cardInstanceId, Amount = amount };
        }

        private GameStateView BuildStartingState(string nickname)
        {
            var state = new GameStateView
            {
                MatchId = "fake-match",
                YourSeat = PlayerSeat.P1,
                ActiveSeat = PlayerSeat.P1,
                Phase = GamePhase.AwaitingAction,
                WaitingForSeat = PlayerSeat.P1,
                TurnNumber = 1,
                You = new PlayerView { Nickname = nickname, Life = 3, MindbugsLeft = 2 },
                Opponent = new OpponentView { Nickname = BotName, Life = 3, MindbugsLeft = 2, HandCount = HandSize, DrawPileCount = BotDeck.Length - HandSize }
            };

            _state = state;
            DrawYourCards(); // your first 5 cards
            return state;
        }
    }
}
