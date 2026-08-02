using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Fixtures;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Integration;

[Collection(KeycloakTestConstants.CollectionName)]
[Trait("Category", "Integration")]
public sealed class UserSyncIntegrationTests(KeycloakFixture fixture)
{
    [Fact]
    public async Task SyncService_ShouldLoadKeycloakUserIntoLocalStore()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using ServiceProvider services = fixture.CreateServiceProvider();
        InMemoryUserSyncService sync = new(
            services.GetRequiredService<IKeycloakUserService>(),
            NullLogger<UserKeycloakSyncService>.Instance);
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        IBusinessResult<object> result =
            await sync.SyncLocalUserFromKeycloakAsync(userId);

        result.HasErrors.Should().BeFalse();
        (await sync.GetAnyLocalUserByIdAsync(userId)).Data.Should().BeTrue();
        sync.Users[userId].PreferredUserName.Should().Be(
            KeycloakTestConstants.Username);
        sync.Users[userId].Email.Should().Be("test-user@mvp24hours.dev");
        sync.Users[userId].Name.Should().Be("Test User");
    }

    private sealed class InMemoryUserSyncService(
        IKeycloakUserService keycloakUserService,
        ILogger<UserKeycloakSyncService> logger)
        : UserKeycloakSyncService(keycloakUserService, logger)
    {
        public Dictionary<Guid, UserToken> Users { get; } = [];

        public override Task<IBusinessResult<bool>> GetAnyLocalUserByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(BusinessResult.Success(Users.ContainsKey(id)));
        }

        public override Task<IBusinessResult<object>> GetLocalIdByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Users.ContainsKey(id)
                    ? BusinessResult.Success<object>(id)
                    : BusinessResult.Failure<object>("User not found."));
        }

        public override Task<IBusinessResult<object>> GetLocalIdByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            Guid? id = Users
                .FirstOrDefault(item => string.Equals(
                    item.Value.Email,
                    email,
                    StringComparison.OrdinalIgnoreCase))
                .Key;
            return Task.FromResult(
                id != Guid.Empty
                    ? BusinessResult.Success<object>(id.Value)
                    : BusinessResult.Failure<object>("User not found."));
        }

        public override Task<IBusinessResult<object>> CreateOrUpdateLocalUserAsync(
            UserToken dto,
            CancellationToken cancellationToken = default)
        {
            if (dto.Id is not Guid id)
            {
                return Task.FromResult(
                    BusinessResult.Failure<object>("User id is required."));
            }

            Users[id] = dto;
            return Task.FromResult(BusinessResult.Success<object>(id));
        }
    }
}
