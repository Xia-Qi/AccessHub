using AccessHub.Infrastructure.Database;
using Domain.Base;

namespace AccessHub.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AccessHubDbContext _context;
        private readonly IDomainEventService _domainEventService;

        public UnitOfWork(
            AccessHubDbContext context,
            IDomainEventService domainEventService)
        {
            _context = context;
            _domainEventService = domainEventService;
        }

        public Task BeginTransactionAsync()
        {
            throw new NotImplementedException();
        }

        public Task CommitTransactionAsync()
        {
            throw new NotImplementedException();
        }

        public Task RollbackTransactionAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<int> SaveChangesAsync()
        {
            // Dispatch Domain Events before saving changes
            await DispatchDomainEvents();

            return await _context.SaveChangesAsync();
        }

        private async Task DispatchDomainEvents()
        {
            var entities = _context.ChangeTracker
                .Entries<IEntityWithEvents>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity);

            var domainEvents = entities
                .SelectMany(e => e.DomainEvents)
                .ToList();

            entities.ToList().ForEach(e => e.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                await _domainEventService.PublishAsync(domainEvent);
            }
        }
    }
}
