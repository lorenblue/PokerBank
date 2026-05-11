using Microsoft.EntityFrameworkCore;
using PokerBank.Domain;

namespace PokerBank.Api.Data;

public sealed class PokerBankDbContext(DbContextOptions<PokerBankDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<PokerGame> Games => Set<PokerGame>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(player =>
        {
            player.HasKey(p => p.Id);

            player.Property(p => p.Name)
                .HasMaxLength(Player.MaxNameLength)
                .IsRequired();

            player.Property(p => p.IsActive)
                .IsRequired();
        });

        modelBuilder.Entity<PokerGame>(game =>
        {
            game.HasKey(g => g.Id);

            game.Property(g => g.CreatedAtUtc)
                .IsRequired();

            game.Property(g => g.Status)
                .HasConversion<string>()
                .IsRequired();

            game.Ignore(g => g.Entries);
            game.Ignore(g => g.TotalBuyIns);
            game.Ignore(g => g.TotalCashOuts);
        });
    }
}
