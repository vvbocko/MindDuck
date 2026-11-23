# multiplayer-card-game
A University group project - 2D Unity multiplayer card game

## Running the server on Linux


```bash
read -rsp "PostgreSQL password: " DB_PASSWORD
printf '\n'
export ConnectionStrings__MindDuck="Host=localhost;Port=5433;Database=mindduck;Username=mindduck;Password=${DB_PASSWORD}"
unset DB_PASSWORD
dotnet run --project MindDuckServer/src/MindDuck.Server/MindDuck.Server.csproj
```

This requires the .NET 11 SDK and the Compose PostgreSQL service reachable at `localhost:5433`.

## Temporary players

For an existing Docker database, enable generated player IDs once:

```bash
sudo docker compose exec -T db psql -v ON_ERROR_STOP=1 -U mindduck -d mindduck \
  < db/migrations/20261122_player_identity.sql
```

Each successful room join creates a new player row. The generated ID is kept in the room for the current server process. A match row is created when the second player joins. The current game-end flow does not yet update its winner or `finished_at`.
