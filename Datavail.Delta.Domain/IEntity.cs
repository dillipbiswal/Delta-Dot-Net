using System;

namespace Datavail.Delta.Domain
{
    public interface IEntity
    {
        Guid Id { get; }
    }
}