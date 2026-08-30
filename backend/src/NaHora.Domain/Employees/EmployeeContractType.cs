namespace NaHora.Domain.Employees;

/// <summary>
/// The kind of employment relationship.
/// </summary>
/// <remarks>
/// Recorded because working-hours rules differ by it — an apprentice and an
/// intern have their own limits — and because the AFD identifies the worker,
/// not the contract. Nothing in this build branches on it yet; it is captured
/// now so the records already carry it when the rules arrive.
/// </remarks>
public enum EmployeeContractType
{
    /// <summary>Contrato CLT, the ordinary case.</summary>
    Clt = 0,

    /// <summary>Jovem aprendiz.</summary>
    Apprentice = 1,

    /// <summary>Estagiário.</summary>
    Intern = 2,

    /// <summary>Contrato temporário.</summary>
    Temporary = 3,

    /// <summary>Servidor estatutário.</summary>
    Statutory = 4
}
