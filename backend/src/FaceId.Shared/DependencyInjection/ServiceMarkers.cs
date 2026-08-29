namespace FaceId.Shared.DependencyInjection;

/// <summary>
/// Marks an implementation to be registered with a scoped lifetime by the
/// assembly scanner. Prefer this over manual registration.
/// </summary>
public interface IScopedService;

/// <summary>
/// Marks an implementation to be registered with a transient lifetime.
/// </summary>
public interface ITransientService;

/// <summary>
/// Marks an implementation to be registered with a singleton lifetime.
/// A singleton must not depend on scoped services.
/// </summary>
public interface ISingletonService;
