namespace MindDuck.Persistence;

public interface IMatchRepository
{
    // tworzy nowy wpis gry w bazie
    void CreateMatch(string matchId);

    // dopisuje ruchy z przebiegu gry
    void AppendEvents(string matchId, List<string> events);

    // zapisuje wynik/zwycięzcę gry
    void FinishMatch(string matchId, string winner);

    // pobiera ostatnie mecze
    List<MatchResult> GetRecent(int limit);
}