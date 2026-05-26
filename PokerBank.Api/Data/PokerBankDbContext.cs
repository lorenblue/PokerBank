using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PokerBank.Api.Data.Auth;
using PokerBank.Domain;

namespace PokerBank.Api.Data;

public sealed class PokerBankDbContext(DbContextOptions<PokerBankDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<PokerGroup> PokerGroups => Set<PokerGroup>();

    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();

    public DbSet<Player> Players => Set<Player>();

    public DbSet<PokerGame> Games => Set<PokerGame>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PokerGroup>(group =>
        {
            group.HasKey(g => g.Id);

            group.Property(g => g.Name)
                .HasMaxLength(PokerGroup.MaxNameLength)
                .IsRequired();

            group.Property(g => g.IsActive)
                .IsRequired();
        });

        modelBuilder.Entity<GroupMembership>(membership =>
        {
            membership.HasKey(m => new { m.UserId, m.PokerGroupId });

            membership.Property(m => m.Role)
                .HasConversion<string>()
                .IsRequired();

            membership.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            membership.HasOne<PokerGroup>()
                .WithMany()
                .HasForeignKey(m => m.PokerGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Player>(player =>
        {
            player.HasKey(p => p.Id);

            player.Property(p => p.PokerGroupId)
                .IsRequired();

            player.HasOne<PokerGroup>()
                .WithMany()
                .HasForeignKey(p => p.PokerGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            player.Property(p => p.Name)
                .HasMaxLength(Player.MaxNameLength)
                .IsRequired();

            player.Property(p => p.EmailAddress)
                .HasMaxLength(Player.MaxEmailAddressLength);

            player.Property(p => p.UserId);

            player.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            player.HasIndex(p => new { p.PokerGroupId, p.UserId })
                .IsUnique()
                .HasFilter("\"UserId\" IS NOT NULL");

            player.Property(p => p.IsActive)
                .IsRequired();
        });

        modelBuilder.Entity<PokerGame>(game =>
        {
            game.HasKey(g => g.Id);

            game.Property(g => g.PokerGroupId)
                .IsRequired();

            game.HasOne<PokerGroup>()
                .WithMany()
                .HasForeignKey(g => g.PokerGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            game.Property(g => g.CreatedAtUtc)
                .IsRequired();

            game.Property(g => g.Status)
                .HasConversion<string>()
                .IsRequired();

            game.HasMany(g => g.Entries)
                .WithOne()
                .HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade);

            game.Navigation(g => g.Entries)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            game.Ignore(g => g.TotalBuyIns);
            game.Ignore(g => g.TotalCashOuts);
        });

        modelBuilder.Entity<GameEntry>(entry =>
        {
            entry.ToTable("GameEntries");

            entry.Property<Guid>("GameId");

            entry.HasKey("GameId", nameof(GameEntry.Id));

            entry.Property(e => e.Id)
                .ValueGeneratedNever();

            entry.Property(e => e.PlayerId)
                .IsRequired();

            entry.HasOne<Player>()
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entry.ComplexProperty(e => e.Amount, amount =>
            {
                amount.Property(money => money.Amount)
                    .HasColumnName("Amount")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();
            });

            entry.Property(e => e.Type)
                .HasConversion<string>()
                .IsRequired();

            entry.Property(e => e.RecordedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<Payment>(payment =>
        {
            payment.HasKey(p => p.Id);

            payment.Property(p => p.PokerGroupId)
                .IsRequired();

            payment.HasOne<PokerGroup>()
                .WithMany()
                .HasForeignKey(p => p.PokerGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            payment.Property(p => p.PlayerId)
                .IsRequired();

            payment.HasOne<Player>()
                .WithMany()
                .HasForeignKey(p => p.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            payment.ComplexProperty(p => p.Amount, amount =>
            {
                amount.Property(money => money.Amount)
                    .HasColumnName("Amount")
                    .HasColumnType("numeric(18,2)")
                    .IsRequired();
            });

            payment.Property(p => p.Direction)
                .HasConversion<string>()
                .IsRequired();

            payment.Property(p => p.Method)
                .HasConversion<string>()
                .IsRequired();

            payment.Property(p => p.RecordedAtUtc)
                .IsRequired();
        });
    }
}
