using System;
using System.Threading.Tasks;
using CheckWebsiteStatus.Configuration;
using CheckWebsiteStatus.SimpleLogger;
using Quartz;
using Quartz.Impl;

namespace CheckWebsiteStatus.Scheduler
{
    public class CustomSchedulerFactory<T> : ISchedulerFactory where T : class, IJob
    {
        private static readonly ICLogger Logger = CLogger<CustomSchedulerFactory<T>>.GetLogger();

        private readonly string _jobName;
        private readonly string _groupName;
        private readonly string _triggerName;
        private readonly int _repeatIntervalInSeconds;
        private IScheduler _scheduler;
        private readonly StdSchedulerFactory _factory;
        private readonly ConfigurationItems _configurationItems;

        public CustomSchedulerFactory(string jobName, string groupName, string triggerName, int repeatIntervalInSeconds, ConfigurationItems configurationItems)
        {
            _jobName = jobName;
            _groupName = groupName;
            _triggerName = triggerName;
            _repeatIntervalInSeconds = repeatIntervalInSeconds;
            _configurationItems = configurationItems;
            _factory = new StdSchedulerFactory();
        }

        public async Task RunScheduler()
        {
            await BuildScheduler();
            await StartScheduler();
            await ScheduleJob();
        }

        private async Task BuildScheduler()
        {
            Logger.Log("Build the Scheduler");
            _scheduler = await _factory.GetScheduler();
        }

        private IJobDetail GetJob()
        {
            return JobBuilder
                .Create<T>()
                .WithIdentity(_jobName, _groupName)
                .Build();
        }

        private ITrigger GetTrigger()
        {
            var dto = new DateTimeOffset(DateTime.Now).AddSeconds(5);
            return TriggerBuilder
                .Create()
                .WithIdentity(_triggerName, _groupName)
                .StartAt(dto)
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(_repeatIntervalInSeconds).RepeatForever())
                .Build();
        }

        private async Task StartScheduler()
        {
            Logger.Log("Start the Scheduler");
            await _scheduler.Start();
        }

        private async Task ScheduleJob()
        {
            Logger.Log("Schedule the Job");
            var job = GetJob();
            job.JobDataMap.Put("configuration", _configurationItems);
            var trigger = GetTrigger();
            await _scheduler.ScheduleJob(job, trigger);
        }

        public async Task ShutdownScheduler()
        {
            await _scheduler.Shutdown();
        }
    }
}