using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Project.DataAccess.Repository.SampleRepository;
using Project.Domain.Entities.Sample;
using Project.Infrastructure.Configurations.Queries;

namespace Project.Application.SampleQueries.GetAllSamplesQuery
{
    public class GetAllSamplesQueryHandler : IQueryHandler<GetAllSamplesQuery, GetAllSamplesResponse>
    {
        private readonly IMapper _mapper;
        private readonly ISampleRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetAllSamplesQueryHandler(IMapper mapper, ISampleRepository repository)
        {
            _mapper = mapper;
            _repository = repository;
        }


        public async Task<GetAllSamplesResponse> Handle(GetAllSamplesQuery query, CancellationToken cancellationToken)
        {
            var allSamples = await _repository.GetAll().ToListAsync(cancellationToken);
            var result = _mapper.Map<List<Sample>, List<SampleResponse>>(allSamples);
            return new GetAllSamplesResponse()
            {
                SampleResponses =  result
            };
        }
    }
}
