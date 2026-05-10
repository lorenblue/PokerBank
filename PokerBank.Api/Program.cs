using PokerBank.Api;
using PokerBank.Api.Data;
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
    await dbContext.Database.EnsureCreatedAsync();
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

app.MapCreatePlayer();

app.Run();
