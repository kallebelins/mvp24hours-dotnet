# CronJob
Solution created to enable tasks to be scheduled to run in the background. To schedule, you must use the UNIX `cron` format, informing the periodicity of the execution.

## CronJob Service
```csharp
class MyCronJob : CronJobService<MyCronJob>
{
    public MyCronJob(
        IScheduleConfig<CustomerCronJob> config, 
        IHostApplicationLifetime hostApplication, 
        IServiceProvider serviceProvider) : base(config, hostApplication, serviceProvider)
    { }

    public override Task DoWork(CancellationToken cancellationToken)
    {
       //put the logic here
    }
}
```

## Settings
```csharp
...
services.AddCronJob<MyCronJob>(options => 
{
    TimeZoneInfo = TimeZoneInfo.Utc,
    CronExpression = "* * * * *"
})
```

In the exemple above, the Job will be executed `every minute`. 