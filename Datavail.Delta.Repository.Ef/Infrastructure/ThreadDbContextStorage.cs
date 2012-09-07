using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;

namespace Datavail.Delta.Repository.Ef.Infrastructure
{
    public class ThreadDbContextStorage : IDbContextStorage
    {
        public DbContext GetDbContextForKey(string key)
        {
            SimpleDbContextStorage storage = GetSimpleDbContextStorage();
            return storage.GetDbContextForKey(key);
        }

        public void SetDbContextForKey(string factoryKey, DbContext context)
        {
            SimpleDbContextStorage storage = GetSimpleDbContextStorage();
            storage.SetDbContextForKey(factoryKey, context);
        }

        public IEnumerable<DbContext> GetAllDbContexts()
        {
            SimpleDbContextStorage storage = GetSimpleDbContextStorage();
            return storage.GetAllDbContexts();
        }

        private SimpleDbContextStorage GetSimpleDbContextStorage()
        {
            var namedDataSlot = Thread.GetNamedDataSlot(StorageKey);
            var storage = Thread.GetData(namedDataSlot) as SimpleDbContextStorage;
            if (storage == null)
            {
                storage = new SimpleDbContextStorage();
                Thread.SetData(namedDataSlot, storage);
            }
            return storage;
        }

        private const string StorageKey = "ThreadDbContextStorageKey";
    }
}