using TMPro;
using UnityEngine;

namespace MindDuck.Client.Match
{
    public sealed class PlayerInfoView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text lifesNumberText;
        [SerializeField] private TMP_Text mindducksNumberText;
        [SerializeField] private TMP_Text drawPileText;
        [SerializeField] private GameObject activeTurnMarker;

        public void Show(string nickname, int life, int mindducksLeft, int drawPileCount, bool isTheirTurn)
        {

        }
    }
}