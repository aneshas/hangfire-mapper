using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hangfire.Mapper.Tests.DummyJob
{
    public interface IResourceRepository
    {
        Task<IEnumerable<Resource>> List(int page);
    }
}