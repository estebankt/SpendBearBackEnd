namespace Identity.Application.Features.RegisterUser;

public record RegisterUserCommand(string Auth0UserId, string Email, string FirstName, string LastName);
