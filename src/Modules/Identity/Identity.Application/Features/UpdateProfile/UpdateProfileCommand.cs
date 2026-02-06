namespace Identity.Application.Features.UpdateProfile;

public record UpdateProfileCommand(string Auth0UserId, string FirstName, string LastName);
