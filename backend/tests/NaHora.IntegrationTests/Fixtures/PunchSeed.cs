using NaHora.Domain.Employees;
using NaHora.Domain.ValueObjects;
using NaHora.Infrastructure.Persistence;

namespace NaHora.IntegrationTests.Fixtures;

/// <summary>
/// Gives a seeded account the employment record that punching requires.
/// </summary>
/// <remarks>
/// Separate from <see cref="AuthenticationSeed"/> because not every account is
/// an employee: an administrator who never punches is a real case, and the
/// tests that cover it need a seed without this.
/// </remarks>
internal static class PunchSeed
{
    public static async Task<Guid> HireAsync(
        NaHoraDbContext dbContext,
        SeededAccount account,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        var employee = Employee.Hire(
            account.CompanyId,
            account.UserId,
            Cpf.Create(account.Cpf).Value,

            // No PIS. Optional by design: it is required for the AFD export and
            // must surface there as a warning, never block a hire or a punch.
            pis: null,
            "Maria Souza",
            $"REG-{Guid.CreateVersion7().ToString("N")[..6]}",
            "Operadora",
            DateOnly.FromDateTime(now.AddYears(-1).DateTime),
            now).Value;

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
