namespace Identity.Application.Features.GetProfile;

public record GetProfileQuery(Guid UserId);

public record GetProfileResponse(Guid Id, string Email, string FirstName, string LastName);
