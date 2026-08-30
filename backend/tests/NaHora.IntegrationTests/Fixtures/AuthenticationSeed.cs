using NaHora.Domain.Companies;
using NaHora.Domain.Users;
using NaHora.Domain.ValueObjects;
using NaHora.Infrastructure.Persistence;

namespace NaHora.IntegrationTests.Fixtures;

/// <summary>
/// A company and a user ready to sign in.
/// </summary>
internal static class AuthenticationSeed
{
    public const string Password = "Tr0vao-Azul!2026";

    public static async Task<SeededAccount> CreateAsync(
        NaHoraDbContext dbContext,
        IPasswordHasher passwordHasher,
        DateTimeOffset now,
        UserRole role = UserRole.Administrator,
        CancellationToken cancellationToken = default)
    {
        var cpf = DocumentFactory.NewCpf();
        var suffix = DocumentFactory.NewShortSuffix();

        var company = Company.Register(
            Cnpj.Create(DocumentFactory.NewCnpj()).Value,
            "Acme Industria Ltda",
            "Acme",
            EmailAddress.Create($"contato-{suffix}@acme.com.br").Value,
            AddressFactory.New(),
            Company.DefaultTimeZoneId,
            now).Value;

        company.ConfirmEmail(now);

        var user = User.Create(
            company.Id,
            Domain.ValueObjects.Cpf.Create(cpf).Value,
            "Maria Souza",
            EmailAddress.Create($"maria.souza-{suffix}@acme.com.br").Value,
            passwordHasher.Hash(Password),
            role,
            mustChangePassword: false,
            now).Value;

        user.Activate();

        dbContext.Companies.Add(company);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SeededAccount(company.Id, user.Id, cpf);
    }
}
