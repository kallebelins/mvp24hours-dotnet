# CronJob
Solu��o criada para habilitar tarefas para serem executadas em background. Para configurar o agendamento, voc� deve utilizar o padr�o UNIX `cron` informando o periodo da execu��o.

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
       //Coloque sua l�gica aqui
    }
}
```

## Configura��es
```csharp
...
services.AddCronJob<MyCronJob>(options => 
{
    TimeZoneInfo = TimeZoneInfo.Utc,
    CronExpression = "* * * * *"
})
```

No exemplo acima, o servi�o ser� executada `a todo minuto`.