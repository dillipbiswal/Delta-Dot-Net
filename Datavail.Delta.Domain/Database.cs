using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class Database : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public virtual DatabaseInstance Instance { get; set; }
        public virtual StatusWrapper Status { get; set; }
        public string Name { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewDatabase factory instead")]
        public Database()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static Database NewDatabase(string name, DatabaseInstance instance)
        {
            var entity = new Database { Name = name, Instance = instance, Status = Domain.Status.Active };
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