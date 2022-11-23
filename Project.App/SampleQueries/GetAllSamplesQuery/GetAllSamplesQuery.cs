using Project.Infrastructure.Configurations.Queries;

namespace Project.Application.SampleQueries.GetAllSamplesQuery
{
    public class GetAllSamplesQuery : IQuery<GetAllSamplesResponse>
    {
        public GetAllSamplesQuery(GetAllSamplesRequest request)
        {
            Request = request;
        }

        public GetAllSamplesRequest Request { get; set; }

    }
}
