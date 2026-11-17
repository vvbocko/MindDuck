using Npgsql;
using MindDuck.Contracts;
using MindDuck.Persistence;
using MindDuck.Server.WebSockets;
using MindDuck.Server.Services;
using MindDuck.Rules;

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
builder.Services.AddTransient<GameWebSocketHandler>();

var app = builder.Build();

app.UseWebSockets();

// endpointy API tutaj beda
app.MapGet("/api/cards", async (ICardRepository repo, CancellationToken cancellationToken) =>
    await repo.GetAllCardsAsync(cancellationToken));

app.MapPost("/api/rooms", (RoomManager roomManager) =>
{
    string roomCode = roomManager.CreateRoom();
    return Results.Ok(new { RoomCode = roomCode });
});

app.Map(Protocol.WebSocketPath, async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<GameWebSocketHandler>();

        await handler.HandleAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.MapGet("/health/db", async (NpgsqlDataSource dataSource) =>
{
    await using var connection = await dataSource.OpenConnectionAsync();
    await using var command = new NpgsqlCommand("SELECT 1", connection);
    await command.ExecuteScalarAsync();

    return Results.Ok(new { database = "connected" });
});

app.Run();