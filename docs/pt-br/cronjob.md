# CronJob
Solução criada para permitir que tarefas sejam agendadas para serem executadas em background. Para agendar, deve-se utilizar o formato `cron` do UNIX, informando a periodicidade da execução.

## Serviço CronJob
```csharp
class MyCronJob : CronJobService<MyCronJob>
{
    public MyCronJob(
        IScheduleConfig<MyCronJob> config, 
        IHostApplicationLifetime hostApplication, 
        IServiceProvider serviceProvider) : base(config, hostApplication, serviceProvider)
    { }

    public override Task DoWork(CancellationToken cancellationToken)
    {
       //coloque a lógica aqui
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

No exemplo acima, o Job será executado `a cada minuto`.

Você poderá realizar a configuração usando a ferramenta [Cronitor](https://crontab.guru/).