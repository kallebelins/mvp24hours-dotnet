using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Identity.Keycloak.Core.Contract.Logic;
using Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authentication;
using Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;
using Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;
using Mvp24Hours.Identity.Keycloak.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TokenClient = Mvp24Hours.Identity.Keycloak.Infrastructure.Clients.TokenClient;

namespace Mvp24Hours.Identity.Keycloak.WebAPI.Extensions
{
    public static class KeycloakExtensions
    {
        public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection("JwtBearer").Get<JwtBearerOptions>();
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = jwtOptions.Authority;
                    options.Audience = jwtOptions.Audience;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateLifetime = true,

                        NameClaimType = JwtClaimTypes.PreferredUserName,
                        RoleClaimType = JwtClaimTypes.Role
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = (context) =>
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-onmessagereceived", $"token:{context.Token}");
                            return Task.CompletedTask;
                        },
                        OnChallenge = (context) =>
                        {
                            if (context.AuthenticateFailure != null)
                                TelemetryHelper.Execute(TelemetryLevels.Error, "jwt-onchallenge-failure", context.AuthenticateFailure);
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = (context) =>
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Error, "jwt-onauthenticationfailed-failure", context.Exception);
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers.Add("Token-Expired", "true");
                            }
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = async (context) =>
                        {
                            var localUserSvc = context.HttpContext.RequestServices.GetService<IUserKeycloakService>();
                            if (localUserSvc != null)
                            {
                                TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-ontokenvalidated-get-integration-start");
                                var user = context.HttpContext.RequestServices.GetUserToken();
                                var anyUserBo = await localUserSvc.GetAnyLocalUserById((Guid)user.Id);
                                if (!anyUserBo.GetDataValue() || anyUserBo.HasErrors)
                                {
                                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-ontokenvalidated-get-integration-create-user-start");
                                    var localUserBo = await localUserSvc.CreateOrUpdateLocalUser(user);
                                    if (localUserBo.HasErrors)
                                    {
                                        TelemetryHelper.Execute(TelemetryLevels.Error, "jwt-ontokenvalidated-get-integration-create-user-failure", localUserBo.Messages.FirstOrDefault());
                                    }
                                    else
                                    {
                                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-ontokenvalidated-get-integration-user", $"data: {localUserBo.GetDataValue()}");
                                    }
                                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-ontokenvalidated-get-integration-create-user-end");
                                }
                                TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-ontokenvalidated-get-integration-end");
                            }

                            var sbClaims = new StringBuilder();
                            foreach (var c in context.Principal.Claims)
                            {
                                if (!string.IsNullOrEmpty(c.Value))
                                    sbClaims.Append($"{c.Type}; ");
                            }
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-ontokenvalidated", $"claims:{sbClaims}");
                        }
                    };
                });

            services.AddTransient<IClaimsTransformation>(_ =>
                new KeycloakRolesClaimsTransformation(JwtClaimTypes.Role, "realm_access"));

            return services;
        }

        public static IServiceCollection AddKeycloakAuthorization(this IServiceCollection services,
            Dictionary<string, List<string>> roles = null,
            Dictionary<string, List<DecisionRequirement>> decisionRequiriments = null,
            Dictionary<string, List<RptRequirement>> rptRequiriments = null,
            Dictionary<string, List<IAuthorizationRequirement>> resourceRequiriments = null
        )
        {
            services.AddAuthorization(options =>
            {
                #region Role Claim
                if (roles.AnyOrNotNull())
                {
                    foreach (var key in roles.Keys)
                    {
                        if (roles[key].AnyOrNotNull())
                        {
                            options.AddPolicy(key, policy => policy.RequireClaim(JwtClaimTypes.Role, roles[key].ToArray()));
                        }
                    }
                }
                #endregion

                #region Decision Requirements

                if (decisionRequiriments.AnyOrNotNull())
                {
                    services.AddSingleton<IAuthorizationHandler, DecisionRequirementHandler>();
                    foreach (var key in decisionRequiriments.Keys)
                    {
                        if (decisionRequiriments[key].AnyOrNotNull())
                        {
                            options.AddPolicy(key
                                , builder => builder.AddRequirements(decisionRequiriments[key].ToArray())
                            );
                        }
                    }
                }

                #endregion

                #region Rpt Requirements

                if (rptRequiriments.AnyOrNotNull())
                {
                    services.AddSingleton<IAuthorizationHandler, RptRequirementHandler>();
                    foreach (var key in rptRequiriments.Keys)
                    {
                        if (rptRequiriments[key].AnyOrNotNull())
                        {
                            options.AddPolicy(key
                                , builder => builder.AddRequirements(rptRequiriments[key].ToArray())
                            );
                        }
                    }
                }

                #endregion

                #region Resource Based RPT requirement

                if (resourceRequiriments.AnyOrNotNull())
                {
                    foreach (var key in resourceRequiriments.Keys)
                    {
                        if (resourceRequiriments[key].AnyOrNotNull())
                        {
                            options.AddPolicy(key, builder => builder.RequireAssertion(async context =>
                            {
                                if (context.Resource is not HttpContext httpContext)
                                {
                                    return false;
                                }
                                var authorizationService =
                                    httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
                                var policy =
                                    new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                                        .AddRequirements(resourceRequiriments[key].ToArray())
                                        .Build();

                                var result = await authorizationService.AuthorizeAsync(httpContext.User, policy);

                                return result.Succeeded;
                            }));
                        }
                    }
                }

                #endregion
            });

            return services;
        }

        public static IServiceCollection AddKeycloakPolicies(this IServiceCollection services, Assembly assembly, Dictionary<string, List<IAuthorizationRequirement>> resourceRequiriments = null)
        {
            services.AddKeycloakAuthorization(assembly.GetRolePolicies(), assembly.GetDecisionPolicies(), assembly.GetRptPolicies(), resourceRequiriments);
            return services;
        }

        public static IServiceCollection AddKeycloakService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<KeycloakService>(client =>
            {
                client.BaseAddress = new Uri(configuration["KeycloakResourceUrl"]);
                client.PropagateAuthorization(services);
            });
            services.AddHttpClient<TokenClient>();
            services.AddSingleton(_ =>
                configuration.GetSection("ClientCredentialsTokenRequest").Get<ClientCredentialsTokenRequest>());
            return services;
        }

        private static Dictionary<string, List<string>> GetRolePolicies(this Assembly assembly)
        {
            var result = new Dictionary<string, List<string>>();

            var typesFromClasses = assembly
                .DefinedTypes.Where(x => x.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any());

            var typesFromMethods = assembly
                .DefinedTypes.Where(x => x.GetMethods().Any(m => m.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any()));

            foreach (var type in typesFromClasses.Concat(typesFromMethods))
            {
                result.Add(type.Name, new List<string>());

                var authClass = type
                    .GetCustomAttributes(true).OfType<AuthorizeAttribute>().FirstOrDefault();

                if (authClass != null && authClass.Roles.HasValue())
                {
                    var policies = authClass.Roles.Split(',');
                    foreach (var policy in policies)
                    {
                        result[type.Name].Add(policy.Trim());
                    }
                }

                foreach (var method in type.GetMethods())
                {
                    var authMethod = method
                        .GetCustomAttributes(true).OfType<AuthorizeAttribute>().FirstOrDefault();

                    if (authMethod != null && authMethod.Roles.HasValue())
                    {
                        var policies = authMethod.Roles.Split(',');
                        foreach (var policy in policies)
                        {
                            result[type.Name].Add(policy.Trim());
                        }
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, List<RptRequirement>> GetRptPolicies(this Assembly assembly)
        {
            var result = new Dictionary<string, List<RptRequirement>>();

            var typesFromAssemblies = assembly
                .DefinedTypes.Where(x => x.GetMethods().Any(m => m.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any()));

            foreach (var type in typesFromAssemblies)
            {
                result.Add(type.Name, new List<RptRequirement>());

                foreach (var method in type.GetMethods())
                {
                    var auth = method
                        .GetCustomAttributes(true).OfType<AuthorizeAttribute>().FirstOrDefault();

                    if (auth != null && auth.Policy.HasValue() && auth.Policy.Contains("#"))
                    {
                        var policies = auth.Policy.Split(',');
                        foreach (var policy in policies)
                        {
                            var values = policy.Split('#');
                            if (values.Length == 2)
                            {
                                result[type.Name].Add(new RptRequirement(values[0].Trim(), values[1].Trim()));
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, List<DecisionRequirement>> GetDecisionPolicies(this Assembly assembly)
        {
            var result = new Dictionary<string, List<DecisionRequirement>>();

            var typesFromAssemblies = assembly.DefinedTypes.Where(x => x.GetMethods().Any(m => m.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any()));

            foreach (var type in typesFromAssemblies)
            {
                result.Add(type.Name, new List<DecisionRequirement>());

                foreach (var method in type.GetMethods())
                {
                    var auth = method.GetCustomAttributes(true).OfType<AuthorizeAttribute>().FirstOrDefault();
                    if (auth != null && auth.Policy.HasValue() && auth.Policy.Contains("#"))
                    {
                        var policies = auth.Policy.Split(',');
                        foreach (var policy in policies)
                        {
                            var values = policy.Split('#');
                            if (values.Length == 2)
                            {
                                result[type.Name].Add(new DecisionRequirement(values[0].Trim(), values[1].Trim()));
                            }
                        }
                    }
                }
            }

            return result;
        }

    }
}
