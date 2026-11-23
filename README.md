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

Each successful room join creates a new player row. The generated ID is kept in the room for the current server process. A match row is created when the second player joins. Successfully applied `PlayCardCommand` and `AttackCommand` actions are appended to `match_events` as JSON (`type` and `payload`). Rejected commands are not stored. The current game-end flow does not yet update the match winner or `finished_at`.

For an existing database, apply the match-participant and event-actor migration once:

```bash
sudo docker compose exec -T db psql -v ON_ERROR_STOP=1 -U mindduck -d mindduck \
  < db/migrations/20261123_match_participants_and_event_actors.sql
```

New match rows store both player IDs; event rows store the ID of the player who performed the action. Existing rows keep `NULL` for these new columns because their participants/actors cannot be inferred reliably.
