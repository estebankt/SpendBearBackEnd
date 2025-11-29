using SpendBear.SharedKernel;

namespace Identity.Domain.Entities;

public class User : AggregateRoot
{
    public string Auth0UserId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Private constructor for EF Core
    private User() { }

    private User(string auth0UserId, string email, string firstName, string lastName)
    {
        Auth0UserId = auth0UserId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = DateTime.UtcNow;
        
        // TODO: Raise UserRegisteredEvent
    }

    public static Result<User> Create(string auth0UserId, string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Result.Failure<User>(new Error("User.InvalidAuth0Id", "Auth0UserId is required"));
            
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>(new Error("User.InvalidEmail", "Email is required"));

        var user = new User(auth0UserId, email, firstName, lastName);
        return Result.Success(user);
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}
