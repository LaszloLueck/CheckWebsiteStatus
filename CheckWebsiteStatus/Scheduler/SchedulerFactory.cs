using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;

namespace CheckWebsiteStatus.Scheduler
{
 public class CustomSchedulerFactory<T> : ISchedulerFactory where T : class, IJob
    {
        private readonly string _jobName;
        private readonly string _groupName;
        private readonly string _triggerName;
        private readonly int _repeatIntervalInSeconds;
        private IScheduler _scheduler;
        private readonly StdSchedulerFactory _factory;

        public CustomSchedulerFactory(string jobName, string groupName, string triggerName, int repeatIntervalInSeconds)
        {
            _jobName = jobName;
            _groupName = groupName;
            _triggerName = triggerName;
            _repeatIntervalInSeconds = repeatIntervalInSeconds;
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
            await _scheduler.Start();
        }

        private async Task ScheduleJob()
        {
            var job = GetJob();
            var trigger = GetTrigger();
            await _scheduler.ScheduleJob(job, trigger);
        }

        public async Task ShutdownScheduler()
        {
            await _scheduler.Shutdown();
        }
    }
}