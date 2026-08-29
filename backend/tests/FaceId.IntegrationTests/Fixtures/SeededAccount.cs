namespace FaceId.IntegrationTests.Fixtures;

/// <summary>
/// The seeded company and user, plus the CPF that signs in.
/// </summary>
/// <remarks>
/// The CPF is generated per seed rather than fixed, so tests sharing one
/// database stay independent of each other and of execution order.
/// </remarks>
internal sealed record SeededAccount(Guid CompanyId, Guid UserId, string Cpf);
