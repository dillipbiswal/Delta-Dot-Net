using System;

namespace Datavail.Delta.Domain
{
    public interface IDomainObject
    {
        Guid Id { get; }
    }
}