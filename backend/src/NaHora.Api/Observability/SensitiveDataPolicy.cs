using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace NaHora.Api.Observability;

/// <summary>
/// Strips values that must never reach a log.
/// </summary>
/// <remarks>
/// Enforced as a Serilog policy rather than left to whoever writes the log
/// statement. Discipline fails exactly once and the failure is silent — and for
/// biometric data the failure is permanent, because a leaked face cannot be
/// rotated the way a password can.
///
/// This runs on destructured objects. A value interpolated into a message
/// template is not covered, which is why the test suite also scans log output
/// for these terms.
/// </remarks>
internal sealed class SensitiveDataPolicy : IDestructuringPolicy
{
    private static readonly string[] ForbiddenFragments =
    [
        "password",
        "passwordhash",
        "salt",
        "token",
        "secret",
        "apikey",
        "privatekey",
        "signature",

        // Biometric material and the numbers that would let an attacker tune an
        // attack against it.
        "embedding",
        "vector",
        "recognitionscore",
        "livenessscore",
        "qualityscore",
        "threshold"
    ];

    private static readonly ConcurrentDictionary<Type, PropertyPlan[]> Plans = new();

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        result = null!;

        if (value is null)
        {
            return false;
        }

        var type = value.GetType();

        if (type.IsPrimitive || type == typeof(string) || type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
        {
            return false;
        }

        var plan = Plans.GetOrAdd(type, BuildPlan);
        if (plan.Length == 0)
        {
            return false;
        }

        var properties = new List<LogEventProperty>(plan.Length);

        foreach (var entry in plan)
        {
            if (entry.IsSensitive)
            {
                // The name is kept and the value dropped: a reader learns that
                // the field was present without learning what it held.
                properties.Add(new LogEventProperty(entry.Name, new ScalarValue("[redacted]")));
                continue;
            }

            var propertyValue = entry.Read(value);
            properties.Add(new LogEventProperty(entry.Name, propertyValueFactory.CreatePropertyValue(propertyValue, destructureObjects: true)));
        }

        result = new StructureValue(properties);

        return true;
    }

    private static PropertyPlan[] BuildPlan(Type type)
    {
        var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToArray();

        if (properties.Length == 0 || !properties.Any(property => IsSensitive(property.Name)))
        {
            // Nothing to hide: let Serilog handle it normally rather than
            // paying the reflection cost on every type in the application.
            return [];
        }

        return [.. properties.Select(property => new PropertyPlan(
            property.Name,
            IsSensitive(property.Name),
            instance => SafeRead(property, instance)))];
    }

    private static object? SafeRead(System.Reflection.PropertyInfo property, object instance)
    {
        try
        {
            return property.GetValue(instance);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // A property that throws must not take down the log statement that
            // was trying to explain what went wrong.
            return "[unreadable]";
        }
    }

    private static bool IsSensitive(string propertyName) =>
        ForbiddenFragments.Any(fragment =>
            propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private sealed record PropertyPlan(string Name, bool IsSensitive, Func<object, object?> Read);
}
