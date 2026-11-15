namespace MindDuck.Persistence;

public interface IPlayerRepository
{
    Player GetPlayer(int id);
    void SavePlayer(Player player);
    void UpdateStats(int playerId, bool win);
}