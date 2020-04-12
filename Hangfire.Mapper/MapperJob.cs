using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Mapper
{
    public abstract class MapperJob<T, TState>
    {
        private readonly IBatchJobClient _batchJobClient;

        protected MapperJob(IBatchJobClient batchJobClient)
        {
            _batchJobClient = batchJobClient;
        }

        protected abstract Task<IEnumerable<T>> Query(TState state);

        public abstract Task Next(T resource);

        [JobDisplayName("Starting mapper job.")]
        public async Task Enqueue(TState initialState) => await NextBatch(initialState);

        [JobDisplayName("Enqueueing batch.")]
        public async Task NextBatch(TState state)
        {
            var result = await Query(state);

            if (result == null) return;

            var resources = result.ToArray();

            if (!resources.Any()) return;

            var batchId = _batchJobClient.StartNew(x =>
            {
                foreach (var methodName in OnStartedMethodNames())
                    x.Enqueue(() => CallNotificationMethod(methodName, state));
                
                foreach (var resource in resources)
                    x.Enqueue(() => Next(resource));
            });

            _batchJobClient.ContinueBatchWith(
                batchId,
                x => x.Enqueue(() => NextBatch(state)));
        }

        private IEnumerable<string> OnStartedMethodNames() =>
            GetType().GetMethods()
                .Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(OnJobStartedAttribute)))
                .Select(x => x.Name);

        [JobDisplayName("Executing job notification.")]
        public void CallNotificationMethod(string methodName, TState state)
        {
        }
    }
}