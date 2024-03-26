# CronJob
Solução criada para habilitar tarefas para serem executadas em background. Para configurar o agendamento, você deve utilizar o padrão UNIX `cron` informando o periodo da execução.

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
       //Coloque sua lógica aqui
    }
}
```

## Configurações
```csharp
...
services.AddCronJob<MyCronJob>(options => 
{
    TimeZoneInfo = TimeZoneInfo.Utc,
    CronExpression = "* * * * *"
})
```

No exemplo acima, o serviço será executada `a todo minuto`.