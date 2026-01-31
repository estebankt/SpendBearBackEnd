using FluentValidation;

namespace Identity.Application.Features.RegisterUser;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Auth0UserId).NotEmpty().WithMessage("Auth0 User ID is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}
