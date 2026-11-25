using MindDuck.Client.Cards;
using MindDuck.Contracts;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MindDuck.Client.Match
{
    public class HandView : MonoBehaviour
    {
        [SerializeField] private CardView cardPrefab;      // for players hand
        [SerializeField] private GameObject cardBackPrefab; // for oponents hand (backs of cards)
        [SerializeField] private Transform layoutContainer;

        public event Action<int> CardClicked;

        private ICardCatalog _catalog;

        public void Initialize(ICardCatalog catalog)
        {
            _catalog = catalog;
        }

        public void ShowCards(IReadOnlyList<CardRefDto> cards, ICollection<int> playableIds)
        {
            Clear();

            foreach (CardRefDto card in cards)
            {
                CardView view = Instantiate(cardPrefab, layoutContainer);

                _catalog.TryGet(card.DefinitionId, out var definition);
                view.Show(card.InstanceId, definition);

                bool isCardPlayable = playableIds.Contains(card.InstanceId);
                view.SetHighlighted(isCardPlayable);

                view.Clicked += id => CardClicked?.Invoke(id);
            }
        }

        public void ShowHidden(int count)
        {
            Clear();

            for (int i = 0; i < count; i++)
            {
                Instantiate(cardBackPrefab, layoutContainer);
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