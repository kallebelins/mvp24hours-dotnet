//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Test query that returns a user.
/// </summary>
public class GetUserQuery : IMediatorQuery<UserDto?>
{
    public int UserId { get; set; }
}

/// <summary>
/// Handler for GetUserQuery.
/// </summary>
public class GetUserQueryHandler : IMediatorQueryHandler<GetUserQuery, UserDto?>
{
    private static readonly List<UserDto> Users = new()
    {
        new UserDto { Id = 1, Name = "John Doe", Email = "john@example.com" },
        new UserDto { Id = 2, Name = "Jane Smith", Email = "jane@example.com" },
        new UserDto { Id = 3, Name = "Bob Wilson", Email = "bob@example.com" }
    };

    public Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = Users.FirstOrDefault(u => u.Id == request.UserId);
        return Task.FromResult(user);
    }
}

/// <summary>
/// Query that returns a list of users.
/// </summary>
public class GetAllUsersQuery : IMediatorQuery<List<UserDto>>
{
    public int? Limit { get; set; }
}

/// <summary>
/// Handler for GetAllUsersQuery.
/// </summary>
public class GetAllUsersQueryHandler : IMediatorQueryHandler<GetAllUsersQuery, List<UserDto>>
{
    private static readonly List<UserDto> Users = new()
    {
        new UserDto { Id = 1, Name = "John Doe", Email = "john@example.com" },
        new UserDto { Id = 2, Name = "Jane Smith", Email = "jane@example.com" },
        new UserDto { Id = 3, Name = "Bob Wilson", Email = "bob@example.com" }
    };

    public Task<List<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var result = request.Limit.HasValue 
            ? Users.Take(request.Limit.Value).ToList() 
            : Users.ToList();
        return Task.FromResult(result);
    }
}

/// <summary>
/// DTO for user data.
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

