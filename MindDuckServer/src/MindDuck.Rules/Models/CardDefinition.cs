using System;
using System.Collections.Generic;
using MindDuck.Rules.Interfaces;

namespace MindDuck.Rules.Models;

public sealed class CardDefinition
{
    private readonly HashSet<Keyword> _keywords;
    private readonly IReadOnlyList<ICardEffect> _effects;

    public string Id { get; }
    public string Name { get; }
    public int Power { get; }

    public IReadOnlyCollection<Keyword> Keywords => _keywords;
    public IReadOnlyList<ICardEffect> Effects => _effects;

    public CardDefinition(string id, string name, int power, IEnumerable<Keyword>? keywords = null, IEnumerable<ICardEffect>? effects = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Card ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Card name cannot be empty.", nameof(name));

        if (power < 1)
            throw new ArgumentException("Card power must be at least 1.", nameof(power));

        Id = id;
        Name = name;
        Power = power;

        _keywords = keywords != null
            ? new HashSet<Keyword>(keywords)
            : new HashSet<Keyword>();

        _effects = new List<ICardEffect>(effects ?? Array.Empty<ICardEffect>()).AsReadOnly();
    }

    public bool HasKeyword(Keyword keyword)
    {
        return _keywords.Contains(keyword);
    }
}