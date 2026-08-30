using NaHora.Features.Common;

namespace NaHora.Features.Employees.GetMyProfile;

/// <summary>
/// The employee's own record, as their profile screen shows it.
/// </summary>
/// <param name="IsExportableToAfd">
/// False when the PIS is missing. Surfaced so the employee can see what is
/// outstanding on their own record rather than finding out when an export
/// leaves them off it.
/// </param>
public sealed record GetMyProfileResponse(
    Guid Id,
    string FullName,
    string Cpf,
    string CpfFormatted,
    string? Pis,
    string Registration,
    string? Position,
    string? Department,
    string ContractType,
    string Status,
    DateOnly HiredAt,
    DateOnly? TerminatedAt,
    DateOnly? BirthDate,
    string? Email,
    string? Phone,
    string? PhoneFormatted,
    AddressResponse? Address,
    bool IsExportableToAfd);
