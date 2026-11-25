using MindDuck.Rules.Commands;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;
using MindDuck.Rules.Effects;
using System.Data;
using System.Threading;

namespace MindDuck.Rules;

public sealed class GameEngine : IGameEngine
{
    private int _nextDecisionId;

    public GameState StartNewGame(GameSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        Player playerOne = setup.PlayerOne;
        Player playerTwo = setup.PlayerTwo;

        if (playerOne.Id == playerTwo.Id)
            throw new ArgumentException("Players must have different IDs.");

        if (setup.StartingLife <= 0)
            throw new ArgumentException("Starting life must be greater than zero.");

        if (setup.StartingHandSize < 0)
            throw new ArgumentException("Starting hand size cannot be negative.");

        if (playerOne.DrawPile.Count < setup.StartingHandSize || playerTwo.DrawPile.Count < setup.StartingHandSize)
            throw new InvalidOperationException("Not enough cards in a player's deck.");

        playerOne.Life = setup.StartingLife;
        playerTwo.Life = setup.StartingLife;

        playerOne.MindDucksLeft = 2;
        playerTwo.MindDucksLeft = 2;

        playerOne.DrawPile.Shuffle();
        playerTwo.DrawPile.Shuffle();

        playerOne.DrawCards(setup.StartingHandSize);
        playerTwo.DrawCards(setup.StartingHandSize);

        var state = new GameState(playerOne, playerTwo);

        state.SetActivePlayer(playerOne);
        state.Phase = GamePhase.Main;
        state.PendingDecision = null;
        state.FrenzyContinuationCardInstanceId = null;

        return state;
    }

    public ApplyResult Apply(GameState state, Player player, GameCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(command);

        Player? currentPlayer = FindPlayer(state, player);

        if (currentPlayer is null) 
            return ApplyResult.Failure(ApplyErrorCode.InvalidPlayer, $"Player {player.Id} does not belong to this game.");

        if (state.Phase == GamePhase.Finished)
            return ApplyResult.Failure(ApplyErrorCode.InvalidGamePhase, "Game has already finished.");

        if (state.Phase == GamePhase.AwaitingDecision)
        {
            if (command is SubmitDecisionCommand decision)
                return ApplyDecision(state, currentPlayer, decision);

            return ApplyResult.Failure(ApplyErrorCode.IllegalAction, "A decision is waiting for a response.");
        }

        if (state.ActivePlayerId != player.Id)
            return ApplyResult.Failure(ApplyErrorCode.NotPlayersTurn, $"It is not {currentPlayer.Name}'s turn.");

        if (command is SubmitDecisionCommand) 
            return ApplyResult.Failure(ApplyErrorCode.MissingDecision, "There is no pending decision.");

        if (state.FrenzyContinuationCardInstanceId is int frenzyId)
        {
            return command switch
            {
                AttackCommand attack when attack.AttackerInstanceId == frenzyId => ApplyAttack(state, currentPlayer, attack),
                EndTurnCommand => SkipFrenzyAttack(state, currentPlayer),
                _ => ApplyResult.Failure(ApplyErrorCode.IllegalAction, "You must perform the additional Frenzy attack or end your turn.")
            };
        }

        return command switch
        {
            PlayCardCommand play => ApplyPlayCard(state, currentPlayer, play),
            AttackCommand attack => ApplyAttack(state, currentPlayer, attack),
            _ => ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Unsupported command.")
        };
    }

    private ApplyResult ApplyPlayCard(GameState state, Player player, PlayCardCommand command)
    {
        Card? card = player.Hand.FindByInstanceId(command.CardInstanceId);

        if (card is null)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Card is not in player's hand.");

        player.Hand.Remove(card);
        player.DrawToFive();

        Player opponent = state.GetOpponent(player);

        if (opponent.MindDucksLeft > 0)
        {
            state.PendingDecision = new PendingDecision(Interlocked.Increment(ref _nextDecisionId), DecisionType.MindDuck, player, opponent, card);
            state.Phase = GamePhase.AwaitingDecision;

            return ApplyResult.Success(state);
        }

        PutIntoPlay(state, player, card);

        if (CheckGameOver(state))
            return ApplyResult.Success(state);

        EndTurn(state, player);

        return ApplyResult.Success(state);
    }

    private static ApplyResult ApplyDecision(GameState state, Player player, SubmitDecisionCommand command)
    {
        PendingDecision? pending = state.PendingDecision;

        if (pending is null)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "There is no pending decision.");

        if (player.Id != pending.RespondingPlayer.Id)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "You cannot respond to this decision.");

        if (command.DecisionId != pending.DecisionId || command.DecisionType != pending.Type)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Invalid decision.");

        return pending.Type switch
        {
            DecisionType.MindDuck => ResolveMindDuck(state, player, command, pending),
            DecisionType.Block => ResolveBlock(state, player, command, pending),
            _ => ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Unsupported decision type.")
        };
    }

    private static ApplyResult ResolveMindDuck(GameState state, Player player, SubmitDecisionCommand command, PendingDecision pending)
    {
        if (command.Accepted)
        {
            if (player.MindDucksLeft <= 0)
                return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "No MindDucks remaining.");

            player.MindDucksLeft--;
            PutIntoPlay(state, player, pending.Card);

            state.PendingDecision = null;

            if (CheckGameOver(state))
                return ApplyResult.Success(state);

            BeginTurn(state, pending.RequestingPlayer, player);

            return ApplyResult.Success(state);
        }

        PutIntoPlay(state, pending.RequestingPlayer, pending.Card);

        state.PendingDecision = null;

        if (CheckGameOver(state))
                return ApplyResult.Success(state);

        EndTurn(state, pending.RequestingPlayer);

        return ApplyResult.Success(state);
    }

    private ApplyResult ApplyAttack(GameState state, Player player, AttackCommand command)
    {
        Card? attacker = player.Board.FindByInstanceId(command.AttackerInstanceId);

        if (attacker is null)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Attacking creature not found.");

        if (!attacker.CanAttack(state.Turn))
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "This creature cannot attack.");

        if (state.FrenzyContinuationCardInstanceId is int frenzyId && attacker.InstanceId != frenzyId)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Only the Frenzy creature can attack again.");

        Player opponent = state.GetOpponent(player);
        int? forcedDefenderId = null;

        if (command.TargetCreatureInstanceId.HasValue)
        {
            if (!attacker.HasKeyword(Keyword.Hunter))
                return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Only Hunter creatures can select a forced target.");

            Card? target = opponent.Board.FindByInstanceId(command.TargetCreatureInstanceId.Value);

            if (target is null)
                return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Hunter target not found.");

            forcedDefenderId = target.InstanceId;
        }

        attacker.RegisterAttack(state.Turn);

        CardEffectResolver.Resolve(state, player, attacker, CardEffectTrigger.Attack);

        if (CheckGameOver(state))
            return ApplyResult.Success(state);

        if (player.Board.FindByInstanceId(attacker.InstanceId) is null)
        {
            CompleteAttack(state, player, attacker);
            return ApplyResult.Success(state);
        }

        List<Card> possibleBlockers = GetEligibleBlockers(attacker, opponent, forcedDefenderId).ToList();

        if (possibleBlockers.Count == 0)
        {
            opponent.Life--;

            if (CheckGameOver(state))
                return ApplyResult.Success(state);

            CompleteAttack(state, player, attacker);
            return ApplyResult.Success(state);
        }

        state.PendingDecision = new PendingDecision(Interlocked.Increment(ref _nextDecisionId), DecisionType.Block, player, opponent, attacker, forcedDefenderId);
        state.Phase = GamePhase.AwaitingDecision;

        return ApplyResult.Success(state);
    }

    private static ApplyResult ResolveBlock(GameState state, Player player, SubmitDecisionCommand command, PendingDecision pending)
    {
        Card attacker = pending.Card;

        if (!command.Accepted)
        {
            if (pending.ForcedDefenderCardId.HasValue)
                return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Hunter forces this creature to block.");

            player.Life--;

            state.PendingDecision = null;
            state.Phase = GamePhase.Main;

            if (CheckGameOver(state))
                return ApplyResult.Success(state);

            CompleteAttack(state, pending.RequestingPlayer, attacker);
            return ApplyResult.Success(state);
        }

        if (command.DefenderCardId is null)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Defender card ID is required.");

        Card? defender = player.Board.FindByInstanceId(command.DefenderCardId.Value);

        if (defender is null)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Defending creature not found.");

        bool canBlock = GetEligibleBlockers(attacker, player, pending.ForcedDefenderCardId).Any(card => card.InstanceId == defender.InstanceId);

        if (!canBlock)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "This creature cannot block the attack.");

        ResolveCombat(state, pending.RequestingPlayer, player, attacker, defender);

        state.PendingDecision = null;
        state.Phase = GamePhase.Main;

        if (CheckGameOver(state))
            return ApplyResult.Success(state);

        CompleteAttack(state, pending.RequestingPlayer, attacker);

        return ApplyResult.Success(state);
    }

    private static IEnumerable<Card> GetEligibleBlockers(Card attacker, Player defender, int? forcedDefenderId)
    {
        if (forcedDefenderId.HasValue)
        {
            Card? forcedCard = defender.Board.FindByInstanceId(forcedDefenderId.Value);
            return forcedCard is null ? Array.Empty<Card>() : new[] { forcedCard };
        }

        if (attacker.HasKeyword(Keyword.Sneaky))
            return defender.Board.Cards.Where(card => card.HasKeyword(Keyword.Sneaky));

        return defender.Board.Cards;
    }

    private static void PutIntoPlay(GameState state, Player player, Card card)
    {
        card.PrepareForPlay(state.Turn);
        player.Board.Add(card);

        CardEffectResolver.Resolve(state, player, card, CardEffectTrigger.Play);
    }

    private static void ResolveCombat(GameState state, Player attackerPlayer, Player defenderPlayer, Card attacker, Card defender)
    {
        CombatResult result = CombatRules.Resolve(attacker, defender);

        if (result.AttackerDefeated)
        {
            CardEffectResolver.Resolve(state, attackerPlayer, attacker, CardEffectTrigger.Defeated);

            attackerPlayer.Board.Remove(attacker);
            attackerPlayer.DiscardPile.Add(attacker);
        }

        if (result.DefenderDefeated)
        {       
            CardEffectResolver.Resolve(state, defenderPlayer, defender, CardEffectTrigger.Defeated);

            defenderPlayer.Board.Remove(defender);
            defenderPlayer.DiscardPile.Add(defender);
        }
    }

    private static void CompleteAttack(GameState state, Player attackerPlayer, Card attacker)
    {
        state.PendingDecision = null;
        state.Phase = GamePhase.Main;

        bool canAttackAgain = attackerPlayer.Board.FindByInstanceId(attacker.InstanceId) is not null && attacker.HasKeyword(Keyword.Frenzy) && attacker.CanAttack(state.Turn);

        if (canAttackAgain)
        {
            state.FrenzyContinuationCardInstanceId = attacker.InstanceId;
            return;
        }

        state.FrenzyContinuationCardInstanceId = null;
        EndTurn(state, attackerPlayer);
    }

    private static ApplyResult SkipFrenzyAttack(GameState state, Player player)
    {
        if (state.FrenzyContinuationCardInstanceId is null)
            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "There is no additional Frenzy attack.");

        state.FrenzyContinuationCardInstanceId = null;
        EndTurn(state, player);

        return ApplyResult.Success(state);
    }

    private static bool CheckGameOver(GameState state)
    {
        Player playerOne = state.Players[0];
        Player playerTwo = state.Players[1];

        if (playerOne.Life <= 0 && playerTwo.Life <= 0)
        {
            state.Finish(null);
            return true;
        }

        if (playerOne.Life <= 0)
        {
            state.Finish(playerTwo);
            return true;
        }

        if (playerTwo.Life <= 0)
        {
            state.Finish(playerOne);
            return true;
        }

        return false;
    }

    private static bool CanPlayerAct(GameState state, Player player)
    {
        bool canPlayCard = player.Hand.Count > 0;
        bool canAttack = player.Board.Cards.Any(card => card.CanAttack(state.Turn));

        return canPlayCard || canAttack;
    }

    private static void EndTurn(GameState state, Player currentPlayer)
    {
        Player nextPlayer = state.GetOpponent(currentPlayer);
        BeginTurn(state, nextPlayer, currentPlayer);
    }

    private static void BeginTurn(GameState state, Player nextPlayer, Player previousPlayer)
    {
        state.AdvanceTurn();
        state.SetActivePlayer(nextPlayer);
        state.Phase = GamePhase.Main;
        state.PendingDecision = null;
        state.FrenzyContinuationCardInstanceId = null;

        foreach (Card card in nextPlayer.Board.Cards)
            card.StartTurn();

        if (!CanPlayerAct(state, nextPlayer))
            state.Finish(previousPlayer);
    }

    public IReadOnlyList<GameCommand> GetLegalActions(GameState state, Player player)
    {
        Player? currentPlayer = FindPlayer(state, player);

        if (currentPlayer is null || state.Phase == GamePhase.Finished)
            return Array.Empty<GameCommand>();

        if (state.Phase == GamePhase.AwaitingDecision)
            return GetDecisionActions(state, currentPlayer);

        if (state.Phase != GamePhase.Main || state.ActivePlayerId != currentPlayer.Id)
            return Array.Empty<GameCommand>();

        List<GameCommand> actions = new();

        if (state.FrenzyContinuationCardInstanceId is int frenzyId)
        {
            Card? frenzyCard = currentPlayer.Board.FindByInstanceId(frenzyId);

            if (frenzyCard is not null && frenzyCard.CanAttack(state.Turn))
                AddAttackActions(actions, frenzyCard, state.GetOpponent(currentPlayer));

            actions.Add(new EndTurnCommand());

            return actions;
        }

        foreach (Card card in currentPlayer.Hand.Cards)
            actions.Add(new PlayCardCommand(card.InstanceId));

        foreach (Card card in currentPlayer.Board.Cards)
        {
            if (card.CanAttack(state.Turn))
                AddAttackActions(actions, card, state.GetOpponent(currentPlayer));
        }

        return actions;
    }

    private static void AddAttackActions(List<GameCommand> actions, Card attacker, Player opponent)
    {
        actions.Add(new AttackCommand(attacker.InstanceId));

        if (attacker.HasKeyword(Keyword.Hunter))
        {
            foreach (Card enemyCard in opponent.Board.Cards)
                actions.Add(new AttackCommand(attacker.InstanceId, enemyCard.InstanceId));
        }
    }

    private static IReadOnlyList<GameCommand> GetDecisionActions(GameState state, Player player)
    {
        PendingDecision? pending = state.PendingDecision;

        if (pending is null || pending.RespondingPlayer.Id != player.Id)
            return Array.Empty<GameCommand>();

        List<GameCommand> actions = new();

        if (pending.Type == DecisionType.MindDuck)
        {
            actions.Add(new SubmitDecisionCommand(pending.DecisionId, DecisionType.MindDuck, false));

            if (player.MindDucksLeft > 0)
                actions.Add(new SubmitDecisionCommand(pending.DecisionId, DecisionType.MindDuck, true));
        }
        else if (pending.Type == DecisionType.Block)
        {
            if (pending.ForcedDefenderCardId.HasValue)
            {
                actions.Add(new SubmitDecisionCommand(pending.DecisionId, DecisionType.Block, true, pending.ForcedDefenderCardId.Value));
            }
            else
            {
                actions.Add(new SubmitDecisionCommand(pending.DecisionId, DecisionType.Block, false));

                foreach (Card card in GetEligibleBlockers(pending.Card, player, null))
                    actions.Add(new SubmitDecisionCommand(pending.DecisionId, DecisionType.Block, true, card.InstanceId));
            }
        }

        return actions;
    }

    private static Player? FindPlayer(GameState state, Player player)
    {
        return state.Players.FirstOrDefault(p => p.Id == player.Id);
    }
}