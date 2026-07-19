using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent;

namespace Mvp24Hours.Application.RabbitMQ.Test.Configuration.Fluent;

public class FluentBuildersTest
{
    #region RetryPolicyConfiguration (public class, direct construction)

    [Fact]
    public void RetryPolicyConfiguration_Default_ShouldHaveExpectedValues()
    {
        var config = new RetryPolicyConfiguration();

        config.RetryType.Should().Be(RetryType.Exponential);
        config.RetryCount.Should().Be(3);
        config.InitialInterval.Should().Be(TimeSpan.FromSeconds(1));
        config.MaxInterval.Should().Be(TimeSpan.FromMinutes(5));
        config.ExponentialBase.Should().Be(2.0);
        config.EnableJitter.Should().BeFalse();
        config.MaxJitterPercent.Should().Be(20);
        config.HandledExceptions.Should().BeEmpty();
        config.IgnoredExceptions.Should().BeEmpty();
    }

    [Fact]
    public void RetryPolicyConfiguration_GetDelay_Immediate_ShouldReturnZero()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.Immediate,
            RetryCount = 3
        };

        config.GetDelay(1).Should().Be(TimeSpan.Zero);
        config.GetDelay(3).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void RetryPolicyConfiguration_GetDelay_FixedInterval_ShouldReturnInterval()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.FixedInterval,
            RetryCount = 3,
            InitialInterval = TimeSpan.FromSeconds(5)
        };

        config.GetDelay(1).Should().Be(TimeSpan.FromSeconds(5));
        config.GetDelay(3).Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RetryPolicyConfiguration_GetDelay_Exponential_ShouldGrow()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.Exponential,
            RetryCount = 5,
            InitialInterval = TimeSpan.FromSeconds(1),
            ExponentialBase = 2.0,
            MaxInterval = TimeSpan.FromMinutes(10)
        };

        TimeSpan delay1 = config.GetDelay(1);
        TimeSpan delay2 = config.GetDelay(2);
        TimeSpan delay3 = config.GetDelay(3);

        delay1.Should().Be(TimeSpan.FromSeconds(1));
        delay2.Should().Be(TimeSpan.FromSeconds(2));
        delay3.Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void RetryPolicyConfiguration_GetDelay_ShouldCapAtMaxInterval()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.Exponential,
            RetryCount = 10,
            InitialInterval = TimeSpan.FromSeconds(1),
            ExponentialBase = 2.0,
            MaxInterval = TimeSpan.FromSeconds(10)
        };

        TimeSpan delay = config.GetDelay(10);
        delay.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void RetryPolicyConfiguration_GetDelay_CustomIntervals_ShouldUseMappedInterval()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.CustomIntervals,
            RetryCount = 3,
            Intervals = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)]
        };

        config.GetDelay(1).Should().Be(TimeSpan.FromSeconds(1));
        config.GetDelay(2).Should().Be(TimeSpan.FromSeconds(5));
        config.GetDelay(3).Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void RetryPolicyConfiguration_GetDelay_Incremental_ShouldIncrease()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.Incremental,
            RetryCount = 3,
            InitialInterval = TimeSpan.FromSeconds(1),
            IntervalIncrement = TimeSpan.FromSeconds(2),
            MaxInterval = TimeSpan.FromHours(1)
        };

        config.GetDelay(1).Should().Be(TimeSpan.FromSeconds(1));
        config.GetDelay(2).Should().Be(TimeSpan.FromSeconds(3));
        config.GetDelay(3).Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RetryPolicyConfiguration_GetDelay_AttemptBelowMin_ShouldClampTo1()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.FixedInterval,
            RetryCount = 3,
            InitialInterval = TimeSpan.FromSeconds(2)
        };

        config.GetDelay(0).Should().Be(TimeSpan.FromSeconds(2));
        config.GetDelay(-5).Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RetryPolicyConfiguration_GetDelay_AttemptAboveMax_ShouldClampToMax()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.FixedInterval,
            RetryCount = 3,
            InitialInterval = TimeSpan.FromSeconds(2)
        };

        config.GetDelay(100).Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RetryPolicyConfiguration_ShouldRetry_DefaultShouldRetryAll()
    {
        var config = new RetryPolicyConfiguration();

        config.ShouldRetry(new InvalidOperationException()).Should().BeTrue();
        config.ShouldRetry(new ArgumentNullException()).Should().BeTrue();
    }

    [Fact]
    public void RetryPolicyConfiguration_ShouldRetry_WithHandledExceptions_ShouldOnlyRetryHandled()
    {
        var config = new RetryPolicyConfiguration();
        config.HandledExceptions.Add(typeof(InvalidOperationException));

        config.ShouldRetry(new InvalidOperationException()).Should().BeTrue();
        config.ShouldRetry(new ArgumentNullException()).Should().BeFalse();
    }

    [Fact]
    public void RetryPolicyConfiguration_ShouldRetry_WithIgnoredExceptions_ShouldNotRetryIgnored()
    {
        var config = new RetryPolicyConfiguration();
        config.IgnoredExceptions.Add(typeof(ArgumentException));

        config.ShouldRetry(new InvalidOperationException()).Should().BeTrue();
        config.ShouldRetry(new ArgumentNullException()).Should().BeFalse();
    }

    [Fact]
    public void RetryPolicyConfiguration_WithJitter_DelaysShouldVary()
    {
        var config = new RetryPolicyConfiguration
        {
            RetryType = RetryType.FixedInterval,
            RetryCount = 5,
            InitialInterval = TimeSpan.FromSeconds(10),
            MaxInterval = TimeSpan.FromMinutes(5),
            EnableJitter = true,
            MaxJitterPercent = 50
        };

        // With jitter, delays should not always be exactly 10s
        var delays = Enumerable.Range(1, 20).Select(i => config.GetDelay(1)).ToList();
        delays.Should().AllSatisfy(d => d.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero));
    }

    #endregion

    #region CircuitBreakerPolicyConfiguration (public class, direct construction)

    [Fact]
    public void CircuitBreakerPolicyConfiguration_Defaults_ShouldHaveExpectedValues()
    {
        var config = new CircuitBreakerPolicyConfiguration();

        config.TrackingPeriod.Should().Be(TimeSpan.FromMinutes(1));
        config.TripThreshold.Should().Be(15);
        config.ActiveThreshold.Should().Be(10);
        config.ResetInterval.Should().Be(TimeSpan.FromMinutes(5));
        config.FailureRateThreshold.Should().Be(50);
        config.HalfOpenDuration.Should().Be(TimeSpan.FromSeconds(30));
        config.SuccessThreshold.Should().Be(3);
        config.HandledExceptions.Should().BeEmpty();
        config.IgnoredExceptions.Should().BeEmpty();
    }

    [Fact]
    public void CircuitBreakerPolicyConfiguration_ShouldCount_DefaultShouldCountAll()
    {
        var config = new CircuitBreakerPolicyConfiguration();

        config.ShouldCount(new InvalidOperationException()).Should().BeTrue();
        config.ShouldCount(new TimeoutException()).Should().BeTrue();
    }

    [Fact]
    public void CircuitBreakerPolicyConfiguration_ShouldCount_WithHandledExceptions_OnlyCountsHandled()
    {
        var config = new CircuitBreakerPolicyConfiguration();
        config.HandledExceptions.Add(typeof(TimeoutException));

        config.ShouldCount(new TimeoutException()).Should().BeTrue();
        config.ShouldCount(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void CircuitBreakerPolicyConfiguration_ShouldCount_WithIgnoredExceptions_DoesNotCountIgnored()
    {
        var config = new CircuitBreakerPolicyConfiguration();
        config.IgnoredExceptions.Add(typeof(ArgumentException));

        config.ShouldCount(new InvalidOperationException()).Should().BeTrue();
        config.ShouldCount(new ArgumentNullException()).Should().BeFalse();
    }

    [Fact]
    public void CircuitBreakerPolicyConfiguration_Callbacks_ShouldBeInvocable()
    {
        bool breakCalled = false;
        bool resetCalled = false;
        bool halfOpenCalled = false;

        var config = new CircuitBreakerPolicyConfiguration
        {
            OnBreak = (ex, ts) => breakCalled = true,
            OnReset = () => resetCalled = true,
            OnHalfOpen = () => halfOpenCalled = true
        };

        config.OnBreak!(new Exception(), TimeSpan.Zero);
        config.OnReset!();
        config.OnHalfOpen!();

        breakCalled.Should().BeTrue();
        resetCalled.Should().BeTrue();
        halfOpenCalled.Should().BeTrue();
    }

    [Fact]
    public void CircuitBreakerState_ShouldHaveThreeValues()
    {
        Enum.GetValues<CircuitBreakerState>().Should().HaveCount(3);
        Enum.IsDefined(CircuitBreakerState.Closed).Should().BeTrue();
        Enum.IsDefined(CircuitBreakerState.Open).Should().BeTrue();
        Enum.IsDefined(CircuitBreakerState.HalfOpen).Should().BeTrue();
    }

    #endregion

    #region RetryPolicyBuilder (only public builder method chaining)

    [Fact]
    public void RetryPolicyBuilder_Immediate_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.Immediate(5);
        act.Should().NotThrow();
    }

    [Fact]
    public void RetryPolicyBuilder_Interval_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.Interval(TimeSpan.FromSeconds(3), 4);
        act.Should().NotThrow();
    }

    [Fact]
    public void RetryPolicyBuilder_Intervals_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10));
        act.Should().NotThrow();
    }

    [Fact]
    public void RetryPolicyBuilder_Exponential_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2));
        act.Should().NotThrow();
    }

    [Fact]
    public void RetryPolicyBuilder_ExponentialWithMultiplier_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.ExponentialWithMultiplier(4, TimeSpan.FromMilliseconds(500), 3.0);
        act.Should().NotThrow();
    }

    [Fact]
    public void RetryPolicyBuilder_Incremental_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
        act.Should().NotThrow();
    }

    [Fact]
    public void RetryPolicyBuilder_WithJitter_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.Immediate(3).WithJitter(25);
        act.Should().NotThrow();
    }

    [Fact]
    public void RetryPolicyBuilder_Handle_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.Handle<InvalidOperationException>();
        act.Should().NotThrow();
    }

    [Fact]
    public void RetryPolicyBuilder_Ignore_ShouldNotThrow()
    {
        var builder = new RetryPolicyBuilder();
        Action act = () => builder.Ignore<ArgumentException>();
        act.Should().NotThrow();
    }

    #endregion

    #region CircuitBreakerPolicyBuilder (only public builder method chaining)

    [Fact]
    public void CircuitBreakerPolicyBuilder_TrackingPeriod_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.TrackingPeriod(TimeSpan.FromMinutes(2));
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_TripThreshold_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.TripThreshold(20);
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_ActiveThreshold_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.ActiveThreshold(5);
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_ResetInterval_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.ResetInterval(TimeSpan.FromMinutes(10));
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_FailureRateThreshold_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.FailureRateThreshold(60);
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_HalfOpenDuration_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.HalfOpenDuration(TimeSpan.FromMinutes(1));
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_SuccessThreshold_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.SuccessThreshold(5);
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_Handle_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.Handle<TimeoutException>();
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_Ignore_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.Ignore<ArgumentException>();
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_OnBreak_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.OnBreak((ex, ts) => { });
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_OnReset_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.OnReset(() => { });
        act.Should().NotThrow();
    }

    [Fact]
    public void CircuitBreakerPolicyBuilder_OnHalfOpen_ShouldNotThrow()
    {
        var builder = new CircuitBreakerPolicyBuilder();
        Action act = () => builder.OnHalfOpen(() => { });
        act.Should().NotThrow();
    }

    #endregion

    #region HostConfigurationBuilder

    [Fact]
    public void HostConfigurationBuilder_Username_ShouldSetUserName()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.Username("admin");

        opts.Configuration!.UserName.Should().Be("admin");
    }

    [Fact]
    public void HostConfigurationBuilder_Password_ShouldSetPassword()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.Password("secret");

        opts.Configuration!.Password.Should().Be("secret");
    }

    [Fact]
    public void HostConfigurationBuilder_VirtualHost_ShouldSetVHost()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.VirtualHost("/production");

        opts.Configuration!.VirtualHost.Should().Be("/production");
    }

    [Fact]
    public void HostConfigurationBuilder_HostName_ShouldSetHostName()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.HostName("rabbitmq.example.com");

        opts.Configuration!.HostName.Should().Be("rabbitmq.example.com");
    }

    [Fact]
    public void HostConfigurationBuilder_Port_ShouldSetPort()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.Port(5673);

        opts.Configuration!.Port.Should().Be(5673);
    }

    [Fact]
    public void HostConfigurationBuilder_RetryCount_ShouldSetRetryCount()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.RetryCount(7);

        opts.RetryCount.Should().Be(7);
    }

    [Fact]
    public void HostConfigurationBuilder_DispatchConsumersAsync_ShouldSetFlag()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.DispatchConsumersAsync(true);

        opts.DispatchConsumersAsync.Should().BeTrue();
    }

    [Fact]
    public void HostConfigurationBuilder_UseSsl_WithServerName_ShouldEnableSsl()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.UseSsl("rabbitmq.example.com");

        opts.Configuration!.Ssl.Should().NotBeNull();
        opts.Configuration.Ssl!.Enabled.Should().BeTrue();
        opts.Configuration.Ssl.ServerName.Should().Be("rabbitmq.example.com");
    }

    [Fact]
    public void HostConfigurationBuilder_UseSsl_WithAction_ShouldConfigureSsl()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.UseSsl(ssl =>
        {
            ssl.ServerName("secure.example.com");
            ssl.CertificatePath("/certs/client.pfx");
            ssl.AcceptablePolicyErrors(false);
        });

        opts.Configuration!.Ssl!.Enabled.Should().BeTrue();
        opts.Configuration.Ssl.ServerName.Should().Be("secure.example.com");
        opts.Configuration.Ssl.CertificatePath.Should().Be("/certs/client.pfx");
    }

    [Fact]
    public void HostConfigurationBuilder_Heartbeat_ShouldSetHeartbeat()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.Heartbeat(30);

        opts.Configuration!.RequestedHeartbeat.Should().Be(30);
    }

    [Fact]
    public void HostConfigurationBuilder_ConnectionTimeout_ShouldSetTimeout()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.ConnectionTimeout(60);

        opts.Configuration!.ConnectionTimeout.Should().Be(60);
    }

    [Fact]
    public void HostConfigurationBuilder_AutomaticRecoveryEnabled_ShouldSetFlag()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.AutomaticRecoveryEnabled(true);

        opts.Configuration!.AutomaticRecoveryEnabled.Should().BeTrue();
    }

    [Fact]
    public void HostConfigurationBuilder_ClientProvidedName_ShouldSetName()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.ClientProvidedName("my-service");

        opts.Configuration!.ClientProvidedName.Should().Be("my-service");
    }

    [Fact]
    public void HostConfigurationBuilder_NetworkRecoveryInterval_ShouldSetInterval()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder.NetworkRecoveryInterval(10);

        opts.Configuration!.NetworkRecoveryInterval.Should().Be(10);
    }

    [Fact]
    public void HostConfigurationBuilder_Chaining_ShouldAllowFluentSyntax()
    {
        var opts = new RabbitMQConnectionOptions();
        var builder = new HostConfigurationBuilder(opts);
        builder
            .HostName("localhost")
            .Port(5672)
            .Username("guest")
            .Password("guest")
            .VirtualHost("/")
            .RetryCount(3)
            .DispatchConsumersAsync();

        opts.Configuration!.HostName.Should().Be("localhost");
        opts.Configuration.Port.Should().Be(5672);
        opts.Configuration.UserName.Should().Be("guest");
        opts.RetryCount.Should().Be(3);
    }

    #endregion

    #region ConsumerConfiguration

    [Fact]
    public void ConsumerConfiguration_Defaults_ShouldHaveExpectedValues()
    {
        var config = new ConsumerConfiguration();

        config.ConcurrentMessageLimit.Should().Be(1);
        config.PrefetchCount.Should().Be(16);
        config.RetryAttempts.Should().Be(3);
        config.RetryDelay.Should().Be(TimeSpan.FromSeconds(1));
        config.UseExponentialBackoff.Should().BeTrue();
        config.Durable.Should().BeTrue();
        config.AutoDelete.Should().BeFalse();
        config.Exclusive.Should().BeFalse();
        config.EnablePriorityQueue.Should().BeFalse();
        config.MaxPriority.Should().Be(10);
        config.RequeueOnFailure.Should().BeFalse();
        config.QueueName.Should().BeNull();
        config.Exchange.Should().BeNull();
        config.RoutingKey.Should().BeNull();
        config.MessageTtl.Should().BeNull();
        config.ConsumerTag.Should().BeNull();
        config.ProcessingTimeout.Should().BeNull();
    }

    [Fact]
    public void ConsumerConfiguration_CustomValues_ShouldBeSetCorrectly()
    {
        var config = new ConsumerConfiguration
        {
            ConcurrentMessageLimit = 10,
            PrefetchCount = 32,
            RetryAttempts = 5,
            QueueName = "my-queue",
            Exchange = "my-exchange",
            RoutingKey = "my.route",
            DeadLetterExchange = "my-dlx",
            DeadLetterRoutingKey = "dlq.route",
            MessageTtl = 60000,
            ConsumerTag = "consumer-1",
            ProcessingTimeout = TimeSpan.FromSeconds(30),
            RequeueOnFailure = true,
            AutoDelete = true,
            Exclusive = true
        };

        config.ConcurrentMessageLimit.Should().Be(10);
        config.PrefetchCount.Should().Be(32);
        config.RetryAttempts.Should().Be(5);
        config.QueueName.Should().Be("my-queue");
        config.Exchange.Should().Be("my-exchange");
        config.RoutingKey.Should().Be("my.route");
        config.DeadLetterExchange.Should().Be("my-dlx");
        config.DeadLetterRoutingKey.Should().Be("dlq.route");
        config.MessageTtl.Should().Be(60000);
        config.ConsumerTag.Should().Be("consumer-1");
        config.ProcessingTimeout.Should().Be(TimeSpan.FromSeconds(30));
        config.RequeueOnFailure.Should().BeTrue();
        config.AutoDelete.Should().BeTrue();
        config.Exclusive.Should().BeTrue();
    }

    #endregion

    #region RabbitMQConfigurationBuilder

    [Fact]
    public void RabbitMQConfigurationBuilder_NullServices_ShouldThrow()
    {
        Action act = () => new RabbitMQConfigurationBuilder(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_Host_WithConnectionString_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.Host("amqp://guest:guest@localhost:5672");

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_Host_WithHostAndPort_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.Host("localhost", 5672);

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_Host_WithAction_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.Host(h => h.HostName("rabbitmq.local").RetryCount(5));

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_UseRetry_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.UseRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1)));

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_UseCircuitBreaker_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.UseCircuitBreaker(cb => cb.TripThreshold(20));

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_UseInMemoryOutbox_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.UseInMemoryOutbox();

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_UseInMemoryOutboxWithOptions_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.UseInMemoryOutbox(opts => opts.BatchSize = 50);

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_ConfigureEndpoints_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.ConfigureEndpoints();

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_ConfigureClient_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.ConfigureClient(opts =>
        {
            opts.Exchange = "custom-exchange";
            opts.Durable = false;
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_AddRequestClient_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.AddRequestClient<TestOrderCommand, TestOrderResponse>();

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_AddRequestClientWithOptions_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        Action act = () => builder.AddRequestClient<TestOrderCommand, TestOrderResponse>(opts =>
        {
            opts.TimeoutMilliseconds = 30000;
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_Services_ShouldNotBeNull()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        builder.Services.Should().NotBeNull();
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void RabbitMQConfigurationBuilder_Chaining_ShouldReturnSameBuilder()
    {
        var services = new ServiceCollection();
        var builder = new RabbitMQConfigurationBuilder(services);

        var result = builder
            .Host("localhost", 5672)
            .UseRetry(r => r.Immediate(3))
            .UseCircuitBreaker(cb => cb.TripThreshold(10))
            .ConfigureEndpoints();

        result.Should().BeSameAs(builder);
    }

    #endregion

    #region SslConfigurationBuilder

    [Fact]
    public void SslConfigurationBuilder_ServerName_ShouldSetServerName()
    {
        var config = new RabbitMQSslConfiguration { Enabled = true };
        var builder = new SslConfigurationBuilder(config);
        builder.ServerName("ssl.example.com");

        config.ServerName.Should().Be("ssl.example.com");
    }

    [Fact]
    public void SslConfigurationBuilder_CertificatePath_ShouldSetPath()
    {
        var config = new RabbitMQSslConfiguration { Enabled = true };
        var builder = new SslConfigurationBuilder(config);
        builder.CertificatePath("/certs/cert.pfx");

        config.CertificatePath.Should().Be("/certs/cert.pfx");
    }

    [Fact]
    public void SslConfigurationBuilder_CertificatePassphrase_ShouldSetPassphrase()
    {
        var config = new RabbitMQSslConfiguration { Enabled = true };
        var builder = new SslConfigurationBuilder(config);
        builder.CertificatePassphrase("mypassword");

        config.CertificatePassphrase.Should().Be("mypassword");
    }

    [Fact]
    public void SslConfigurationBuilder_AcceptablePolicyErrors_ShouldSetFlag()
    {
        var config = new RabbitMQSslConfiguration { Enabled = true };
        var builder = new SslConfigurationBuilder(config);
        builder.AcceptablePolicyErrors(true);

        config.AcceptablePolicyErrors.Should().BeTrue();
    }

    #endregion
}
