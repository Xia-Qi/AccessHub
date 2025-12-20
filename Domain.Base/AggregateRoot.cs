using System;

namespace Domain.Base
{
    public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot where TId : ValueObject
    {
        public int Version { get; protected set; }

        protected void IncrementVersion()
        {
            Version++;
        }
    }

    public interface IAggregateRoot
    {
        int Version { get; }
    }
}