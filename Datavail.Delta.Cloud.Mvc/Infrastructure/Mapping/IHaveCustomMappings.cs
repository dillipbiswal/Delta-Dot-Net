using AutoMapper;

namespace Datavail.Delta.Cloud.Mvc.Infrastructure.Mapping
{
    public interface IHaveCustomMappings
    {
        void CreateMappings(IConfiguration configuration);
    }
}