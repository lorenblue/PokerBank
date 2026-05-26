using Microsoft.EntityFrameworkCore;
using PokerBank.Api;
using PokerBank.Api.Auth;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Auth;
using PokerBank.Api.Features.Balances;
using PokerBank.Api.Features.GameResults;
using PokerBank.Api.Features.Games;
using PokerBank.Api.Features.Me;
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
    await DevelopmentAuthSeed.SeedAsync(scope.ServiceProvider);
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

app.UseAuthentication();
app.UseAuthorization();

app.MapLogin();
app.MapLogout();
app.MapGetCurrentUser();

var viewGroup = app.MapGroup("")
    .RequireAuthorization(AuthorizationPolicies.ViewGroup);

viewGroup.MapGetMyBalance();
viewGroup.MapListGameResults();
viewGroup.MapListGames();
viewGroup.MapGetGame();
viewGroup.MapListPayments();
viewGroup.MapGetPayment();
viewGroup.MapListPlayers();
viewGroup.MapGetPlayer();

var manageGroup = app.MapGroup("")
    .RequireAuthorization(AuthorizationPolicies.ManageGroup);

manageGroup.MapCreateGame();
manageGroup.MapDeleteGame();
manageGroup.MapAddBuyIn();
manageGroup.MapAddCashOut();
manageGroup.MapDeleteGameEntry();
manageGroup.MapCloseGame();
manageGroup.MapRecordPayment();
manageGroup.MapDeletePayment();
manageGroup.MapListBalances();
manageGroup.MapCreatePlayer();
manageGroup.MapUpdatePlayer();
manageGroup.MapArchivePlayer();
manageGroup.MapSendBalanceUpdates();

app.Run();
