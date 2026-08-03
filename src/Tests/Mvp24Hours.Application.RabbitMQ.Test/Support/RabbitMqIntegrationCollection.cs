//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Application.RabbitMQ.Test.Support;

[CollectionDefinition(Name)]
public sealed class RabbitMqIntegrationCollection : ICollectionFixture<RabbitMqIntegrationFixture>
{
    public const string Name = "RabbitMqIntegration";
}
