namespace NaHora.Shared.DependencyInjection;

/// <summary>
/// Marks an implementation to be registered with a singleton lifetime.
/// A singleton must not depend on scoped services.
/// </summary>
public interface ISingletonService;
