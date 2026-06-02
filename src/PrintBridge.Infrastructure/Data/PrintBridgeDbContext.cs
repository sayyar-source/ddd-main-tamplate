using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PrintBridge.Domain.Entities;
using PrintBridge.Domain.Entities.Base;
using PrintBridge.Infrastructure.Data.Configurations;

namespace PrintBridge.Infrastructure.Data;

public class PrintBridgeDbContext : DbContext
{
    private IDbContextTransaction? _transaction;

    public PrintBridgeDbContext(DbContextOptions<PrintBridgeDbContext> options) 
        : base(options)
    {
    }

    #region DbSets
   
    public DbSet<Account> Accounts { get; set; }
    #endregion

    #region DbContext Configuration
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AccountConfiguration());


        ApplyGlobalQueryFilters(modelBuilder);
    }
    private static void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Exclude soft-deleted records from all queries by default
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var propertyMethodCall = System.Linq.Expressions.Expression.Call(
                    typeof(EF), "Property",
                    new[] { typeof(bool) },
                    parameter,
                    System.Linq.Expressions.Expression.Constant("IsDeleted"));

                var falseLiteral = System.Linq.Expressions.Expression.Constant(false);
                var binaryExpression = System.Linq.Expressions.Expression.Equal(propertyMethodCall, falseLiteral);
                var lambda = System.Linq.Expressions.Expression.Lambda(binaryExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }


    #endregion

    #region Transaction Management
    public async Task BeginTransactionAsync()
    {
        _transaction = await Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            await _transaction?.CommitAsync()!;
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
            _transaction = null;
        }
    }
    #endregion

    #region Timestamp Management
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
    #endregion
}