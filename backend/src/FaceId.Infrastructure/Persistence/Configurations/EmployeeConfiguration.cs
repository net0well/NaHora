using FaceId.Domain.Companies;
using FaceId.Domain.Employees;
using FaceId.Domain.Users;
using FaceId.Infrastructure.Persistence.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaceId.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employees");

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.CompanyId).IsRequired();
        builder.Property(employee => employee.UserId).IsRequired();

        builder.Property(employee => employee.Cpf)
            .HasConversion<CpfConverter>()
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(employee => employee.Pis)
            .HasConversion<NullablePisConverter>()
            .HasMaxLength(11);

        builder.Property(employee => employee.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(employee => employee.Registration)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(employee => employee.Position)
            .HasMaxLength(100);

        builder.Property(employee => employee.HiredAt).IsRequired();

        builder.Property(employee => employee.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(employee => employee.CreatedAt).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(employee => employee.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(employee => employee.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(employee => new { employee.CompanyId, employee.Cpf })
            .IsUnique()
            .HasDatabaseName("ix_employees_company_cpf");

        builder.HasIndex(employee => employee.UserId)
            .IsUnique()
            .HasDatabaseName("ix_employees_user");

        // Partial index: a registration number is unique only among employees
        // still on the payroll, so reusing the number of someone who left stays
        // legitimate.
        builder.HasIndex(employee => new { employee.CompanyId, employee.Registration })
            .IsUnique()
            .HasFilter("status <> 'Terminated'")
            .HasDatabaseName("ix_employees_company_registration_active");

        builder.HasIndex(employee => new { employee.CompanyId, employee.Status })
            .HasDatabaseName("ix_employees_company_status");
    }
}
