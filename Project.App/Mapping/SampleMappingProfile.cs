using AutoMapper;
using Project.Application.SampleQueries;
using Project.Application.SampleQueries.GetAllSamplesQuery;
using Project.Domain.Entities.Sample;

namespace Project.Application.Mapping
{
    public class FoodMappingProfile : Profile
    {
        public FoodMappingProfile()
        {
            CreateMap<Sample, SampleResponse>();


        }
    }
}
