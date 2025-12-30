using AccessHub.Infrastructure.Database;
using Domain.Base;
using Microsoft.EntityFrameworkCore.Storage;

namespace AccessHub.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AccessHubDbContext _context;
        private readonly IDomainEventService _domainEventService;
        private IDbContextTransaction _transaction;

        public UnitOfWork(
            AccessHubDbContext context,
            IDomainEventService domainEventService)
        {
            _context = context;
            _domainEventService = domainEventService;
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction is already in progress.");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress.");
            }

            try
            {
                await SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await DisposeTransactionAsync();
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress.");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await DisposeTransactionAsync();
            }
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

        private async Task DisposeTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
