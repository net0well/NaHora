using FaceId.Domain.Abstractions;
using FaceId.Domain.Authorization;
using FaceId.Domain.Errors;
using FaceId.Domain.Users;
using FaceId.Shared.Results;

namespace FaceId.Features.Authentication.GetCurrentUser;

public static class GetCurrentUserHandler
{
    public static async Task<Result<CurrentUserResponse>> Handle(
        GetCurrentUserQuery query,
        ICurrentUser currentUser,
        IUserRepository users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.CompanyId, currentUser.UserId, cancellationToken);

        // The token was valid, but the account is gone or moved. Reported as
        // not found rather than unauthorized: the caller authenticated fine.
        if (user is null)
        {
            return Result.Failure<CurrentUserResponse>(UserErrors.NotFound());
        }

        return Result.Success(new CurrentUserResponse(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role.ToString(),
            currentUser.EmployeeId,
            RolePermissions.For(user.Role)));
    }
}
