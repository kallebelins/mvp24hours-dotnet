using System.Net;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Clients;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakUserServiceTests
{
    [Fact]
    public async Task GetUsersAsync_WithInvalidPaging_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());

        IBusinessResult<IReadOnlyList<UserRepresentation>> result =
            await service.GetUsersAsync(first: -1, max: 0);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsersAsync_WithSearchFilters_ShouldReturnUsers()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"id":"u1","username":"alice"}]""")
        };
        KeycloakUserService service = new(CreateClient(response));

        IBusinessResult<IReadOnlyList<UserRepresentation>> result =
            await service.GetUsersAsync(search: "ali", username: "alice", email: "alice@example.com");

        result.HasErrors.Should().BeFalse();
        result.Data.Should().HaveCount(1);
        result.Data![0].Username.Should().Be("alice");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithMissingId_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());

        IBusinessResult<UserRepresentation> result =
            await service.GetUserByIdAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":"u1","username":"alice"}""")
        };
        KeycloakUserService service = new(CreateClient(response));

        IBusinessResult<UserRepresentation> result = await service.GetUserByIdAsync("u1");

        result.HasErrors.Should().BeFalse();
        result.Data!.Username.Should().Be("alice");
    }

    [Fact]
    public async Task CreateUserAsync_WithNullRequest_ShouldThrow()
    {
        KeycloakUserService service = new(CreateClient());

        Func<Task> act = async () => await service.CreateUserAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidRequest_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());
        CreateUserRequest request = new()
        {
            Username = string.Empty
        };

        IBusinessResult<string> result = await service.CreateUserAsync(request);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_WithTemporaryPassword_ShouldReturnCreatedUserId()
    {
        using HttpResponseMessage response = new(HttpStatusCode.Created)
        {
            Headers = { Location = new Uri("https://identity.example/admin/realms/test/users/new-user-id") }
        };
        KeycloakUserService service = new(CreateClient(response));
        CreateUserRequest request = new()
        {
            Username = "alice",
            Email = "alice@example.com",
            TemporaryPassword = "Temp123!",
            TemporaryPasswordRequired = true
        };

        IBusinessResult<string> result = await service.CreateUserAsync(request);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be("new-user-id");
    }

    [Fact]
    public async Task UpdateUserAsync_WithInvalidRequest_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());
        UpdateUserRequest request = new()
        {
            UserId = string.Empty,
            Username = "alice"
        };

        IBusinessResult<bool> result = await service.UpdateUserAsync(request);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserAsync_WithValidRequest_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));
        UpdateUserRequest request = new()
        {
            UserId = "u1",
            Username = "alice",
            Email = "alice@example.com",
            Enabled = true
        };

        IBusinessResult<bool> result = await service.UpdateUserAsync(request);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_WithMissingId_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());

        IBusinessResult<bool> result = await service.DeleteUserAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_WithValidId_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));

        IBusinessResult<bool> result = await service.DeleteUserAsync("u1");

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserEnabledAsync_WithMissingId_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());

        IBusinessResult<bool> result = await service.SetUserEnabledAsync(string.Empty, true);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserEnabledAsync_WithValidId_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));

        IBusinessResult<bool> result = await service.SetUserEnabledAsync("u1", false);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidRequest_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());
        ResetPasswordRequest request = new()
        {
            UserId = string.Empty,
            Password = string.Empty
        };

        IBusinessResult<bool> result = await service.ResetPasswordAsync(request);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidRequest_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));
        ResetPasswordRequest request = new()
        {
            UserId = "u1",
            Password = "NewPass123!"
        };

        IBusinessResult<bool> result = await service.ResetPasswordAsync(request);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetRealmRolesAsync_WithMissingId_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());

        IBusinessResult<IReadOnlyList<RoleRepresentation>> result =
            await service.GetRealmRolesAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetRealmRolesAsync_WithValidId_ShouldReturnRoles()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"id":"r1","name":"admin"}]""")
        };
        KeycloakUserService service = new(CreateClient(response));

        IBusinessResult<IReadOnlyList<RoleRepresentation>> result =
            await service.GetRealmRolesAsync("u1");

        result.HasErrors.Should().BeFalse();
        result.Data![0].Name.Should().Be("admin");
    }

    [Fact]
    public async Task AssignRealmRolesAsync_WithInvalidRequest_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());
        AssignRolesRequest request = new()
        {
            UserId = string.Empty,
            Roles = []
        };

        IBusinessResult<bool> result = await service.AssignRealmRolesAsync(request);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task AssignRealmRolesAsync_WithClientUuid_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());
        AssignRolesRequest request = new()
        {
            UserId = "u1",
            ClientUuid = "client-1",
            Roles = [new RoleRepresentation { Name = "viewer" }]
        };

        IBusinessResult<bool> result = await service.AssignRealmRolesAsync(request);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task AssignRealmRolesAsync_WithValidRequest_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));
        AssignRolesRequest request = new()
        {
            UserId = "u1",
            Roles = [new RoleRepresentation { Name = "viewer" }]
        };

        IBusinessResult<bool> result = await service.AssignRealmRolesAsync(request);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveRealmRolesAsync_WithValidRequest_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));
        AssignRolesRequest request = new()
        {
            UserId = "u1",
            Roles = [new RoleRepresentation { Name = "viewer" }]
        };

        IBusinessResult<bool> result = await service.RemoveRealmRolesAsync(request);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetClientRolesAsync_WithMissingIds_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());

        IBusinessResult<IReadOnlyList<RoleRepresentation>> result =
            await service.GetClientRolesAsync(string.Empty, string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetClientRolesAsync_WithValidIds_ShouldReturnRoles()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"id":"r2","name":"editor"}]""")
        };
        KeycloakUserService service = new(CreateClient(response));

        IBusinessResult<IReadOnlyList<RoleRepresentation>> result =
            await service.GetClientRolesAsync("u1", "client-uuid");

        result.HasErrors.Should().BeFalse();
        result.Data![0].Name.Should().Be("editor");
    }

    [Fact]
    public async Task AssignClientRolesAsync_WithMissingClientUuid_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());
        AssignRolesRequest request = new()
        {
            UserId = "u1",
            Roles = [new RoleRepresentation { Name = "editor" }]
        };

        IBusinessResult<bool> result = await service.AssignClientRolesAsync(request);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task AssignClientRolesAsync_WithValidRequest_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));
        AssignRolesRequest request = new()
        {
            UserId = "u1",
            ClientUuid = "client-uuid",
            Roles = [new RoleRepresentation { Name = "editor" }]
        };

        IBusinessResult<bool> result = await service.AssignClientRolesAsync(request);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveClientRolesAsync_WithValidRequest_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));
        AssignRolesRequest request = new()
        {
            UserId = "u1",
            ClientUuid = "client-uuid",
            Roles = [new RoleRepresentation { Name = "editor" }]
        };

        IBusinessResult<bool> result = await service.RemoveClientRolesAsync(request);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserGroupsAsync_WithMissingId_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());

        IBusinessResult<IReadOnlyList<GroupRepresentation>> result =
            await service.GetUserGroupsAsync(string.Empty);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserGroupsAsync_WithValidId_ShouldReturnGroups()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"id":"g1","name":"staff"}]""")
        };
        KeycloakUserService service = new(CreateClient(response));

        IBusinessResult<IReadOnlyList<GroupRepresentation>> result =
            await service.GetUserGroupsAsync("u1");

        result.HasErrors.Should().BeFalse();
        result.Data![0].Name.Should().Be("staff");
    }

    [Fact]
    public async Task AddUserToGroupAsync_WithInvalidRequest_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());
        AddUserToGroupRequest request = new()
        {
            UserId = string.Empty,
            GroupId = string.Empty
        };

        IBusinessResult<bool> result = await service.AddUserToGroupAsync(request);

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task AddUserToGroupAsync_WithValidRequest_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));
        AddUserToGroupRequest request = new()
        {
            UserId = "u1",
            GroupId = "g1"
        };

        IBusinessResult<bool> result = await service.AddUserToGroupAsync(request);

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromGroupAsync_WithMissingIds_ShouldReturnValidationError()
    {
        KeycloakUserService service = new(CreateClient());

        IBusinessResult<bool> result = await service.RemoveUserFromGroupAsync(string.Empty, "g1");

        result.HasErrors.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromGroupAsync_WithValidIds_ShouldReturnSuccess()
    {
        using HttpResponseMessage response = new(HttpStatusCode.NoContent);
        KeycloakUserService service = new(CreateClient(response));

        IBusinessResult<bool> result = await service.RemoveUserFromGroupAsync("u1", "g1");

        result.HasErrors.Should().BeFalse();
        result.Data.Should().BeTrue();
    }

    private static KeycloakAdminHttpClient CreateClient(HttpResponseMessage? response = null)
    {
        Mock<HttpMessageHandler> handler = new();
        if (response is not null)
        {
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        HttpClient httpClient = new(handler.Object)
        {
            BaseAddress = new Uri("https://identity.example/admin/realms/test/")
        };
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(value => value.CreateClient("KeycloakAdmin"))
            .Returns(httpClient);
        return new KeycloakAdminHttpClient(
            factory.Object,
            Options.Create(
                new KeycloakAdminOptions
                {
                    AdminBaseUrl = "https://identity.example/admin/realms/test",
                    Realm = "test",
                    ClientId = "admin-client",
                    ClientSecret = "secret"
                }));
    }
}
