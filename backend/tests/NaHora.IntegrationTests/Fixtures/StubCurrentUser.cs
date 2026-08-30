using NaHora.Domain.Abstractions;

namespace NaHora.IntegrationTests.Fixtures;

/// <summary>
/// A signed-in caller, for exercising the tenant filter outside a request.
/// </summary>
/// <remarks>
/// The real implementation reads the HTTP context, and there is none in a
/// background scope — which leaves <c>IsAuthenticated</c> false and the filter
/// deliberately disabled. Testing the filter therefore needs a caller that
/// claims to be authenticated without a request behind it.
/// </remarks>
internal sealed class StubCurrentUser(Guid companyId, Guid userId) : ICurrentUser
{
    public Guid UserId { get; } = userId;

    public Guid CompanyId { get; } = companyId;

    public Guid? EmployeeId => null;

    public string Role => "Employee";

    public bool IsAuthenticated => true;

    public bool HasPermission(string permission) => true;
}
