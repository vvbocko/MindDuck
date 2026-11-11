# multiplayer-card-game
A University group project - 2D Unity multiplayer card game

## Running the server on Linux


```bash
read -rsp "PostgreSQL password: " DB_PASSWORD
printf '\n'
export ConnectionStrings__MindDuck="Host=localhost;Port=5432;Database=mindduck;Username=postgres;Password=${DB_PASSWORD}"
unset DB_PASSWORD
dotnet run --project MindDuckServer/src/MindDuck.Server/MindDuck.Server.csproj
```

This requires the .NET 8 SDK and a PostgreSQL server reachable at `localhost:5432`.
