namespace Identity.Application.Features.GetProfile;

public record GetProfileQuery(Guid? UserId = null, string? Auth0UserId = null);

public record GetProfileResponse(Guid Id, string Email, string FirstName, string LastName);
