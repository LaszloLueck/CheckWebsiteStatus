using System.Threading.Tasks;

namespace CheckWebsiteStatus.Scheduler
{
    public interface ISchedulerFactory
    {
        Task ShutdownScheduler();

        Task RunScheduler(); 
    }
}