using MindDuck.Contracts;
using UnityEngine;

namespace MindDuck.Client.Match
{
    [RequireComponent(typeof(CardView))]
    public class CreatureView : MonoBehaviour
    {
        [SerializeField] private CardView card;
        [SerializeField] private GameObject exhaustedOverlay;

        public CardView Card => card;

        public void Show(CreatureDto creature, CardDefinitionDto definition)
        {
            card.Show(creature.InstanceId, definition);

            if (exhaustedOverlay != null)
            {
                exhaustedOverlay.SetActive(creature.IsExhausted);
            }
        }

        public void SetHighlighted(bool isHighlighted)
        {
            card.SetHighlighted(isHighlighted);
        }
    }
}