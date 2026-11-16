using MindDuck.Contracts;
using UnityEngine;

namespace MindDuck.Client.Match
{
    [RequireComponent(typeof(CardView))]
    public class CreatureView : MonoBehaviour
    {
        [SerializeField] private CardView card;

        public CardView Card => card;

        public void Show(CreatureDto creature, CardDefinitionDto definition)
        {
            card.Show(creature.InstanceId, definition);
        }
    }
}