// The game screen needs to turn a card id into name, power, keyword and abilities
// real catalog will come from GET /api/cards (networking owner,from IApiClient or sth) or read cards.json with Newtonsoft


// for now this class holds the same 20 cards as cards.json so the screen can draw real cards

// it needs to be kept in sync with cards.json or maybe replace this class with one that reads the JSON)
// ( ids here are the ones FakeGameConnection deals)

using System.Collections.Generic;
using MindDuck.Contracts;

namespace MindDuck.Client.Cards
{
    public sealed class LocalCardCatalog : ICardCatalog
    {
        private readonly Dictionary<string, CardDefinitionDto> _cards = new Dictionary<string, CardDefinitionDto>();

        public LocalCardCatalog()
        {
            // id, name, power, keyword (none), abilities (none, one or more)
            Add("quackoon", "Quackoon", 3, null,
                Ability(AbilityTrigger.Play, AbilityEffect.OpponentDiscards, 1));
            Add("duckobra", "Duckobra", 3, Keyword.Poisonous);
            Add("quacksmith", "Quacksmith", 4, null,
                Ability(AbilityTrigger.Play, AbilityEffect.GainLife, null));
            Add("wereduck", "Wereduck", 6, Keyword.Frenzy);
            Add("mallardillo", "Mallardillo", 3, Keyword.Tough,
                Ability(AbilityTrigger.Defeated, AbilityEffect.GainLife, null));
            Add("quacken", "Quacken", 9, Keyword.Tough);
            Add("beakgeist", "Beakgeist", 4, null,
                Ability(AbilityTrigger.Defeated, AbilityEffect.OpponentDiscards, 2));
            Add("quackini", "Quackini", 2, Keyword.Tough);
            Add("honk-kong", "Honk Kong", 8, null,
                Ability(AbilityTrigger.Attack, AbilityEffect.DefeatEnemyCreature, 6));
            Add("ducktopus", "Ducktopus", 5, Keyword.Poisonous,
                Ability(AbilityTrigger.Play, AbilityEffect.OpponentDiscards, 1));
            Add("quacktus", "Quacktus", 2, Keyword.Poisonous);
            Add("quackrobat", "Quackrobat", 4, Keyword.Frenzy);
            Add("quackula", "Quackula", 5, null,
                Ability(AbilityTrigger.Attack, AbilityEffect.GainLife, null));
            Add("eggsploder", "Eggsploder", 1, null,
                Ability(AbilityTrigger.Defeated, AbilityEffect.DefeatEnemyCreature, 3));
            Add("duckydra", "Duckydra", 6, Keyword.Frenzy);
            Add("drakenstein", "Drakenstein", 7, Keyword.Tough);
            Add("pteroducktyl", "Pteroducktyl", 5, null,
                Ability(AbilityTrigger.Play, AbilityEffect.DefeatEnemyCreature, 6));
            Add("quackodile", "Quackodile", 7, Keyword.Frenzy,
                Ability(AbilityTrigger.Attack, AbilityEffect.OpponentDiscards, 1));
            Add("ducklops", "Ducklops", 8, null);
            Add("duckasaurus-rex", "Duckasaurus Rex", 10, null);
        }

        public IReadOnlyCollection<CardDefinitionDto> All => _cards.Values;

        public bool TryGet(string definitionId, out CardDefinitionDto card)
        {
            if (definitionId == null)
            {
                card = null;
                return false;
            }
            return _cards.TryGetValue(definitionId, out card);
        }

        private void Add(string id, string name, int power, Keyword? keyword, params AbilityDto[] abilities)
        {
            _cards[id] = new CardDefinitionDto
            {
                Id = id,
                Name = name,
                Power = power,
                Keyword = keyword,
                Abilities = new List<AbilityDto>(abilities),
                ArtKey = id
            };
        }

        private static AbilityDto Ability(AbilityTrigger trigger, AbilityEffect effect, int? value)
        {
            return new AbilityDto { Trigger = trigger, Effect = effect, Value = value };
        }
    }
}
