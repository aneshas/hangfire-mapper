using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hangfire.Mapper.Tests.DummyJob
{
    public class DummyMapperJob : MapperJob<Resource, DummyMapperJob.State>
    {
        private readonly IResourceRepository _resourceRepository;

        public DummyMapperJob(IBatchJobClient batchJobClient, IResourceRepository resourceRepository)
            : base(batchJobClient)
        {
            _resourceRepository = resourceRepository;
        }

        public class State
        {
            public int Page { get; set; }
        }

        protected override async Task<IEnumerable<Resource>> Query(State state)
        {
            var resources = await _resourceRepository.List(state.Page);

            state.Page++;

            return resources;
        }

        public override Task Next(Resource item)
        {
            throw new System.NotImplementedException();
        }
    }
}