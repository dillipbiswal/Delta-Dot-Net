using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class Cluster : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public virtual Customer Customer { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Server> Nodes { get; set; }
        public virtual ICollection<Server> VirtualServers { get; set; }
        public virtual Status Status { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewCluster factory instead")]
        public Cluster()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static Cluster NewCluster(string name)
        {
            var entity = new Cluster { Name = name };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
            Nodes = new List<Server>();
        }
        #endregion

        #region Methods
        #endregion
    }
}
