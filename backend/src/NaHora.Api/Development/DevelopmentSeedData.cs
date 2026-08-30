namespace NaHora.Api.Development;

/// <summary>
/// The fixed identifiers and documents the development seed plants.
/// </summary>
/// <remarks>
/// <para>
/// Fixed on purpose. Login is per company, so the app has to know a company
/// identifier before anyone can sign in; a value regenerated on every reset
/// would mean editing the Flutter configuration after each one.
/// </para>
/// <para>
/// The documents are the well-known Brazilian test numbers — they carry valid
/// check digits, which the value objects require, and belong to nobody.
/// </para>
/// </remarks>
internal static class DevelopmentSeedData
{
    public static readonly Guid CompanyId = new("0195f3b0-0001-7000-8000-000000000001");

    public const string Cnpj = "11222333000181";
    public const string LegalName = "NaHora Desenvolvimento LTDA";
    public const string TradeName = "NaHora Dev";
    public const string CompanyEmail = "contato@nahora.dev";

    public const string ManagerCpf = "11144477735";
    public const string ManagerName = "Marina Gestora";
    public const string ManagerEmail = "gestora@nahora.dev";

    public const string EmployeeCpf = "52998224725";
    public const string EmployeePis = "12345678900";
    public const string EmployeeName = "Joana Funcionária";
    public const string EmployeeEmail = "funcionaria@nahora.dev";
    public const string EmployeeRegistration = "0001";
    public const string EmployeePosition = "Operadora";
}
