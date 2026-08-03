using Moq;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakRoleServiceTests
{
    [Fact]
    public async Task GetRealmRoleByNameAsync_WithMissingName_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<RoleRepresentation> result =
            await service.GetRealmRoleByNameAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRealmRoleAsync_WithMissingName_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<RoleRepresentation> result = await service.CreateRealmRoleAsync(
            new RoleRepresentation());

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteRealmRoleAsync_WithMissingName_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<bool> result = await service.DeleteRealmRoleAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetClientRoleByNameAsync_WithMissingValues_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<RoleRepresentation> result =
            await service.GetClientRoleByNameAsync(string.Empty, string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task AddRealmRoleCompositesAsync_WithEmptyComposites_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<bool> result = await service.AddRealmRoleCompositesAsync(
            "admin",
            []);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task CreateClientRoleAsync_WithMissingClientUuid_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<RoleRepresentation> result = await service.CreateClientRoleAsync(
            string.Empty,
            new RoleRepresentation { Name = "role" });

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateClientRoleAsync_WithMissingClientUuid_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<bool> result = await service.UpdateClientRoleAsync(
            string.Empty,
            "role",
            new RoleRepresentation { Name = "role" });

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteClientRoleAsync_WithMissingValues_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<bool> result = await service.DeleteClientRoleAsync(string.Empty, string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetClientRolesAsync_WithMissingClientUuid_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<IReadOnlyList<RoleRepresentation>> result =
            await service.GetClientRolesAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveRealmRoleCompositesAsync_WithEmptyComposites_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<bool> result = await service.RemoveRealmRoleCompositesAsync("admin", []);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetRealmRoleCompositesAsync_WithMissingName_ShouldReturnValidationError()
    {
        KeycloakRoleService service = new(CreateClient());

        IBusinessResult<IReadOnlyList<RoleRepresentation>> result =
            await service.GetRealmRoleCompositesAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    private static KeycloakAdminHttpClient CreateClient()
    {
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(value => value.CreateClient("KeycloakAdmin"))
            .Returns(new HttpClient { BaseAddress = new Uri("https://identity.example/") });
        return new KeycloakAdminHttpClient(
            factory.Object,
            Microsoft.Extensions.Options.Options.Create(
                new Core.Options.KeycloakAdminOptions
                {
                    AdminBaseUrl = "https://identity.example/admin/realms/test",
                    Realm = "test",
                    ClientId = "admin-client",
                    ClientSecret = "secret"
                }));
    }
}
