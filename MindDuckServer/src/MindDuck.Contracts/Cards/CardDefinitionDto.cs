namespace MindDuck.Contracts
{
    /// <summary>
    /// One card TYPE, exactly the shape of one entry in data/cards/cards.json and of GET /api/cards:
    /// { "id": "ember-wolf", "name": "Ember Wolf", "power": 7, "keyword": "Frenzy",
    ///   "ability": { "trigger": "Attack", "effect": "OpponentDiscards", "value": 1 }, "artKey": "ember-wolf" }
    /// </summary>
    public sealed class CardDefinitionDto
    {
        /// <summary>= DefinitionId everywhere else.</summary>
        public string Id { get; set; }
        public string Name { get; set; }
        public int Power { get; set; }
        /// <summary>null = no keyword.</summary>
        public Keyword? Keyword { get; set; }
        /// <summary>null = no ability.</summary>
        public AbilityDto Ability { get; set; }
        /// <summary>Which sprite the client shows (Unity looks it up by this name).</summary>
        public string ArtKey { get; set; }
    }

    public sealed class AbilityDto
    {
        public AbilityTrigger Trigger { get; set; }
        public AbilityEffect Effect { get; set; }
        /// <summary>X for DefeatEnemyCreature, N for OpponentDiscards, null for GainLife.</summary>
        public int? Value { get; set; }
    }
}
