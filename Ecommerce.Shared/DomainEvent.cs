using MediatR;

namespace Ecommerce.Shared;

public abstract record DomainEvent : INotification
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record OrderPlacedEvent(Guid UserId, Guid OrderId) : DomainEvent;
