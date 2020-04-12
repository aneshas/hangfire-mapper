using System;
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

        public abstract Task Next(T item);

        public async Task Enqueue(TState initialState) => await NextBatch(initialState);

        // TODO - Add job name comment
        public async Task NextBatch(TState state)
        {
            var result = await Query(state);

            if (result == null) return;

            var resources = result.ToArray();

            if (!resources.Any()) return;
            
            var batchId = _batchJobClient.StartNew(x =>
            {
                foreach (var resource in resources)
                {
                    x.Enqueue(() => Next(resource));
                }
            });

            _batchJobClient.ContinueBatchWith(
                batchId,
                x => x.Enqueue(() => NextBatch(state)));
        }
    }
}