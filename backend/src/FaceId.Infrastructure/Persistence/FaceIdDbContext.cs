using FaceId.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FaceId.Infrastructure.Persistence;

/// <summary>
/// Application database context. Entity configuration lives in
/// <c>IEntityTypeConfiguration</c> implementations, not inline here.
/// </summary>
public sealed class FaceIdDbContext(DbContextOptions<FaceIdDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FaceIdDbContext).Assembly);

        ApplySnakeCaseNaming(modelBuilder);

        base.OnModelCreating(modelBuilder);
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

/// <summary>
/// Marker kept next to the context so the tenant filter has a single place to
/// discover the entities it must constrain.
/// </summary>
internal static class TenantOwnedEntities
{
    public static bool IsTenantOwned(Type entityType) => typeof(ITenantOwned).IsAssignableFrom(entityType);
}
