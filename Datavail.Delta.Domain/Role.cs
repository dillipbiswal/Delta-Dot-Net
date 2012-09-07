using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class Role : DomainBase
    {
        #region "Fields"
        #endregion

        #region "Properties"
        public string Name { get; set; }
        public virtual IList<User> Users { get; set; }
        #endregion

        #region "ctor"
        public Role()
        {
        }

#pragma warning disable 612,618
        public static Role NewRole(string name)
        {
            var entity = new Role() { Name = name};
            return entity;
        }
#pragma warning restore 612,618
        #endregion
    }
}
