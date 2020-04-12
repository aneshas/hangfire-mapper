using System.Threading.Tasks;

namespace Hangfire.Mapper.Tests.DummyJob
{
    public interface INotificationRepository
    {
        Task Send(string source);
    }
}