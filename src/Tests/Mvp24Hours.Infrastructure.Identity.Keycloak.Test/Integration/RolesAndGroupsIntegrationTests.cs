using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Fixtures;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Integration;

[Collection(KeycloakTestConstants.CollectionName)]
[Trait("Category", "Integration")]
public sealed class RolesAndGroupsIntegrationTests(KeycloakFixture fixture)
{
    [Fact]
    public async Task Services_ShouldManageRoleGroupAndMembership()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        await using ServiceProvider services = fixture.CreateServiceProvider();
        IKeycloakRoleService roles =
            services.GetRequiredService<IKeycloakRoleService>();
        IKeycloakGroupService groups =
            services.GetRequiredService<IKeycloakGroupService>();
        IKeycloakUserService users =
            services.GetRequiredService<IKeycloakUserService>();
        string suffix = Guid.NewGuid().ToString("N");
        string roleName = $"integration-role-{suffix}";
        string groupName = $"integration-group-{suffix}";
        string username = $"integration-member-{suffix}";
        string? groupId = null;
        string? userId = null;

        try
        {
            var createdRole = await roles.CreateRealmRoleAsync(new RoleRepresentation
            {
                Name = roleName,
                Description = "Created by an integration test"
            });
            createdRole.HasErrors.Should().BeFalse();
            createdRole.Data!.Name.Should().Be(roleName);

            var loadedRole = await roles.GetRealmRoleByNameAsync(roleName);
            loadedRole.HasErrors.Should().BeFalse();
            loadedRole.Data!.Id.Should().NotBeNullOrWhiteSpace();

            var createdGroup = await groups.CreateGroupAsync(new CreateGroupRequest
            {
                Name = groupName,
                Attributes = new() { ["source"] = ["integration-test"] }
            });
            createdGroup.HasErrors.Should().BeFalse();
            groupId = createdGroup.Data;

            var loadedGroup = await groups.GetGroupByIdAsync(groupId!);
            loadedGroup.HasErrors.Should().BeFalse();
            loadedGroup.Data!.Name.Should().Be(groupName);

            var createdUser = await users.CreateUserAsync(new CreateUserRequest
            {
                Username = username,
                Enabled = true
            });
            createdUser.HasErrors.Should().BeFalse();
            userId = createdUser.Data;

            var added = await users.AddUserToGroupAsync(new AddUserToGroupRequest
            {
                UserId = userId!,
                GroupId = groupId!
            });
            added.HasErrors.Should().BeFalse();
            (await groups.GetGroupMembersAsync(groupId!)).Data
                .Should()
                .ContainSingle(user => user.Id == userId);

            var assigned = await users.AssignRealmRolesAsync(new AssignRolesRequest
            {
                UserId = userId!,
                Roles = [loadedRole.Data]
            });
            assigned.HasErrors.Should().BeFalse();
            (await users.GetRealmRolesAsync(userId!)).Data
                .Should()
                .Contain(role => role.Name == roleName);

            (await users.RemoveRealmRolesAsync(new AssignRolesRequest
            {
                UserId = userId!,
                Roles = [loadedRole.Data]
            })).HasErrors.Should().BeFalse();
            (await users.RemoveUserFromGroupAsync(userId!, groupId!))
                .HasErrors.Should().BeFalse();
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await users.DeleteUserAsync(userId);
            }

            if (!string.IsNullOrWhiteSpace(groupId))
            {
                await groups.DeleteGroupAsync(groupId);
            }

            await roles.DeleteRealmRoleAsync(roleName);
        }
    }
}
