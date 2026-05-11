using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PokerBank.Api.Data;

public sealed class PokerBankDbContextFactory : IDesignTimeDbContextFactory<PokerBankDbContext>
{
    public PokerBankDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PokerBankDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=54329;Database=pokerbank;Username=pokerbank;Password=pokerbank");

        return new PokerBankDbContext(optionsBuilder.Options);
    }
}
