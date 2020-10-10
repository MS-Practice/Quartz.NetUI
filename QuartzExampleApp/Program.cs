using Quartz;
using Quartz.Impl;
using Quartz.Impl.Calendar;
using Quartz.Logging;
using System;
using System.Threading.Tasks;
/// <summary>
/// Cron Expression：总共分为7个部分，以空格分割符相连 秒 分 时 Day-of-Month 月 Day-of-Week 年
/// 0 0 12 ? * WED 表示每周三的12点
/// 0 0/30 8-9 5,20 * ?  每个月的5号-20号的8点到9点半
/// </summary>
namespace QuartzExampleApp
{
    /// <summary>
    /// SimpleTrigger ：一次性作业，可以重复执行（但每次都会重新实例化）
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
            StdSchedulerFactory factory = new StdSchedulerFactory();
            var scheduler = await factory.GetScheduler();

            // 开始
            await scheduler.Start();

            // 定义 Job，绑定作业实例
            var job = JobBuilder.Create<HelloJob>()
                .WithDescription("Job描述")
                .WithIdentity(name: "id", group: "group")
            #region job传参
                .UsingJobData("UserName", "marsonshine")
                .UsingJobData("Age", "28")
            #endregion
                .Build();

            #region 定义时间段日历，配置排除指定时间段
            // 定义日历
            HolidayCalendar cal = new HolidayCalendar();
            // 排除的时间（触发器不会在这个时间节点触发作业）
            cal.AddExcludedDate(new DateTime(2021, 1, 1));
            // 添加进调度器中
            await scheduler.AddCalendar("NewYearDay", cal, replace: false, updateTriggers: true); 
            #endregion

            // 定义并绑定 job 触发器实例
            var trigger = TriggerBuilder.Create()
                .WithIdentity(name: "triggerId", group: "triggerGroup")
                .StartNow()
#if NotWithSchedule
                        .WithSimpleSchedule(s =>
                {
                    s.WithIntervalInSeconds(10)
                    .RepeatForever();
                }) 
#endif
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(9,30))//每天九点30分执行
                .ModifiedByCalendar("NewYearDay")   // 排除指定的时间
                .Build();
            // 定义触发器，每5分钟重复一次，直到到达指定的时间（晚上的22点）
            var trigger2 = TriggerBuilder.Create()
                .WithIdentity(name: "triggerId2", group: "triggerGroup")
                .WithSimpleSchedule(s =>
                {
                    s.WithIntervalInMinutes(5)
                    .RepeatForever();
                })
                .EndAt(DateBuilder.DateOf(22, 0, 0))
                .Build();

            // 将job和触发器绑定到调度器中
            await scheduler.ScheduleJob(jobDetail: job, trigger: trigger);
            await Task.Delay(TimeSpan.FromSeconds(60));

            // 关闭调度程序
            await scheduler.Shutdown();
            Console.WriteLine("Press any key to close the application");
            Console.ReadKey();
        }

        private class ConsoleLogProvider : ILogProvider
        {
            public Logger GetLogger(string name)
            {
                return (level, func, exception, parameters) =>
                {
                    if (level >= LogLevel.Info && func != null)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] [{level}] {func()}", parameters);
                    }
                    return true;
                };
            }

            public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
            {
                throw new NotImplementedException();
            }

            public IDisposable OpenNestedContext(string message)
            {
                throw new NotImplementedException();
            }
        }

        private class HelloJob : IJob
        {
            public string UserName { private get; set; }
            public int Age { private get; set; }
            public HelloJob()
            {
                Console.WriteLine("Constructor");
            }
            // 如果要给job实例传递值，必须通过JobDataMap传递
            public async Task Execute(IJobExecutionContext context)
            {
                // 接受调度器传过来的附加值
                var dataMap = context.JobDetail.JobDataMap;
                string userName = dataMap.GetString("UserName");
                int age = dataMap.GetInt("Age");
                // 可以通过属性来自动获取值，在调度器出发调用作业时，会自动调用同名的属性的setter
                await Console.Out.WriteLineAsync($"Hello Job! 自动接收对应的属性值：Username={UserName}, Age={Age}");
                await Console.Out.WriteLineAsync($"Hello Job! 接收值：username={userName}, age={age}");
            }
        }
    }
}
