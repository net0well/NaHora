namespace FaceId.Shared.Results;

/// <summary>
/// Nature of a failure, so the API layer can pick a status code without
/// matching on message text.
/// </summary>
public enum ErrorType
{
    /// <summary>Input did not satisfy a rule. Maps to 422.</summary>
    Validation = 0,

    /// <summary>The resource does not exist, or is not visible to the caller. Maps to 404.</summary>
    NotFound = 1,

    /// <summary>Conflicts with existing state. Maps to 409.</summary>
    Conflict = 2,

    /// <summary>The caller is authenticated but not allowed. Maps to 403.</summary>
    Forbidden = 3,

    /// <summary>The caller is not authenticated. Maps to 401.</summary>
    Unauthorized = 4,

    /// <summary>A dependency is unavailable. Maps to 503.</summary>
    Unavailable = 5
}
