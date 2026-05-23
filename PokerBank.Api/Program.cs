using Microsoft.EntityFrameworkCore;
using PokerBank.Api;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Balances;
using PokerBank.Api.Features.GameResults;
using PokerBank.Api.Features.Games;
using PokerBank.Api.Features.Payments;
using PokerBank.Api.Features.Players;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddDatabase()
    .AddObservability();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PokerBankDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("PokerBank API")
        .DisableAgent());
}

app.UseHttpsRedirection();

app.MapListBalances();
app.MapListGameResults();
app.MapCreateGame();
app.MapListGames();
app.MapGetGame();
app.MapDeleteGame();
app.MapAddBuyIn();
app.MapAddCashOut();
app.MapDeleteGameEntry();
app.MapCloseGame();
app.MapRecordPayment();
app.MapListPayments();
app.MapGetPayment();
app.MapDeletePayment();
app.MapCreatePlayer();
app.MapListPlayers();
app.MapGetPlayer();
app.MapUpdatePlayer();
app.MapArchivePlayer();

app.Run();
