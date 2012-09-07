using System;
using System.Data.Objects;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Objects.DataClasses;
using Datavail.Delta.Infrastructure.Repository;

namespace Datavail.Delta.Repository.Ef.Infrastructure
{
    internal class UnitOfWork : IUnitOfWork
    {
        private DbTransaction _transaction;
        private readonly DbContext _dbContext;

        public UnitOfWork(DbContext context)
        {
            _dbContext = context;
        }

        public bool IsInTransaction
        {
            get { return _transaction != null; }
        }

        public void BeginTransaction()
        {
            BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public void BeginTransaction(IsolationLevel isolationLevel)
        {
            if (_transaction != null)
            {
                throw new ApplicationException("Cannot begin a new transaction while an existing transaction is still running. " +
                                                "Please commit or rollback the existing transaction before starting a new one.");
            }
            OpenConnection();
            _transaction = ((IObjectContextAdapter)_dbContext).ObjectContext.Connection.BeginTransaction(isolationLevel);
        }

        public void RollBackTransaction()
        {
            if (IsInTransaction)
                _transaction.Rollback();
        }

        public void CommitTransaction()
        {
            if (_transaction == null)
            {
                throw new ApplicationException("Cannot roll back a transaction while there is no transaction running.");
            }

            try
            {
                ((IObjectContextAdapter)_dbContext).ObjectContext.SaveChanges();
                _transaction.Commit();
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally
            {
                ReleaseCurrentTransaction();
            }
        }

        public void SaveChanges()
        {
            if (IsInTransaction)
            {
                throw new ApplicationException("A transaction is running. Call BeginTransaction instead.");
            }
            try { ((IObjectContextAdapter)_dbContext).ObjectContext.SaveChanges(); }
            catch (OptimisticConcurrencyException ex)
            {
                // Resolve the concurrency conflict by refreshing the 
                // object context before re-saving changes. 
                foreach (var stateEntry in ex.StateEntries)
                {
                    if (stateEntry.Entity != null)
                    {
                        if (stateEntry.State == EntityState.Detached)
                        {
                            ((IObjectContextAdapter)_dbContext).ObjectContext.Attach((IEntityWithKey)stateEntry.Entity);
                        }

                        ((IObjectContextAdapter)_dbContext).ObjectContext.Refresh(RefreshMode.ClientWins, stateEntry.Entity);
                    }
                }

                // Save changes.
                ((IObjectContextAdapter)_dbContext).ObjectContext.SaveChanges();
            }
        }

        public void SaveChanges(SaveOptions saveOptions)
        {
            if (IsInTransaction)
            {
                throw new ApplicationException("A transaction is running. Call BeginTransaction instead.");
            }

            ((IObjectContextAdapter)_dbContext).ObjectContext.SaveChanges(saveOptions);
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes off the managed and unmanaged resources used.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (_disposed)
                return;

            _disposed = true;
        }

        private bool _disposed;
        #endregion

        private void OpenConnection()
        {
            if (((IObjectContextAdapter)_dbContext).ObjectContext.Connection.State != ConnectionState.Open)
            {
                ((IObjectContextAdapter)_dbContext).ObjectContext.Connection.Open();
            }
        }

        /// <summary>
        /// Releases the current transaction
        /// </summary>
        private void ReleaseCurrentTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }
    }
}
