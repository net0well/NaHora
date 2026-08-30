using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NaHora.Domain.Abstractions;
using NaHora.Domain.Adjustments;
using NaHora.Domain.Attendance;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Scheduling;
using NaHora.Domain.Users;
using Wolverine.EntityFrameworkCore;

namespace NaHora.Infrastructure.Persistence;

/// <summary>
/// Application database context. Entity configuration lives in
/// <see cref="IEntityTypeConfiguration{TEntity}"/> implementations, not inline.
/// </summary>
public sealed class NaHoraDbContext(
    DbContextOptions<NaHoraDbContext> options,
    ICurrentUser currentUser) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Punch> Punches => Set<Punch>();

    /// <summary>
    /// Corrections to time records.
    /// </summary>
    /// <remarks>
    /// Never modifies <see cref="Punches"/>. A treatment points at a punch and
    /// says which time should count; the punch itself is append-only, enforced
    /// by a database trigger rather than by convention.
    /// </remarks>
    public DbSet<PunchAdjustment> PunchAdjustments => Set<PunchAdjustment>();

    /// <summary>The hours each company works by default. One row per company.</summary>
    public DbSet<CompanyWorkSchedule> CompanyWorkSchedules => Set<CompanyWorkSchedule>();

    /// <summary>
    /// Per-employee hours, versioned by effective period.
    /// </summary>
    /// <remarks>
    /// Several rows per employee is normal: changing somebody's hours closes
    /// the current period and opens another, so a day is always judged against
    /// what was in force then.
    /// </remarks>
    public DbSet<EmployeeWorkSchedule> EmployeeWorkSchedules => Set<EmployeeWorkSchedule>();

    /// <summary>
    /// The fixed catalogue of reasons a punch may be corrected for.
    /// </summary>
    /// <remarks>
    /// Not tenant-filtered: the catalogue is the product's, shared by every
    /// company, and seeded in a migration rather than written at runtime.
    /// </remarks>
    public DbSet<AdjustmentJustification> AdjustmentJustifications => Set<AdjustmentJustification>();

    /// <summary>
    /// NSR counters. Not tenant-filtered: the counter is read by primary key
    /// under a lock inside the punch transaction, and a global filter would add
    /// a predicate to a query whose whole purpose is to lock one known row.
    /// </summary>
    public DbSet<CompanyNsrCounter> NsrCounters => Set<CompanyNsrCounter>();

    /// <summary>
    /// Read by the global query filter on every query. EF re-evaluates it per
    /// execution rather than baking the value into a cached plan.
    /// </summary>
    internal Guid CurrentCompanyId => currentUser.CompanyId;

    /// <summary>
    /// Disables tenant filtering for unauthenticated work — migrations, the
    /// seeder, background jobs that legitimately span tenants.
    /// </summary>
    internal bool TenantFilterEnabled => currentUser.IsAuthenticated;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NaHoraDbContext).Assembly);

        // Wolverine's envelope tables. They live in this context on purpose:
        // sharing the transaction is what makes the outbox atomic with the
        // business change that produced the message.
        modelBuilder.MapWolverineEnvelopeStorage();

        ApplyTenantFilters(modelBuilder);
        ApplySnakeCaseNaming(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Constrains every <see cref="ITenantOwned"/> entity to the caller's
    /// company.
    /// </summary>
    /// <remarks>
    /// This is the first of two layers. It catches the query somebody forgot to
    /// filter; handlers still check ownership explicitly, because a filter
    /// disabled by accident must not become a cross-tenant leak.
    ///
    /// Applied by reflection so a new entity is covered the moment it
    /// implements the interface, with nothing to remember.
    /// </remarks>
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (!typeof(ITenantOwned).IsAssignableFrom(clrType))
            {
                continue;
            }

            // entity => !TenantFilterEnabled || entity.CompanyId == CurrentCompanyId
            var parameter = Expression.Parameter(clrType, "entity");
            var companyIdProperty = Expression.Property(parameter, nameof(ITenantOwned.CompanyId));

            var contextConstant = Expression.Constant(this);
            var filterEnabled = Expression.Property(contextConstant, nameof(TenantFilterEnabled));
            var currentCompanyId = Expression.Property(contextConstant, nameof(CurrentCompanyId));

            var body = Expression.OrElse(
                Expression.Not(filterEnabled),
                Expression.Equal(companyIdProperty, currentCompanyId));

            modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    /// <summary>
    /// PostgreSQL folds unquoted identifiers to lower case, so PascalCase names
    /// would force quoting in every hand-written query and migration.
    /// </summary>
    private static void ApplySnakeCaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null)
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (keyName is not null)
                {
                    key.SetName(ToSnakeCase(keyName));
                }
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var constraintName = foreignKey.GetConstraintName();
                if (constraintName is not null)
                {
                    foreignKey.SetConstraintName(ToSnakeCase(constraintName));
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (indexName is not null)
                {
                    index.SetDatabaseName(ToSnakeCase(indexName));
                }
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var builder = new System.Text.StringBuilder(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];

            if (char.IsUpper(current) && index > 0 && value[index - 1] != '_')
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
