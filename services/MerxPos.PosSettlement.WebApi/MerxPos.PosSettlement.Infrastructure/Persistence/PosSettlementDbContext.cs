using Microsoft.EntityFrameworkCore;
using MerxPos.PosSettlement.Domain.Entities;

namespace MerxPos.PosSettlement.Infrastructure.Persistence;

public class SettlementDbContext : DbContext
{
    public SettlementDbContext(DbContextOptions<SettlementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Settlement> Settlements => Set<Settlement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SettlementDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}