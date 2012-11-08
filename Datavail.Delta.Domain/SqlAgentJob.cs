using System;

namespace Datavail.Delta.Domain
{
    public class SqlAgentJob : DomainBase
    {
        #region Fields
        #endregion

        #region Properties
        public virtual DatabaseInstance Instance { get; set; }
        public virtual Status Status { get; set; }
        public string Name { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewSqlAgentJob factory instead")]
        public SqlAgentJob()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static SqlAgentJob NewSqlAgentJob(string name, DatabaseInstance instance)
        {
            var entity = new SqlAgentJob { Name = name, Instance = instance, Status = Domain.Status.Active };
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