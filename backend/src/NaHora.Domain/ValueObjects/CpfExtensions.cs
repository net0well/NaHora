using System.Diagnostics.CodeAnalysis;

namespace NaHora.Domain.ValueObjects;

/// <summary>Parsing helpers that keep call sites free of try/catch.</summary>
public static class CpfExtensions
{
    public static bool IsValidCpf([NotNullWhen(true)] this string? input) => Cpf.TryParse(input, out _);
}
