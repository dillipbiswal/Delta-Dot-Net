using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Datavail.Delta.Repository.Ef.Infrastructure
{
    public class DbContextManager
    {
        public static void Init(string[] mappingAssemblies, bool recreateDatabaseIfExist = false, bool lazyLoadingEnabled = true)
        {
            Init(DefaultConnectionStringName, mappingAssemblies, recreateDatabaseIfExist, lazyLoadingEnabled);
        }

        public static void Init(string connectionStringName, string[] mappingAssemblies, bool recreateDatabaseIfExist = false, bool lazyLoadingEnabled = true)
        {
            AddConfiguration(connectionStringName, mappingAssemblies, recreateDatabaseIfExist, lazyLoadingEnabled);
        }       

        public static void Init(string connectionStringName, DbContext context)
        {
            
        }

        public static void InitStorage(IDbContextStorage storage)
        {
            if (storage == null) 
            {
                throw new ArgumentNullException("storage");
            }
            if ((Storage != null) && (Storage != storage))
            {
                throw new ApplicationException("A storage mechanism has already been configured for this application");
            }            
            Storage = storage;
        }

        /// <summary>
        /// The default connection string name used if only one database is being communicated with.
        /// </summary>
        public static readonly string DefaultConnectionStringName = "DefaultDb";        

        /// <summary>
        /// Used to get the current db context session if you're communicating with a single database.
        /// When communicating with multiple databases, invoke <see cref="CurrentFor" /> instead.
        /// </summary>
        public static DbContext Current
        {
            get
            {
                return CurrentFor(DefaultConnectionStringName);
            }
        }

        /// <summary>
        /// Used to get the current DbContext associated with a key; i.e., the key 
        /// associated with an object context for a specific database.
        /// 
        /// If you're only communicating with one database, you should call <see cref="Current" /> instead,
        /// although you're certainly welcome to call this if you have the key available.
        /// </summary>
        public static DbContext CurrentFor(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            if (Storage == null)
            {
                throw new ApplicationException("An IDbContextStorage has not been initialized");
            }

            DbContext context;
            lock (SyncLock)
            {
                if (!_dbContextBuilders.ContainsKey(key))
                {
                    throw new ApplicationException("An DbContextBuilder does not exist with a key of " + key);
                }

                context = Storage.GetDbContextForKey(key);

                if (context == null)
                {
                    context = _dbContextBuilders[key].BuildDbContext();
                    Storage.SetDbContextForKey(key, context);
                }
            }
            return context;
        }

        /// <summary>
        /// This method is used by application-specific db context storage implementations
        /// and unit tests. Its job is to walk thru existing cached object context(s) and Close() each one.
        /// </summary>
        public static void CloseAllDbContexts()
        {
            foreach (var ctx in Storage.GetAllDbContexts().Where(ctx => ctx.Database.Connection.State == System.Data.ConnectionState.Open))
            {
                ctx.Database.Connection.Close();
            }
        }

        private static void AddConfiguration(string connectionStringName, string[] mappingAssemblies, bool recreateDatabaseIfExists = false, bool lazyLoadingEnabled = true)
        {
            if (string.IsNullOrEmpty(connectionStringName))
            {
                throw new ArgumentNullException("connectionStringName");
            }

            if (mappingAssemblies == null)
            {
                throw new ArgumentNullException("mappingAssemblies");
            }

            lock (SyncLock)
            {
                _dbContextBuilders.Add(connectionStringName,
                    new DbContextBuilder<DbContext>(connectionStringName, mappingAssemblies, recreateDatabaseIfExists, lazyLoadingEnabled));
            }
        }

        /// <summary>
        /// An application-specific implementation of IDbContextStorage must be setup either thru
        /// <see cref="InitStorage" /> or one of the Init overloads. 
        /// </summary>
        private static IDbContextStorage Storage { get; set; }

        /// <summary>
        /// Maintains a dictionary of db context builders, one per database.  The key is a 
        /// connection string name used to look up the associated database, and used to decorate respective
        /// repositories. If only one database is being used, this dictionary contains a single
        /// factory with a key of <see cref="DefaultConnectionStringName" />.
        /// </summary>
        private static Dictionary<string, IDbContextBuilder<DbContext>> _dbContextBuilders = new Dictionary<string, IDbContextBuilder<DbContext>>();

        private static readonly object SyncLock = new object();
    }
}
