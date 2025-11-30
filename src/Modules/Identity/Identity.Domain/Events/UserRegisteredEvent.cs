using SpendBear.SharedKernel;

namespace Identity.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : DomainEvent;
