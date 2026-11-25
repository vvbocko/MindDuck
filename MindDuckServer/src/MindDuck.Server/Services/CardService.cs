using MindDuck.Contracts;
using MindDuck.Persistence;
using MindDuck.Rules.Effects;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using PersistenceCard = MindDuck.Persistence.Card;
using RulesCard = MindDuck.Rules.Models.Card;
using RulesKeyword = MindDuck.Rules.Models.Keyword;

namespace MindDuck.Server.Services;

public sealed class CardService(ICardRepository cardRepository) : ICardService
{
    private const int DeckSize = 10;

    public async Task PrepareDecksAsync(MindDuck.Rules.Models.Player playerOne, MindDuck.Rules.Models.Player playerTwo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(playerOne);
        ArgumentNullException.ThrowIfNull(playerTwo);

        if (playerOne.Id == playerTwo.Id)
        {
            throw new ArgumentException("Players must have different IDs.");
        }

        if (playerOne.DrawPile.Count != 0 || playerTwo.DrawPile.Count != 0)
        {
            throw new InvalidOperationException("Players must have empty decks before a match starts.");
        }

        var cards = await cardRepository.GetAllCardsAsync(cancellationToken);
        if (cards.Count == 0)
        {
            throw new InvalidOperationException("The cards table contains no cards.");
        }

        var definitions = cards.Select(ToDefinition).ToList();
        AddDeck(playerOne, definitions, firstInstanceId: 1);
        AddDeck(playerTwo, definitions, firstInstanceId: DeckSize + 1);
    }

    // addes deck, not random (for some time)
    private static void AddDeck(MindDuck.Rules.Models.Player player, IReadOnlyList<CardDefinition> definitions, int firstInstanceId)
    {
        for (int i = 0; i < DeckSize; i++)
        {
            var definition = definitions[i % definitions.Count];
            player.DrawPile.Add(new RulesCard(firstInstanceId + i, definition));
        }
    }

    private static CardDefinition ToDefinition(PersistenceCard card)
    {
        var keywords = card.Keyword is { } keyword
            ? new[] { ToRulesKeyword(keyword) }
            : Array.Empty<RulesKeyword>();
        var effects = card.Abilities.Select(ToEffect).ToList();

        return new CardDefinition(card.CardId, card.Name, card.Power, keywords, effects);
    }

    private static RulesKeyword ToRulesKeyword(MindDuck.Contracts.Keyword keyword)
    {
        return keyword switch
        {
            MindDuck.Contracts.Keyword.Poisonous => RulesKeyword.Poisonous,
            MindDuck.Contracts.Keyword.Tough => RulesKeyword.Tough,
            MindDuck.Contracts.Keyword.Frenzy => RulesKeyword.Frenzy,
            _ => throw new NotSupportedException($"Card keyword '{keyword}' is not supported by the rules engine.")
        };
    }

    private static ICardEffect ToEffect(CardAbility ability)
    {
        var trigger = ability.Trigger switch
        {
            AbilityTrigger.Play => CardEffectTrigger.Play,
            AbilityTrigger.Attack => CardEffectTrigger.Attack,
            AbilityTrigger.Defeated => CardEffectTrigger.Defeated,
            _ => throw new NotSupportedException($"Card ability trigger '{ability.Trigger}' is not supported.")
        };

        if (ability.Effect == AbilityEffect.GainLife)
        {
            int amount = ability.Value ?? 2;
            if (amount <= 0)
            {
                throw new InvalidOperationException("GainLife abilities require a positive value.");
            }

            return new GainLifeEffect(trigger, amount);
        }
        else
        {
            //TODO : implement other AbilityEffects
            return new GainLifeEffect(trigger, 0);
        }

        throw new NotSupportedException(
            $"Card ability effect '{ability.Effect}' is not supported by the rules engine.");
    }
}
