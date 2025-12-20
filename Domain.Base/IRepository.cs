using System.Threading.Tasks;

namespace Domain.Base
{
    public interface IRepository<TAggregate, TId> 
        where TAggregate : AggregateRoot<TId>
        where TId : ValueObject
    {
        Task<TAggregate> GetByIdAsync(TId id);
        Task AddAsync(TAggregate aggregate);
        Task UpdateAsync(TAggregate aggregate);
        Task DeleteAsync(TAggregate aggregate);
    }
}