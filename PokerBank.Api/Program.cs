using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data;
using PokerBank.Api.Features.Players;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<PokerBankDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("PokerBank")
            ?? throw new InvalidOperationException("Connection string 'PokerBank' is required.")));

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
}

app.UseHttpsRedirection();

app.MapCreatePlayer();

app.Run();
