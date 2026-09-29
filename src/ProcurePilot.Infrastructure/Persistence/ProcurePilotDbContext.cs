using Microsoft.EntityFrameworkCore;

namespace ProcurePilot.Infrastructure.Persistence;

public sealed class ProcurePilotDbContext(DbContextOptions<ProcurePilotDbContext> options)
    : DbContext(options)
{
}
