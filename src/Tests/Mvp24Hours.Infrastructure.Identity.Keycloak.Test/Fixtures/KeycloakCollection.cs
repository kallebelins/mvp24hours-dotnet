namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Fixtures;

[CollectionDefinition(KeycloakTestConstants.CollectionName, DisableParallelization = true)]
public sealed class KeycloakCollection : ICollectionFixture<KeycloakFixture>
{
}
