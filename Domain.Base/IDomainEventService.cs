namespace Domain.Base
{
    public interface IDomainEventService
    {
        Task PublishAsync(IDomainEvent domainEvent);
    }
}
