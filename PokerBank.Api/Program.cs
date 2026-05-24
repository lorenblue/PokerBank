using Microsoft.EntityFrameworkCore;
using PokerBank.Api;
using PokerBank.Api.Auth;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Auth;
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
app.MapListBalances();
app.MapListGameResults();
app.MapListGames();
app.MapGetGame();
app.MapListPayments();
app.MapGetPayment();
app.MapListPlayers();
app.MapGetPlayer();

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
manageGroup.MapCreatePlayer();
manageGroup.MapUpdatePlayer();
manageGroup.MapArchivePlayer();

app.Run();
