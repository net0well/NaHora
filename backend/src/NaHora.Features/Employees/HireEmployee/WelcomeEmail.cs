using System.Net;
using NaHora.Domain.Companies;
using NaHora.Domain.Employees;
using NaHora.Domain.Notifications;
using NaHora.Domain.ValueObjects;

namespace NaHora.Features.Employees.HireEmployee;

/// <summary>
/// The message a newly registered employee receives.
/// </summary>
/// <remarks>
/// It carries the password, which is why it goes to the employee and to nobody
/// else, and why the body never reaches a log.
/// </remarks>
internal static class WelcomeEmail
{
    public static EmailMessage For(
        Company? company,
        Employee employee,
        EmailAddress to,
        string password)
    {
        var companyName = company?.TradeName ?? company?.LegalName ?? "sua empresa";

        var subject = $"Seu acesso ao NaHora — {companyName}";

        var plainText =
            $"""
             Olá, {employee.FullName}.

             Sua conta no NaHora foi criada por {companyName}.

             Entre com:
               CPF:   {employee.Cpf.Formatted}
               Senha: {password}

             Esta senha é temporária. O app pede uma nova no primeiro acesso.

             Se você não esperava esta mensagem, avise o responsável na empresa.
             """;

        // Encoded: a name is data, and a name with a bracket in it must render
        // as that name rather than as markup.
        var html =
            $"""
             <p>Olá, {Encode(employee.FullName)}.</p>
             <p>Sua conta no NaHora foi criada por {Encode(companyName)}.</p>
             <p>Entre com:<br>
             CPF: <strong>{Encode(employee.Cpf.Formatted)}</strong><br>
             Senha: <strong>{Encode(password)}</strong></p>
             <p>Esta senha é temporária. O app pede uma nova no primeiro acesso.</p>
             <p>Se você não esperava esta mensagem, avise o responsável na empresa.</p>
             """;

        return new EmailMessage(to, employee.FullName, subject, plainText, html);
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
