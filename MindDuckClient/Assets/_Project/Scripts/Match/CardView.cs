using System;
using System.Collections.Generic;
using MindDuck.Contracts;
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
    }

    private static string DescribeAbilities(List<AbilityDto> abilities)
    {
        return abilities.ToString();
    }

    private static string DescribeEffect(AbilityDto ability)
    {
        return ability.Effect.ToString();
    }
}