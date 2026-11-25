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

            _connection.StateUpdated += OnStateUpdated;
            _connection.DecisionRequired += OnDecisionRequired;
            _connection.ActionRejected += OnActionRejected;
            _connection.OpponentConnectionChanged += OnOpponentConnectionChanged;
            _connection.MatchEnded += OnMatchEnded;
            _connection.ConnectionLost += OnConnectionLost;

            statusText.text = "Connecting...";
        }

        private void OnDestroy()
        {
            if (_connection == null) return;

            _connection.StateUpdated -= OnStateUpdated;
            _connection.DecisionRequired -= OnDecisionRequired;
            _connection.ActionRejected -= OnActionRejected;
            _connection.OpponentConnectionChanged -= OnOpponentConnectionChanged;
            _connection.MatchEnded -= OnMatchEnded;
            _connection.ConnectionLost -= OnConnectionLost;
        }

        private void OnStateUpdated(StateUpdate update)
        {
            if (update.Sequence <= _lastSequence) return;

            _lastSequence = update.Sequence;
            _state = update.State;
            Redraw(_state);
        }

        private void Redraw(GameStateView state)
        {
            bool isYourTurn = (state.ActiveSeat == state.YourSeat);

            yourInfo.Show(state.You.Nickname, state.You.Life, state.You.MindbugsLeft, state.You.DrawPileCount, isYourTurn);
            opponentInfo.Show(state.Opponent.Nickname, state.Opponent.Life, state.Opponent.MindbugsLeft, state.Opponent.DrawPileCount,!isYourTurn);

            yourHand.ShowCards(state.You.Hand, state.LegalActions.PlayableCardIds);
            opponentHand.ShowHidden(state.Opponent.HandCount);

            yourBoard.Show(state.You.Board, state.LegalActions.AttackerIds);
            opponentBoard.Show(state.Opponent.Board, new List<int>());

        }

        private void OnDecisionRequired(DecisionRequired decision)
        {
            // show DecisionPanel (networking/menus)
        }

        private void OnActionRejected(ActionRejected rejected)
        {
        }

        private void OnOpponentConnectionChanged(bool isConnected, int secondsLeft)
        {
            statusText.text = isConnected ? "Opponent is back" : $"Opponent disconnected. Waiting {secondsLeft} s";
        }

        private void OnMatchEnded(MatchEnded ended)
        {
            bool youWon = _state != null && ended.Winner == _state.YourSeat;
            statusText.text = youWon ? "You won!" : $"Game over ({ended.Reason})";
            // opens game-over screen (networking/menus stuff).
        }

        private void OnConnectionLost()
        {
            statusText.text = "Connection lost";
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

        private static string DescribeTurn(GameStateView state)
        {
            return state.ToString();
        }
    }
}