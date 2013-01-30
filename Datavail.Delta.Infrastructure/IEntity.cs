using System;

namespace Datavail.Delta.Infrastructure
{
    public interface IEntity
    {
        Guid Id { get; }
    }
}