using System.Collections.ObjectModel;

namespace NaHora.Domain.Adjustments;

/// <summary>
/// Every reason a punch may be corrected for. Fixed by the product, seeded in a
/// migration, never written by a customer.
/// </summary>
/// <remarks>
/// The identifiers are literal and permanent. They are foreign keys from every
/// adjustment ever recorded, so regenerating one would orphan history; a new
/// reason gets a new identifier and a new migration, and an obsolete one is
/// hidden rather than deleted.
/// </remarks>
public static class AdjustmentJustificationCatalogue
{
    public const string ForgotToPunch = "FORGOT_TO_PUNCH";
    public const string DeviceFailure = "DEVICE_FAILURE";
    public const string NoConnectivity = "NO_CONNECTIVITY";
    public const string MedicalLeave = "MEDICAL_LEAVE";
    public const string WorkTravel = "WORK_TRAVEL";
    public const string ExternalWork = "EXTERNAL_WORK";
    public const string CompensatoryTime = "COMPENSATORY_TIME";
    public const string SystemError = "SYSTEM_ERROR";
    public const string ManagerCorrection = "MANAGER_CORRECTION";

    /// <summary>The catch-all, and the only one that obliges free text.</summary>
    public const string Other = "OTHER";

    /// <summary>The catalogue, in the order a manager should see it.</summary>
    public static IReadOnlyList<AdjustmentJustification> All { get; } =
        new ReadOnlyCollection<AdjustmentJustification>(
        [
            new(new Guid("0195f3a1-0001-7000-8000-000000000001"), ForgotToPunch, "Esquecimento de marcação", false, 1),
            new(new Guid("0195f3a1-0002-7000-8000-000000000002"), DeviceFailure, "Falha no aparelho ou bateria", false, 2),
            new(new Guid("0195f3a1-0003-7000-8000-000000000003"), NoConnectivity, "Sem conexão no momento da marcação", false, 3),
            new(new Guid("0195f3a1-0004-7000-8000-000000000004"), MedicalLeave, "Atestado médico", false, 4),
            new(new Guid("0195f3a1-0005-7000-8000-000000000005"), WorkTravel, "Viagem a serviço", false, 5),
            new(new Guid("0195f3a1-0006-7000-8000-000000000006"), ExternalWork, "Trabalho externo sem cobertura", false, 6),
            new(new Guid("0195f3a1-0007-7000-8000-000000000007"), CompensatoryTime, "Compensação de horas", false, 7),
            new(new Guid("0195f3a1-0008-7000-8000-000000000008"), SystemError, "Erro do sistema", false, 8),
            new(new Guid("0195f3a1-0009-7000-8000-000000000009"), ManagerCorrection, "Correção administrativa", false, 9),

            // The only one requiring notes. Left free, it becomes the way to
            // avoid choosing, and the catalogue stops distinguishing anything.
            new(new Guid("0195f3a1-0010-7000-8000-000000000010"), Other, "Outro", true, 10)
        ]);
}
