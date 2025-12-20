using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Base
{
    public interface IEntityWithEvents
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
    }
    public interface IAuditableEntity
    {
        string CreatedBy { get; set; }
        DateTime CreatedAt { get; set; }
        string? LastModifiedBy { get; set; }
        DateTime? LastModifiedAt { get; set; }
    }
    public abstract class Entity : IAuditableEntity
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }
    public abstract class Entity<TId> : IEntityWithEvents, IAuditableEntity where TId : ValueObject
    {
        private List<IDomainEvent> _domainEvents;

        public TId Id { get; protected set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly();

        protected Entity()
        {
            _domainEvents = new List<IDomainEvent>();
        }

        protected void AddDomainEvent(IDomainEvent eventItem)
        {
            _domainEvents ??= new List<IDomainEvent>();
            _domainEvents.Add(eventItem);
        }

        public void ClearDomainEvents()
        {
            _domainEvents?.Clear();
        }

        public override bool Equals(object obj)
        {
            if (obj is null || obj is not Entity<TId> other)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (GetType() != obj.GetType())
                return false;

            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(Entity<TId> left, Entity<TId> right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Entity<TId> left, Entity<TId> right)
        {
            return !(left == right);
        }
    }
}
