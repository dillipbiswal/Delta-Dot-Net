using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class DatabaseInstance : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public virtual IList<Database> Databases { get; set; }
        public virtual IList<SqlAgentJob> Jobs { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public virtual Server Server { get; set; }
        public virtual Status Status { get; set; }
        public DatabaseVersion DatabaseVersion { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        public string Username { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewDatabaseInstance factory instead")]
        public DatabaseInstance()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static DatabaseInstance NewDatabaseInstance(string name, Server server, bool useIntegratedSecurity = true, string username = null, string password = null)
        {
            var entity = new DatabaseInstance { Name = name, Server = server, UseIntegratedSecurity = useIntegratedSecurity, Username = username, Password = password };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
        }
        #endregion

        #region Methods
        #endregion
    }
}
