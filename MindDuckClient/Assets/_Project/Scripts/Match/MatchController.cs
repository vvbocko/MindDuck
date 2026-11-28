using MindDuck.Client.Cards;
using MindDuck.Client.Networking;
using MindDuck.Contracts;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MindDuck.Client.Match
{
    public class MatchController : MonoBehaviour
    {
        [Header("Your side")]
        [SerializeField] private PlayerInfoView yourInfo;
        [SerializeField] private HandView yourHand;
        [SerializeField] private BoardView yourBoard;

        [Header("Opponent side")]
        [SerializeField] private PlayerInfoView opponentInfo;
        [SerializeField] private HandView opponentHand;
        [SerializeField] private BoardView opponentBoard;

        [Header("Middle")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private DecisionPanel decisionPanel;

        private static readonly List<int> NoIds = new List<int>();

        private IGameConnection _connection;
        private GameStateView _state;
        private int _lastSequence = -1;

        public void Initialize(IGameConnection connection, ICardCatalog catalog)
        {
            _connection = connection;

            yourHand.Initialize(catalog);
            yourBoard.Initialize(catalog);
            opponentHand.Initialize(catalog);
            opponentBoard.Initialize(catalog);

            yourHand.CardClicked += OnYourHandCardClicked;
            yourBoard.CreatureClicked += OnYourCreatureClicked;
            opponentBoard.CreatureClicked += OnOpponentCreatureClicked;

            _connection.MatchStarted += OnMatchStarted;
            _connection.StateUpdated += OnStateUpdated;
            _connection.DecisionRequired += OnDecisionRequired;
            _connection.ActionRejected += OnActionRejected;
            _connection.OpponentConnectionChanged += OnOpponentConnectionChanged;
            _connection.MatchEnded += OnMatchEnded;
            _connection.ConnectionLost += OnConnectionLost;
            _connection.Error += OnError;

            decisionPanel.OptionChosen += OnDecisionOptionChosen;
            decisionPanel.Hide();

            statusText.text = "Connecting...";
        }

        private void OnDestroy()
        {
            if (_connection == null) return;

            _connection.MatchStarted -= OnMatchStarted;
            _connection.StateUpdated -= OnStateUpdated;
            _connection.DecisionRequired -= OnDecisionRequired;
            _connection.ActionRejected -= OnActionRejected;
            _connection.OpponentConnectionChanged -= OnOpponentConnectionChanged;
            _connection.MatchEnded -= OnMatchEnded;
            _connection.ConnectionLost -= OnConnectionLost;
            _connection.Error -= OnError;
            
            decisionPanel.OptionChosen -= OnDecisionOptionChosen;
        }

        private void OnMatchStarted(MatchStarted started)
        {
            statusText.text = "Match started!";
        }

        private void OnStateUpdated(StateUpdate update)
        {
            if (update.Sequence <= _lastSequence) return;

            _lastSequence = update.Sequence;
            _state = update.State;

            Redraw(_state);

            // could ad danimations? use update.Events
        }

        private void Redraw(GameStateView state)
        {
            bool isYourTurn = (state.ActiveSeat == state.YourSeat);

            yourInfo.Show(state.You.Nickname, state.You.Life, state.You.MindbugsLeft, state.You.DrawPileCount, isYourTurn);
            opponentInfo.Show(state.Opponent.Nickname, state.Opponent.Life, state.Opponent.MindbugsLeft, state.Opponent.DrawPileCount,!isYourTurn);

            yourHand.ShowCards(state.You.Hand, state.LegalActions.PlayableCardIds);
            opponentHand.ShowHidden(state.Opponent.HandCount);

            yourBoard.Show(state.You.Board, state.LegalActions.AttackerIds);
            opponentBoard.Show(state.Opponent.Board, NoIds);

            statusText.text = DescribeTurn(state);

            bool waitingForYourDecision = state.Phase == GamePhase.AwaitingDecision && state.WaitingForSeat == state.YourSeat;
            if (!waitingForYourDecision)
            {
                decisionPanel.Hide();
            }
        }

        private void OnDecisionRequired(DecisionRequired decision)
        {
            decisionPanel.Show(decision);
            Debug.Log($"[MatchController] Decision {decision.DecisionId} ({decision.Kind}): {decision.Prompt}");
        }

        private void OnActionRejected(ActionRejected rejected)
        {
            statusText.text = rejected.Message;
            Debug.LogWarning($"[MatchController] Rejected: {rejected.Code} - {rejected.Message}");
        }

        private void OnOpponentConnectionChanged(bool isConnected, int secondsLeft)
        {
            statusText.text = isConnected ? "Opponent is back" : $"Opponent disconnected. Waiting {secondsLeft} s";
        }

        private void OnMatchEnded(MatchEnded ended)
        {
            decisionPanel.Hide();

            bool youWon = _state != null && ended.Winner == _state.YourSeat;
            statusText.text = youWon ? "You won!" : $"Game over ({ended.Reason})";
            // opens game-over screen (networking/menus stuff).
        }

        private void OnConnectionLost()
        {
            statusText.text = "Connection lost";
        }

        private void OnError(Error error)
        {
            statusText.text = "Error: " + error.Message;
            Debug.LogWarning($"[MatchController] Server error ({error.Code}): {error.Message}");
        }


        private void OnYourHandCardClicked(int cardInstanceId)
        {
            if (_state == null) return;

            if (_state.LegalActions.PlayableCardIds.Contains(cardInstanceId))
            {
                _connection.PlayCard(cardInstanceId);
            }
        }

        private void OnYourCreatureClicked(int creatureInstanceId)
        {
            if (_state == null) return;

            if (_state.LegalActions.AttackerIds.Contains(creatureInstanceId))
            {
                _connection.Attack(creatureInstanceId);
            }
        }

        private void OnOpponentCreatureClicked(int creatureInstanceId)
        {
            if (_state == null) return;

            Debug.Log($"[MatchController] boop {creatureInstanceId}");

        }

        private void OnDecisionOptionChosen(int decisionId, string optionId)
        {
            _connection.SubmitDecision(decisionId, optionId);
        }

        private static string DescribeTurn(GameStateView state)
        {
            if (state.Phase == GamePhase.GameOver) return "Game over";

            if (state.Phase == GamePhase.AwaitingDecision)
            {
                return state.WaitingForSeat == state.YourSeat
                    ? "Your decision..."
                    : $"Waiting for {state.Opponent.Nickname}...";
            }

            return state.ActiveSeat == state.YourSeat
                ? $"Turn {state.TurnNumber}: your turn - play a card or attack"
                : $"Turn {state.TurnNumber}: {state.Opponent.Nickname}'s turn";
          
        }
    }
}