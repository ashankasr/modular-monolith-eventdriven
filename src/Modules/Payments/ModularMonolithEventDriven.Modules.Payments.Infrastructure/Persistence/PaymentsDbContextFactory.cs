using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Integrated Security=false;User ID=sa;Password=Donkey@1;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "payments"));

        return new PaymentsDbContext(optionsBuilder.Options);
    }
}
