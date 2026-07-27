using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Fixtures;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Integration;

[Collection(KeycloakTestConstants.CollectionName)]
[Trait("Category", "Integration")]
public sealed class AdminUserIntegrationTests(KeycloakFixture fixture)
{
    [Fact]
    public async Task UserService_ShouldCompleteCrudLifecycle()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using ServiceProvider services = fixture.CreateServiceProvider();
        IKeycloakUserService users =
            services.GetRequiredService<IKeycloakUserService>();
        string suffix = Guid.NewGuid().ToString("N");
        string username = $"integration-{suffix}";
        string? userId = null;

        try
        {
            var created = await users.CreateUserAsync(new CreateUserRequest
            {
                Username = username,
                Email = $"{username}@example.test",
                FirstName = "Integration",
                LastName = "User",
                EmailVerified = true,
                TemporaryPassword = "Initial-Pass-123!",
                TemporaryPasswordRequired = false,
                Attributes = new()
                {
                    ["source"] = ["integration-test"]
                }
            });
            created.HasErrors.Should().BeFalse();
            userId = created.Data;
            userId.Should().NotBeNullOrWhiteSpace();

            var loaded = await users.GetUserByIdAsync(userId!);
            loaded.HasErrors.Should().BeFalse();
            loaded.Data!.Username.Should().Be(username);
            loaded.Data.Email.Should().Be($"{username}@example.test");

            var updated = await users.UpdateUserAsync(new UpdateUserRequest
            {
                UserId = userId!,
                Username = username,
                Email = $"{username}@example.test",
                FirstName = "Updated",
                LastName = "User",
                Enabled = true,
                EmailVerified = true
            });
            updated.HasErrors.Should().BeFalse();
            updated.Data.Should().BeTrue();

            var password = await users.ResetPasswordAsync(new ResetPasswordRequest
            {
                UserId = userId!,
                Password = "Updated-Pass-123!",
                Temporary = false
            });
            password.HasErrors.Should().BeFalse();

            var disabled = await users.SetUserEnabledAsync(userId!, false);
            disabled.HasErrors.Should().BeFalse();
            (await users.GetUserByIdAsync(userId!)).Data!.Enabled.Should().BeFalse();

            var search = await users.GetUsersAsync(username: username);
            search.HasErrors.Should().BeFalse();
            search.Data.Should().ContainSingle(user => user.Id == userId);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var deleted = await users.DeleteUserAsync(userId);
                deleted.HasErrors.Should().BeFalse();
                deleted.Data.Should().BeTrue();
            }
        }
    }
}
