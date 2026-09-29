using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProcurePilot.Infrastructure.Persistence;

public sealed class ProcurePilotDbContextFactory : IDesignTimeDbContextFactory<ProcurePilotDbContext>
{
    public ProcurePilotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__procurepilot")
            ?? "Host=localhost;Port=5432;Database=procurepilot;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<ProcurePilotDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ProcurePilotDbContext).Assembly.FullName))
            .Options;

        return new ProcurePilotDbContext(options, new NullTenantContext());
    }
}
