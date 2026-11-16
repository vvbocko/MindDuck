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

        private GameStateView gameState;

        private void OnStateUpdated(StateUpdate update)
        {
        }

        private void Redraw(GameStateView state)
        {

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
            bool youWon = gameState != null && ended.Winner == gameState.YourSeat;
            statusText.text = youWon ? "You won!" : $"Game over ({ended.Reason})";
            // opens game-over screen (networking/menus stuff).
        }

        private void OnConnectionLost()
        {
            statusText.text = "Connection lost";
        }


        private void OnYourHandCardClicked(int cardInstanceId)
        {
            
        }

        private void OnYourCreatureClicked(int creatureInstanceId)
        {
            
        }

        private void OnOpponentCreatureClicked(int creatureInstanceId)
        {

        }

        private static string DescribeTurn(GameStateView state)
        {
            return state.ToString();
        }
    }
}