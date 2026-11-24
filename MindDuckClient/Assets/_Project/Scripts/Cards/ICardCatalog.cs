using System.Collections.Generic;
using MindDuck.Contracts;

namespace MindDuck.Client.Cards
{
    public interface ICardCatalog
    {
        IReadOnlyCollection<CardDefinitionDto> All { get; }

        bool TryGet(string definitionId, out CardDefinitionDto card);
    }
}