using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authorization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;

namespace Mvp24Hours.Identity.Keycloak.Infrastructure.Extensions
{
    public static class HttpClientExtensions
    {
        public static HttpClient PropagateAuthorization(this HttpClient client, IServiceCollection services)
        {
            // access the DI container
            var serviceProvider = services.BuildServiceProvider();

            // Find the HttpContextAccessor service
            var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();

            // Get the bearer token from the request context (header)
            string authorizationValue = httpContextAccessor.GetAuthorization();

            // Add authorization if found
            if (authorizationValue != null)
                client.DefaultRequestHeaders.Add("Authorization", authorizationValue);

            return client;
        }

        public static string GetAuthorization(this IHttpContextAccessor httpContextAccessor)
        {
            // Get the bearer token from the request context (header)
            string authorizationValue = null;

            if (httpContextAccessor?.HttpContext != null && httpContextAccessor.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                authorizationValue = httpContextAccessor.HttpContext.Request
                    .Headers["Authorization"]
                    .FirstOrDefault();
            }

            return authorizationValue;
        }

        public static UserToken GetUserToken(this IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService<IHttpContextAccessor>()?.GetUserToken();
        }

        public static UserToken GetUserToken(this IHttpContextAccessor httpContextAccessor)
        {
            string authorizationValue = httpContextAccessor?.GetAuthorization();

            if (authorizationValue.HasValue())
            {
                string jwt = authorizationValue.ToLower().StartsWith("bearer ")
                    ? authorizationValue.Split(' ').ElementAtOrDefault(1)
                    : authorizationValue;

                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);

                token.Payload.TryGetValue("name", out object name);
                token.Payload.TryGetValue("preferred_username", out object preferred_username);
                token.Payload.TryGetValue("email", out object email);
                token.Payload.TryGetValue("sid", out object sid);
                token.Payload.TryGetValue("scope", out object scope);
                token.Payload.TryGetValue("email_verified", out object email_verified);
                token.Payload.TryGetValue("session_state", out object session_state);
                token.Payload.TryGetValue("azp", out object azp);
                token.Payload.TryGetValue("sub", out object sub);

                token.Payload.TryGetValue("allowed-origins", out object allowed_origins);
                token.Payload.TryGetValue("realm_access", out object realm_access);
                token.Payload.TryGetValue("resource_access", out object resource_access);

                List<string> allowedOrigins = null;
                if (allowed_origins != null)
                {
                    allowedOrigins = ((JArray)allowed_origins)?.ToObject<List<string>>();
                }

                List<string> realmRoles = null;
                if (realm_access != null)
                {
                    realmRoles = (((dynamic)realm_access).roles as JArray)?.ToObject<List<string>>();
                }

                List<string> resourceRoles = null;
                if (resource_access != null)
                {
                    resourceRoles = (((dynamic)resource_access).account?.roles as JArray)?.ToObject<List<string>>();
                }

                return new UserToken
                {
                    Id = sub.ToString().ToGuid(),
                    Name = name?.ToString(),
                    PreferredUserName = preferred_username?.ToString(),
                    Email = email?.ToString(),
                    EmailVerified = Convert.ToBoolean(email_verified),
                    Scope = scope?.ToString(),
                    SessionId = sid?.ToString()?.ToGuid(),
                    SessionState = session_state?.ToString()?.ToGuid(),
                    AuthorizedParty = azp?.ToString(),
                    AllowedOrigins = allowedOrigins,
                    RealmRoles = realmRoles,
                    ResourceRoles = resourceRoles
                };
            }

            return null;
        }

        public static Guid? GetUserId(this IServiceProvider serviceProvider)
        {
            return serviceProvider?.GetService<IHttpContextAccessor>()?.GetUserId();
        }

        public static Guid? GetUserId(this IHttpContextAccessor httpContextAccessor)
        {
            string authorizationValue = httpContextAccessor?.GetAuthorization();

            if (authorizationValue.HasValue())
            {
                string jwt = authorizationValue.ToLower().StartsWith("bearer ")
                    ? authorizationValue.Split(' ').ElementAtOrDefault(1)
                    : authorizationValue;

                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);

                token.Payload.TryGetValue("sub", out object sub);
                return sub?.ToString()?.ToGuid();
            }
            return null;
        }
    }
}
