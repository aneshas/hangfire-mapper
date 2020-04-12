using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hangfire.Mapper.Tests.DummyJob
{
    public class DummyMapperJob : MapperJob<Resource, DummyMapperJob.State>
    {
        private readonly IResourceRepository _resourceRepository;
        
        private readonly INotificationRepository _notificationRepository;

        public DummyMapperJob(
            IBatchJobClient batchJobClient, 
            IResourceRepository resourceRepository, 
            INotificationRepository notificationRepository)
            : base(batchJobClient)
        {
            _resourceRepository = resourceRepository;
            _notificationRepository = notificationRepository;
        }

        [OnJobStarted("Marking job as started.")] // TODO - These comments wont get shown
        public Task MarkJobAsStarted(State initialState)
        {
            throw new NotImplementedException();
        }

        [OnJobStarted("Sending job start email notification.")]
        public Task SendJobStartedEmailNotification(State initialState)
        {
            throw new NotImplementedException();
        }

        protected override async Task<IEnumerable<Resource>> Query(State state)
        {
            var resources = await _resourceRepository.List(state.Page);

            state.Page++;

            return resources;
        }

        [JobDisplayName("Processing a single resource.")]
        public override Task Next(Resource resource)
        {
            throw new System.NotImplementedException();
        }

        [OnJobCompleted("Marking job as completed.")]
        public Task MarkJobAsCompleted(State state) =>
            _notificationRepository.Send("MarkJobAsCompleted");

        public class State
        {
            public int Page { get; set; }
        }
    }
}