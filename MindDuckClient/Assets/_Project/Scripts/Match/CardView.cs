using MindDuck.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text keywordText;
    [SerializeField] private TMP_Text abilityText;

    [Header("Visuals")]
    [SerializeField] private Image artImage;
    [SerializeField] private GameObject highlight;

    [Header("Input")]
    [SerializeField] private Button button;


    public int InstanceId { get; private set; }

    public event Action<int> Clicked;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(() => Clicked?.Invoke(InstanceId));
        }
    }

    public void Show(int instanceId, CardDefinitionDto definition)
    {
        InstanceId = instanceId;

        if (definition == null)
        {
            nameText.text = "?";
            powerText.text = "";
            keywordText.text = "";
            abilityText.text = $"Unknown card (#{instanceId})";
            return;
        }

        nameText.text = definition.Name;
        powerText.text = definition.Power.ToString();
        keywordText.text = definition.Keyword.HasValue ? definition.Keyword.Value.ToString() : "";
        abilityText.text = DescribeAbilities(definition.Abilities);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        if (highlight != null)
        {
            highlight.SetActive(isHighlighted);
        }
    }

    private static string DescribeAbilities(List<AbilityDto> abilities)
    {
        if (abilities == null || abilities.Count == 0)
        {
            return "";
        }

        var text = new StringBuilder();
        foreach (AbilityDto ability in abilities)
        {
            if (text.Length > 0) text.Append('\n');
            text.Append(ability.Trigger).Append(": ").Append(DescribeEffect(ability));
        }
        return text.ToString();
    }

    private static string DescribeEffect(AbilityDto ability)
    {
        switch (ability.Effect)
        {
            case AbilityEffect.GainLife: return "gain 2 life";
            case AbilityEffect.DefeatEnemyCreature: return $"defeat an enemy creature with power > {ability.Value}";
            case AbilityEffect.OpponentDiscards: return $"opponent discards {ability.Value}";
            default: return ability.Effect.ToString();
        }
    }
}