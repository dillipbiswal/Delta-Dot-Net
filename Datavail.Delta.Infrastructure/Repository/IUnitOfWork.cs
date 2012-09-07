using System;
using System.Data;
using System.Data.Objects;

namespace Datavail.Delta.Infrastructure.Repository
{
    public interface IUnitOfWork : IDisposable
    {
        bool IsInTransaction { get; }

        void SaveChanges();

        //TODO: Look to refactor to eliminate the need for a Reference to EF in Infrastructure assembly
        void SaveChanges(SaveOptions saveOptions);

        void BeginTransaction();

        void BeginTransaction(IsolationLevel isolationLevel);

        void RollBackTransaction();

        void CommitTransaction();
    }
}