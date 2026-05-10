using Microsoft.EntityFrameworkCore;
using PokerBank.Domain;

namespace PokerBank.Api.Data;

public sealed class PokerBankDbContext(DbContextOptions<PokerBankDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

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
    }
}
