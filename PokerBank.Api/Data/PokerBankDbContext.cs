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

            game.OwnsMany(g => g.Entries, entry =>
            {
                entry.ToTable("GameEntries");

                entry.WithOwner()
                    .HasForeignKey("GameId");

                entry.HasKey("GameId", nameof(GameEntry.Id));

                entry.Property(e => e.Id)
                    .ValueGeneratedNever();

                entry.Property(e => e.PlayerId)
                    .IsRequired();

                entry.Property(e => e.Amount)
                    .HasConversion(
                        money => money.Amount,
                        amount => new Money(amount))
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();

                entry.Property(e => e.Type)
                    .HasConversion<string>()
                    .IsRequired();

                entry.Property(e => e.RecordedAtUtc)
                    .IsRequired();
            });

            game.Navigation(g => g.Entries)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            game.Ignore(g => g.TotalBuyIns);
            game.Ignore(g => g.TotalCashOuts);
        });
    }
}
