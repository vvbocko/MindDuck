using MindDuck.Contracts;
using MindDuck.Persistence;
using MindDuck.Rules.Effects;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using PersistenceCard = MindDuck.Persistence.Card;
using RulesCard = MindDuck.Rules.Models.Card;
using RulesKeyword = MindDuck.Rules.Models.Keyword;
using System;

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
        if (cards.Count < 20)
        {
            throw new InvalidOperationException("The cards table contains too few cards :c.");
        }

        var definitions = cards.Select(ToDefinition).ToList();
        AddDecks(playerOne, playerTwo, definitions);
    }

    private static void AddDecks(MindDuck.Rules.Models.Player player1, MindDuck.Rules.Models.Player player2, IReadOnlyList<CardDefinition> definitions)
    {
        int[] permutation = Enumerable.Range(0, definitions.Count).ToArray();
        Random.Shared.Shuffle(permutation);
        for (int i = 0; i < 2*DeckSize; i+=2)
        {
            var definition1 = definitions[permutation[i]];
            player1.DrawPile.Add(new RulesCard(i+1, definition1));
            var definition2 = definitions[permutation[i+1]];
            player2.DrawPile.Add(new RulesCard(i+2, definition2));
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
