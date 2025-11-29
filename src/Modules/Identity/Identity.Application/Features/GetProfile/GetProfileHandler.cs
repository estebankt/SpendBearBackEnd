using SpendBear.SharedKernel;
using Identity.Domain.Repositories;

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
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<GetProfileResponse>(new Error("User.NotFound", "User not found."));
        }

        return Result.Success(new GetProfileResponse(user.Id, user.Email, user.FirstName, user.LastName));
    }
}
