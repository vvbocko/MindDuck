using Npgsql;

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

var app = builder.Build();

// endpointy API tutaj beda
app.Run();