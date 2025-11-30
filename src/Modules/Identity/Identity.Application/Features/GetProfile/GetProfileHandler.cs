using SpendBear.SharedKernel;
using Identity.Domain.Repositories;
using Identity.Domain.Entities;

namespace Identity.Application.Features.GetProfile;

public class GetProfileHandler
{
    private readonly IUserRepository _userRepository;

    public GetProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetProfileResponse>> Handle(GetProfileQuery query, CancellationToken cancellationToken = default)
    {
        User? user = null;
        if (query.UserId.HasValue)
        {
            user = await _userRepository.GetByIdAsync(query.UserId.Value, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(query.Auth0UserId))
        {
            user = await _userRepository.GetByAuth0IdAsync(query.Auth0UserId, cancellationToken);
        }

        if (user == null)
        {
            return Result.Failure<GetProfileResponse>(new Error("User.NotFound", "User not found."));
        }

        return Result.Success(new GetProfileResponse(user.Id, user.Email, user.FirstName, user.LastName));
    }
}