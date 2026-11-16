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

        public void Show(IReadOnlyList<CreatureDto> creatures, ICollection<int> highlightIds)
        {
            Clear();

            foreach (CreatureDto creature in creatures)
            {
                CreatureView view = Instantiate(creaturePrefab, layoutContainer);
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