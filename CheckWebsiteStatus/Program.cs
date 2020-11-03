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
            await Logger.Log("Start program");

            await Logger.Log("Load Configuration");
            IConfigurationFactory configurationFactory = new ConfigurationFactory();
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder(configurationFactory);

            var a = configurationBuilder.GetConfiguration().Map(configuration =>
            {
                Task.Run(async () =>
                {
                    await Logger.Log("successfully loaded configuration!");
                    await Logger.Log("Load and start scheduler");
                    ISchedulerFactory schedulerFactory =
                        new CustomSchedulerFactory<SchedulerJob>("job1", "group1", "trigger1", 600, configuration);

                    await schedulerFactory.RunScheduler();

                    await Logger.Log("App is in running state!");
                });
                return Task.Delay(-1);
            }).ValueOr(() => Task.CompletedTask);

            await Task.WhenAll(a);

            Environment.Exit(1);
        }
    }
}