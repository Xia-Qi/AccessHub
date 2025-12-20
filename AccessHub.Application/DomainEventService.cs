using Domain.Base;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccessHub.Application
{
    /// <summary>
    /// 领域事件服务的具体实现。这里使用MediatR发布事件。
    /// </summary>
    public class DomainEventService : IDomainEventService
    {
        private readonly IPublisher _mediator;
        private readonly ILogger<DomainEventService> _logger;

        public DomainEventService(
            IPublisher mediator,
            ILogger<DomainEventService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task PublishAsync(IDomainEvent domainEvent)
        {
            _logger.LogInformation("Publishing domain event: {event}", domainEvent.GetType().Name);
            await _mediator.Publish(domainEvent);
        }
    }
}
