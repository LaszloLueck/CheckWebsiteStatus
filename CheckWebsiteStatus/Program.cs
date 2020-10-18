using System;
using System.Threading.Tasks;
using CheckWebsiteStatus.Configuration;
using CheckWebsiteStatus.Scheduler;
using CheckWebsiteStatus.SimpleLogger;

namespace CheckWebsiteStatus
{
    class Program
    {
        private static readonly ICLogger Logger = CLogger<Program>.GetLogger();

        protected Program()
        {
        }

        static async Task Main(string[] args)
        {
            Logger.Log("Start program");

            Logger.Log("Load Configuration");
            IConfigurationFactory configurationFactory = new ConfigurationFactory();
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder(configurationFactory);

            var a = configurationBuilder.GetConfiguration().Map(configuration =>
            {
                Logger.Log("successfully loaded configuration!");
                Logger.Log("Load and start scheduler");
                ISchedulerFactory schedulerFactory =
                    new CustomSchedulerFactory<SchedulerJob>("job1", "group1", "trigger1", 60, configuration);

                schedulerFactory.RunScheduler();

                Logger.Log("App is in running state!");
                return Task.Delay(-1);
            }).ValueOr(() => Task.CompletedTask);

            await Task.WhenAll(a);

            Environment.Exit(1);
        }
    }
}