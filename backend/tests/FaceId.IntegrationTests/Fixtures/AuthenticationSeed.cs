using FaceId.Domain.Companies;
using FaceId.Domain.Users;
using FaceId.Domain.ValueObjects;
using FaceId.Infrastructure.Persistence;

namespace FaceId.IntegrationTests.Fixtures;

/// <summary>
/// A company and a user ready to sign in.
/// </summary>
internal static class AuthenticationSeed
{
    public const string Password = "Tr0vao-Azul!2026";

    public static async Task<SeededAccount> CreateAsync(
        FaceIdDbContext dbContext,
        IPasswordHasher passwordHasher,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var cpf = DocumentFactory.NewCpf();
        var suffix = Guid.CreateVersion7().ToString("N")[..8];

        var company = Company.Register(
            Cnpj.Create(DocumentFactory.NewCnpj()).Value,
            "Acme Industria Ltda",
            "Acme",
            EmailAddress.Create($"contato-{suffix}@acme.com.br").Value,
            Company.DefaultTimeZoneId,
            now).Value;

        company.ConfirmEmail(now);

        var user = User.Create(
            company.Id,
            Domain.ValueObjects.Cpf.Create(cpf).Value,
            "Maria Souza",
            EmailAddress.Create($"maria.souza-{suffix}@acme.com.br").Value,
            passwordHasher.Hash(Password),
            UserRole.Administrator,
            mustChangePassword: false,
            now).Value;

        user.Activate();

        dbContext.Companies.Add(company);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SeededAccount(company.Id, user.Id, cpf);
    }
}
