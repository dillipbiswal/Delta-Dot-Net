using System.Data.Entity.ModelConfiguration;
using Datavail.Delta.Domain;

namespace Datavail.Delta.Repository.Ef.Mapping
{
    public class SqlAgentJobMapping : EntityTypeConfiguration<SqlAgentJob>
    {
        public SqlAgentJobMapping()
        {
            Property(x => x.Name);
            HasKey(x => x.Id);
            Property(x => x.Status.Value).HasColumnName("Status_Id");
            ToTable("SqlAgentJobs");
        }
    }
}