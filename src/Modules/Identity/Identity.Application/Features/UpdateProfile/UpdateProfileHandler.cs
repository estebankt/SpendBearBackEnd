using SpendBear.SharedKernel;
using Identity.Domain.Repositories;

namespace Identity.Application.Features.UpdateProfile;

public class UpdateProfileHandler
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(UpdateProfileCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByAuth0IdAsync(command.Auth0UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(new Error("User.NotFound", "User not found."));
        }

        user.UpdateProfile(command.FirstName, command.LastName);
        await _userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
