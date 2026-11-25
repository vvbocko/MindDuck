using MindDuck.Client.Cards;
using MindDuck.Contracts;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MindDuck.Client.Match
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private CreatureView creaturePrefab;
        [SerializeField] private Transform layoutContainer;

        public event Action<int> CreatureClicked;

        private ICardCatalog _catalog;

        public void Initialize(ICardCatalog catalog)
        {
            _catalog = catalog;
        }

        public void Show(IReadOnlyList<CreatureDto> creatures, ICollection<int> highlightIds)
        {
            Clear();

            foreach (CreatureDto creature in creatures)
            {
                CreatureView view = Instantiate(creaturePrefab, layoutContainer);

                _catalog.TryGet(creature.DefinitionId, out var definition);
                view.Show(creature, definition);

                bool isHighlighted = highlightIds.Contains(creature.InstanceId);
                view.SetHighlighted(isHighlighted);

                view.Card.Clicked += id => CreatureClicked?.Invoke(id);
            }
        }

        private void Clear()
        {
            foreach (Transform child in layoutContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}