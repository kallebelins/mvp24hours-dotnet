# Logging
> Em construção.

## Serilog
Serilog é uma biblioteca de log de diagnóstico para aplicativos .NET. Acesse e descubra:
[Serilog](https://serilog.net/)

## NLog
NLog é uma biblioteca fácil de configurar. Acesse e descubra:
[NLog Asp.NET Core 3](https://github.com/NLog/NLog/wiki/Getting-started-with-ASP.NET-Core-3)

Siga os modelos de arquivo xml para configuração do NLog.

### Log Console
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true">
	<targets>
		<target name="console"
				xsi:type="ColoredConsole"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}" />
		<target name="debug"
				xsi:type="Debugger"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}" />
	</targets>
	<rules>
		<logger name="*" minlevel="Trace" writeTo="console,debug" />
	</rules>
</nlog>
```

### Log Arquivo
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true">
	<targets>
		<target name="logfile"
				xsi:type="File"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}"
				fileName="${basedir}/logs/${date:format=yyyy-MM-dd}-webapi.log" />
	</targets>
	<rules>
		<logger name="*" minlevel="Trace" writeTo="logfile" />
	</rules>
</nlog>
```

### Log Csv
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
	<variable name="applicationName" value="MyApplication"/>
	<targets>
		<target name="asyncFile"
				xsi:type="AsyncWrapper"
				queueLimit="5000"
				overflowAction="Discard">
			<target name="file"
					xsi:type="File"
					fileName="${basedir}/logs/${applicationName}/log.csv"
					archiveFileName="${basedir}/logs/${applicationName}/log.{######}.csv"
					maxArchiveFiles="180"
					archiveEvery="Hour"
					archiveNumbering="Sequence"
					concurrentWrites="true"
					keepFileOpen="false"
					encoding="iso-8859-2">
				<layout xsi:type="CsvLayout">
					<column name="Type" layout="${level}"/>
					<column name="DateTime" layout="${date}" />
					<column name="Custom-Message" layout="${message}" />
					<column name="Error-Source" layout="${event-context:item=error-source}" />
					<column name="Error-Class" layout="${event-context:item=error-class}" />
					<column name="Error-Method" layout="${event-context:item=error-method}" />
					<column name="Error-Message" layout="${event-context:item=error-message}" />
					<column name="Inner-Error-Message" layout="${event-context:item=inner-error-message}" />
					<column name="Web-Variables" layout="${web_variables}" />
				</layout>
			</target>
		</target>
	</targets>
	<rules>
		<logger name="*" minlevel="Debug" writeTo="file" />
	</rules>
</nlog>
```

### Log Email
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
	  autoReload="true">
	<targets>
		<target name="console"
				xsi:type="ColoredConsole"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}" />
		<target name="debug"
				xsi:type="Debugger"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}" />
		<target name="logfile"
				xsi:type="File"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}"
				fileName="${basedir}/logs/${date:format=yyyy-MM-dd}-webapi.log" />
		<target name="Mail"
			 xsi:type="Mail" html="true"
			 subject="Server-Date: ${longdate}; Level: ${level}"
			 body="Server-Date: ${longdate}; Level: ${level}; Message: ${message}"
			 to="recipient@sample.com"
			 from="webmaster@sample.com"
			 Encoding="UTF-8"
			 smtpUsername="webmaster@sample.com"
			 enableSsl="true"
			 smtpPassword="123456"
			 smtpAuthentication="Basic"
			 smtpServer="smtp-relay.sample.com"
			 smtpPort="587" />
	</targets>
	<rules>
		<logger name="*" level="Fatal" writeTo="Mail" />
		<logger name="*" level="Error" writeTo="Mail" />
		<logger name="*" minlevel="Trace" writeTo="console,debug" />
		<logger name="*" minlevel="Trace" writeTo="logfile" />
	</rules>
</nlog>
```

### Log SqlServer
```xml
<?xml version="1.0" encoding="utf-8" ?>
<!-- 
// Running script database:
CREATE TABLE [dbo].[logs](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[datetime] [datetime] NOT NULL,
	[message] [nvarchar](4000) NOT NULL,
	[lvl] [nchar](10) NOT NULL,
 CONSTRAINT [PK_logs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
-->
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
	  autoReload="true">
	<targets>
		<target name="logfile"
				xsi:type="File"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}"
				fileName="${basedir}/logs/${date:format=yyyy-MM-dd}-webapi.log" />
		<target xsi:type="Database"
				name="database"
				dbProvider="System.Data.SqlClient"
				connectionString="data source=.;Initial Catalog=nlog;Integrated Security=True;"
				commandText="INSERT INTO [logs](datetime,message,lvl) VALUES (getutcdate(),@msg,@level)">
			<parameter name="@msg" layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}" />
			<parameter name="@level" layout="${level}" />
		</target>
	</targets>
	<rules>
		<logger name="*" level="Fatal" writeTo="database" />
		<logger name="*" level="Error" writeTo="database" />
		<logger name="*" minlevel="Trace" writeTo="logfile" />
	</rules>
</nlog>
```

### Log ElasticSearch
```xml
<?xml version="1.0" encoding="utf-8" ?>
<!-- 
Install-Package NLog.Targets.ElasticSearch
-->
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
	  autoReload="true">
	<extensions>
		<add assembly="NLog.Targets.ElasticSearch"/>
	</extensions>
	<targets>
		<target name="elastic" xsi:type="BufferingWrapper" flushTimeout="5000">
			<target xsi:type="ElasticSearch"
				requireAuth="true"
				username="myUserName"
				password="coolpassword"
				layout="Server-Date: ${longdate}; Level: ${level}; Message: ${message}"
				uri="http://localhost:9200" />
		</target>
	</targets>
	<rules>
		<logger name="*" minlevel="Info" writeTo="elastic" />
	</rules>
</nlog>
```

### ElasticSearch APM
```xml
<?xml version="1.0" encoding="utf-8" ?>
<!-- 
Install-Package Elastic.Apm.NLog
-->
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
	  autoReload="true">
	<extensions>
		<add assembly="Elastic.Apm.NLog"/>
	</extensions>
	<targets>
		<target type="file" name="logfile" fileName="myfile.txt">
			<layout type="jsonlayout">
				<attribute name="traceid" layout="${ElasticApmTraceId}" />
				<attribute name="transactionid" layout="${ElasticApmTransactionId}" />
			</layout>
		</target>
	</targets>
	<rules>
		<logger name="*" minlevel="Trace" writeTo="logfile" />
	</rules>
</nlog>
```
O pré-requisito para funcionar corretamente é estar com um Agente Elastic APM configurado. Se o agente não estiver configurado, os espaços reservados do APM estarão vazios. Acesse [APM Agent .NET](https://github.com/elastic/apm-agent-dotnet).

### Outras Configurações
Veja outras opções em [NLog-Project](https://nlog-project.org/config/?tab=layout-renderers).