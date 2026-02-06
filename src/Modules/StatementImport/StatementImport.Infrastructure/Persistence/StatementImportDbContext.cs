using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SpendBear.Infrastructure.Core.Data;
using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;

namespace StatementImport.Infrastructure.Persistence;

public class StatementImportDbContext : BaseDbContext, IStatementImportUnitOfWork
{
    public StatementImportDbContext(DbContextOptions<StatementImportDbContext> options, IDomainEventDispatcher domainEventDispatcher)
        : base(options, domainEventDispatcher)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("statement_import");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
