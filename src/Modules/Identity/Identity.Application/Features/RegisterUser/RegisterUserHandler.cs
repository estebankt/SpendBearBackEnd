using SpendBear.SharedKernel;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;

namespace Identity.Application.Features.RegisterUser;

public class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;

    public RegisterUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        if (await _userRepository.GetByAuth0IdAsync(command.Auth0UserId, cancellationToken) != null)
        {
            return Result.Failure<Guid>(new Error("User.AlreadyExists", "User with this Auth0 ID already exists."));
        }

        if (!await _userRepository.IsEmailUniqueAsync(command.Email, cancellationToken))
        {
            return Result.Failure<Guid>(new Error("User.EmailNotUnique", "Email is already in use."));
        }

        var userResult = User.Create(command.Auth0UserId, command.Email, command.FirstName, command.LastName);
        if (userResult.IsFailure)
        {
            return Result.Failure<Guid>(userResult.Error);
        }

        await _userRepository.AddAsync(userResult.Value, cancellationToken);
        await _userRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(userResult.Value.Id);
    }
}
