using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Quartz;

namespace CheckWebsiteStatus.Scheduler
{
    public class SchedulerJob : IJob
    {

//        private static readonly Configuration.Configuration ConfigurationFactory =
//            new ConfigurationFactory(new ConfigurationUtils()).Configuration;

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(async () =>
            {
                await Task.Run(() => {});
                Console.WriteLine("Fire the scheduled event!");
            });
        }
    }
}