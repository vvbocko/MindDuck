using System.Collections.Generic;
using MindDuck.Rules.Commands;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules;
public class MatchManager
{
    private readonly Dictionary<Guid, Match>
        _matches = new();

    public Match AddMatch(Match match)
    {
        _matches[match.MatchId] = match;
        return match;
    }

    public Match GetMatch(Guid id)
    {
        return _matches[id];
    }

    public bool RemoveMatch(Guid id)
    {
        return _matches.Remove(id);
    }
}