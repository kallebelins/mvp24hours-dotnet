//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;

namespace Mvp24Hours.Extensions;

/// <summary>
/// Extension methods for registering EF Core interceptors in the DI container.
/// </summary>
public static class EFCoreInterceptorExtensions
{
    /// <summary>
    /// Adds the soft delete interceptor that converts physical deletes to soft deletes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultUser">Default user when no user provider is available.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The interceptor converts <c>EntityState.Deleted</c> to <c>EntityState.Modified</c> for entities
    /// implementing <see cref="Mvp24Hours.Core.Contract.Domain.Entity.ISoftDeletable"/> or
    /// <c>ISoftDeletable&lt;TUserId&gt;</c>, setting <c>IsDeleted</c>, <c>DeletedAt</c> and <c>DeletedBy</c>.
    /// It does not handle the legacy <c>IEntityDateLog</c> / <c>IEntityLog&lt;T&gt;</c> interfaces, which are
    /// stamped by the deprecated <c>Mvp24HoursContext.ApplyLogRules</c> and by <c>Repository.Remove</c>.
    /// </para>
    /// <para>
    /// <see cref="ICurrentUserProvider"/> and <see cref="IClock"/> are optional: when they are not
    /// registered, the interceptor falls back to <paramref name="defaultUser"/> and <c>DateTime.UtcNow</c>.
    /// </para>
    /// <para>
    /// <strong>Two follow-up steps are required.</strong> EF Core does not discover interceptors from the
    /// application container, so resolve it inside <c>AddDbContext</c>, and apply the read-side query
    /// filter in <c>OnModelCreating</c> — the interceptor only changes writes:
    /// <code>
    /// services.AddMvp24HoursEFCoreSoftDeleteInterceptor();
    ///
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .AddInterceptors(sp.GetRequiredService&lt;SoftDeleteInterceptor&gt;());
    /// });
    ///
    /// // in AppDbContext:
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ApplySoftDeleteGlobalFilter();
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddMvp24HoursEFCoreSoftDeleteInterceptor(
        this IServiceCollection services,
        string defaultUser = "System")
    {
        services.AddScoped(sp =>
            new SoftDeleteInterceptor(
                sp.GetService<ICurrentUserProvider>(),
                sp.GetService<IClock>(),
                defaultUser));

        return services;
    }
}
