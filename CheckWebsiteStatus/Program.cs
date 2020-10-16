using System;
using System.Threading.Tasks;
using CheckWebsiteStatus.Scheduler;
using CheckWebsiteStatus.SimpleLogger;

namespace CheckWebsiteStatus
{
    class Program
    {
        private static ICLogger Logger = CLogger<Program>.GetLogger();
        
        protected Program(){}
        
        static async Task Main(string[] args)
        {
            Logger.Log("Start program");
            Logger.Log("Load and start scheduler");
            
            ISchedulerFactory schedulerFactory = new CustomSchedulerFactory<SchedulerJob>("job1", "group1", "trigger1", 60);

            await schedulerFactory.RunScheduler();
            
            Console.WriteLine("App is in running state!");
            await Task.Delay(-1);

            Environment.Exit(1);

        }
    }
}