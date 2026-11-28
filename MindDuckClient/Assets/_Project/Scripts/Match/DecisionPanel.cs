using MindDuck.Contracts;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindDuck.Client.Match
{
    public sealed class DecisionPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button optionButtonPrefab;

        private readonly List<Button> _buttons = new List<Button>();

        public event Action<int, string> OptionChosen;

        public void Show(DecisionRequired decision)
        {
            RemoveButtons();

            promptText.text = decision.Prompt;

            foreach (DecisionOptionDto option in decision.Options)
            {
                Button button = Instantiate(optionButtonPrefab, buttonContainer);
                button.GetComponentInChildren<TMP_Text>().text = option.Label;
                button.onClick.AddListener(() => Choose(decision.DecisionId, option.OptionId));
                _buttons.Add(button);
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            RemoveButtons();
            gameObject.SetActive(false);
        }

        private void Choose(int decisionId, string optionId)
        {
            Hide();
            OptionChosen?.Invoke(decisionId, optionId);
        }

        private void RemoveButtons()
        {
            foreach (Button button in _buttons)
            {
                Destroy(button.gameObject);
            }

            _buttons.Clear();
        }
    }
}
