using Npgsql;
using MindDuck.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("MindDuck")
    ?? throw new InvalidOperationException(
        "Nie skonfigurowano connection stringa 'MindDuck'.");

builder.Services.AddSingleton(
    _ => NpgsqlDataSource.Create(connectionString));

builder.Services.AddScoped<ICardRepository, PostgreSqlCardRepository>();
builder.Services.AddScoped<IMatchRepository, PostgreSqlMatchRepository>();
builder.Services.AddScoped<IPlayerRepository, PostgreSqlPlayerRepository>();

builder.Services.AddSingleton<RoomManager>();
builder.Services.AddSingleton<MatchManager>();

var app = builder.Build();

// endpointy API tutaj beda
app.MapGet("/api/cards", (ICardRepository repo) =>
    {
        return repo.GetAllCards();
    });

app.MapPost("/api/rooms", (RoomManager roomManager) =>
{
    string roomCode = roomManager.CreateRoom();
    return Results.Ok(new { RoomCode = roomCode });
});

app.MapGet("/health/db", async (NpgsqlDataSource dataSource) =>
{
    await using var connection = await dataSource.OpenConnectionAsync();
    await using var command = new NpgsqlCommand("SELECT 1", connection);
    await command.ExecuteScalarAsync();

    return Results.Ok(new { database = "connected" });
});

app.Run();