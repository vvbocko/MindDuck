using TMPro;
using UnityEngine;

namespace MindDuck.Client.Match
{
    public sealed class PlayerInfoView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text livesNumberText;
        [SerializeField] private TMP_Text mindducksNumberText;
        [SerializeField] private TMP_Text drawPileText;
        [SerializeField] private GameObject activeTurnMarker;

        public void Show(string nickname, int life, int mindducksLeft, int drawPileCount, bool isTheirTurn)
        {
            nicknameText.SetText(nickname);
            livesNumberText.SetText("HP:  " + life);
            mindducksNumberText.SetText("MindDucks:  " + mindducksLeft);
            drawPileText.SetText("Draw cards pile:  " + drawPileCount);
            activeTurnMarker.SetActive(isTheirTurn);
        }
    }
}