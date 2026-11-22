using MindDuck.Contracts;
using MindDuck.Rules.Commands;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using ContractGamePhase = MindDuck.Contracts.GamePhase;
using RulesGamePhase = MindDuck.Rules.Models.GamePhase;

namespace MindDuck.Server.WebSockets
{
    public sealed class GameStateViewBuilder
    {
        private readonly IGameEngine _gameEngine;

        public GameStateViewBuilder(IGameEngine gameEngine)
        {
            _gameEngine = gameEngine;
        }

    public GameStateView Build(Guid matchId, GameState state, Player player, PlayerSeat playerSeat)
        {
            Player opponent = state.GetOpponent(player);
            PlayerSeat activeSeat;
            if (state.ActivePlayerId == state.Players[0].Id)
            {
                activeSeat = PlayerSeat.P1;
            }
            else
            {
                activeSeat = PlayerSeat.P2;
            }

            ContractGamePhase contractPhase = MapPhase(state.Phase);

            PlayerSeat? waitingForSeat;
            if (contractPhase == ContractGamePhase.GameOver)
            {
                waitingForSeat = null;
            }
            else
            {
                waitingForSeat = activeSeat;
            } 

            var legalActions = _gameEngine.GetLegalActions(state, player);

            List<int> playableCardIds = new List<int>();
            List<int> attackerCardIds = new List<int>();

            foreach (var action in legalActions)
            {
                if (action is PlayCardCommand playCommand)
                {
                    playableCardIds.Add(playCommand.CardInstanceId);
                }
                else if (action is AttackCommand attackCommand)
                {
                    attackerCardIds.Add(attackCommand.AttackerInstanceId);
                }
            }

            return new GameStateView
            {
                MatchId = matchId.ToString(),
                YourSeat = playerSeat,
                ActiveSeat = activeSeat,
                Phase = contractPhase,
                WaitingForSeat = waitingForSeat,
                TurnNumber = state.Turn,
                You = MapPlayerView(player, includeHandCards: true),
                Opponent = MapOpponentView(opponent),
                LegalActions = new LegalActionsDto
                {
                    PlayableCardIds = playableCardIds,
                    AttackerIds = attackerCardIds
                }
            };
        }

        private static ContractGamePhase MapPhase(RulesGamePhase phase)
        {
            switch (phase)
            {
                case RulesGamePhase.AwaitingDecision:
                    return ContractGamePhase.AwaitingDecision;

                case RulesGamePhase.Finished:
                    return ContractGamePhase.GameOver;

                default:
                    return ContractGamePhase.AwaitingAction;
            }
        }

        private static PlayerView MapPlayerView(Player player, bool includeHandCards)
        {
            List<CardRefDto> handCards = new List<CardRefDto>();
            if (includeHandCards)
            {
                foreach (var card in player.Hand.Cards)
                {
                    CardRefDto cardDto = new CardRefDto();
                    cardDto.InstanceId = card.InstanceId;
                    cardDto.DefinitionId = card.Definition.Id;
                    handCards.Add(cardDto);
                }
            }

            List<CardRefDto> discardCards = new List<CardRefDto>();
            foreach (var card in player.DiscardPile.Cards)
            {
                CardRefDto cardDto = new CardRefDto();
                cardDto.InstanceId = card.InstanceId;
                cardDto.DefinitionId = card.Definition.Id;
                discardCards.Add(cardDto);
            }

            List<CreatureDto> boardCreatures = new List<CreatureDto>();
            foreach (var card in player.Board.Cards)
            {
                CreatureDto creatureDto = new CreatureDto();
                creatureDto.InstanceId = card.InstanceId;
                creatureDto.DefinitionId = card.Definition.Id;
                creatureDto.IsExhausted = card.ToughUsed;
                boardCreatures.Add(creatureDto);
            }

            PlayerView view = new PlayerView();
            view.Nickname = player.Name;
            view.Life = player.Life;
            view.MindbugsLeft = player.MindDucksLeft;
            view.Hand = handCards;
            view.DrawPileCount = player.DrawPile.Count;
            view.Discard = discardCards;
            view.Board = boardCreatures;

            return view;
        }

        private static OpponentView MapOpponentView(Player opponent)
        {
            List<CardRefDto> discardCards = new List<CardRefDto>();
            foreach (var card in opponent.DiscardPile.Cards)
            {
                CardRefDto cardDto = new CardRefDto();
                cardDto.InstanceId = card.InstanceId;
                cardDto.DefinitionId = card.Definition.Id;
                discardCards.Add(cardDto);
            }

            List<CreatureDto> boardCreatures = new List<CreatureDto>();
            foreach (var card in opponent.Board.Cards)
            {
                CreatureDto creatureDto = new CreatureDto();
                creatureDto.InstanceId = card.InstanceId;
                creatureDto.DefinitionId = card.Definition.Id;
                creatureDto.IsExhausted = card.ToughUsed;
                boardCreatures.Add(creatureDto);
            }

            OpponentView view = new OpponentView();
            view.Nickname = opponent.Name;
            view.Life = opponent.Life;
            view.MindbugsLeft = opponent.MindDucksLeft;
            view.HandCount = opponent.Hand.Count;
            view.DrawPileCount = opponent.DrawPile.Count;
            view.Discard = discardCards;
            view.Board = boardCreatures;

            return view;
        }
    }
}