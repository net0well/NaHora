using FaceId.Domain.Abstractions;
using FaceId.Domain.RealTime;

namespace FaceId.ArchitectureTests;

/// <summary>
/// Keeps every message routable.
/// </summary>
/// <remarks>
/// Routing is by contract, so a message that declares no contract is published
/// to nowhere. Nothing fails when that happens: the send succeeds, the handler
/// never runs, and the feature is quietly half-built. That is what these catch.
/// </remarks>
public sealed class MessagingRules
{
    private static Type[] DomainEvents =>
        [.. typeof(Entity).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Namespace?.EndsWith(".Events", StringComparison.Ordinal) == true)];

    [Fact]
    public void EveryDomainEvent_DeclaresItsDeliveryContract()
    {
        // Arrange & Act
        var undeclared = DomainEvents
            .Where(type => !typeof(IAuditableEvent).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToArray();

        // Assert
        undeclared.ShouldBeEmpty(
            "a domain event must implement IAuditableEvent to reach the durable queue: "
            + string.Join(", ", undeclared));
    }

    [Fact]
    public void TheLiveUpdate_IsNotRoutedToTheDurableQueue()
    {
        // Arrange & Act — the two contracts are mutually exclusive: an event
        // that is worth nothing in ten seconds must not be persisted, retried
        // and redelivered to observers who have long since disconnected.
        var isAuditable = typeof(IAuditableEvent).IsAssignableFrom(typeof(RealTimeEvent));

        // Assert
        isAuditable.ShouldBeFalse("a live update must not travel on the durable queue");
    }

    [Fact]
    public void EveryDomainEvent_CarriesTheCompanyItBelongsTo()
    {
        // Arrange & Act — a message without a tenant cannot be filtered by one,
        // and a consumer that has to guess the tenant guesses wrong eventually.
        var untenanted = DomainEvents
            .Where(type => type.GetProperty("CompanyId") is null)
            .Select(type => type.Name)
            .ToArray();

        // Assert
        untenanted.ShouldBeEmpty($"every event must name its company: {string.Join(", ", untenanted)}");
    }
}
