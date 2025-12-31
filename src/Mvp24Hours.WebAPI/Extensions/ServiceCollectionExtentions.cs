//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.WebAPI.Binders;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Exceptions;
using Mvp24Hours.WebAPI.Filters;
using Mvp24Hours.WebAPI.Filters.Swagger;
using Mvp24Hours.WebAPI.Models;
using Mvp24Hours.WebAPI.Http;
using Mvp24Hours.WebAPI.Services;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using MvpProblemDetailsOptions = Mvp24Hours.WebAPI.Configuration.MvpProblemDetailsOptions;
using MvpResponseCachingOptions = Mvp24Hours.WebAPI.Configuration.ResponseCachingOptions;
using MvpApiVersioningOptions = Mvp24Hours.WebAPI.Configuration.ApiVersioningOptions;
using Microsoft.Extensions.Options;

namespace Mvp24Hours.WebAPI.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceCollectionExtentions
    {
        /// <summary>
        /// Adds IHttpContextAccessor and IActionContextAccessor
        /// </summary>
        public static IServiceCollection AddMvp24HoursWebEssential(this IServiceCollection services)
        {
            services.AddSingleton(services);
            if (!services.Exists<IHttpContextAccessor>())
            {
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            }
            if (!services.Exists<IActionContextAccessor>())
            {
                services.AddSingleton<IActionContextAccessor, ActionContextAccessor>()
                    .AddScoped<IUrlHelper>(x => x.GetRequiredService<IUrlHelperFactory>()
                    .GetUrlHelper(x.GetRequiredService<IActionContextAccessor>().ActionContext));
            }
            return services;
        }

        /// <summary>
        /// Add json serialization
        /// </summary>
        public static IServiceCollection AddMvp24HoursWebJson(this IServiceCollection services, JsonSerializerSettings jsonSerializerSettings = null)
        {
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = (jsonSerializerSettings ?? JsonHelper.JsonDefaultSettings).ContractResolver;
                    options.SerializerSettings.Converters = (jsonSerializerSettings ?? JsonHelper.JsonDefaultSettings).Converters;
                    options.SerializerSettings.DateFormatHandling = (jsonSerializerSettings ?? JsonHelper.JsonDefaultSettings).DateFormatHandling;
                    options.SerializerSettings.DateFormatString = (jsonSerializerSettings ?? JsonHelper.JsonDefaultSettings).DateFormatString;
                    options.SerializerSettings.NullValueHandling = (jsonSerializerSettings ?? JsonHelper.JsonDefaultSettings).NullValueHandling;
                    options.SerializerSettings.ReferenceLoopHandling = (jsonSerializerSettings ?? JsonHelper.JsonDefaultSettings).ReferenceLoopHandling;
                });
            return services;
        }

        /// <summary>
        /// Registers custom model binders for Mvp24Hours types.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method registers model binders for:
        /// <list type="bullet">
        /// <item><see cref="DateOnly"/> and nullable DateOnly</item>
        /// <item><see cref="TimeOnly"/> and nullable TimeOnly</item>
        /// <item><see cref="DateTimeOffset"/> and nullable DateTimeOffset</item>
        /// <item>Strongly-typed IDs (<see cref="Mvp24Hours.Core.ValueObjects.EntityId{TSelf, TValue}"/>)</item>
        /// <item>Paging criteria (<see cref="Mvp24Hours.Core.Contract.ValueObjects.Logic.IPagingCriteria"/>)</item>
        /// </list>
        /// </para>
        /// <para>
        /// These binders enable automatic model binding from query strings, route parameters,
        /// and form data in both Minimal APIs and MVC controllers.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // In Program.cs
        /// builder.Services.AddMvp24HoursModelBinders();
        /// 
        /// // Now you can use strongly-typed IDs in endpoints:
        /// app.MapGet("/customers/{customerId}", (CustomerId customerId) => { ... });
        /// 
        /// // And DateOnly/TimeOnly:
        /// app.MapGet("/events", (DateOnly date, TimeOnly time) => { ... });
        /// 
        /// // And IPagingCriteria:
        /// app.MapGet("/products", (IPagingCriteria paging) => { ... });
        /// </code>
        /// </example>
        public static IServiceCollection AddMvp24HoursModelBinders(this IServiceCollection services)
        {
            services.Configure<MvcOptions>(options =>
            {
                options.ModelBinderProviders.Insert(0, new Mvp24HoursModelBinderProvider());
            });

            return services;
        }

        /// <summary>
        /// Add configuration for GzipCompressionProvider
        /// </summary>
        public static IServiceCollection AddMvp24HoursWebGzip(this IServiceCollection services, bool enableForHttps = false)
        {
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = enableForHttps;
                options.Providers.Add<GzipCompressionProvider>();
            });

            return services;
        }

        /// <summary>
        /// Add configuration for Swagger
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>DEPRECATED:</b> Use <c>AddMvp24HoursNativeOpenApi()</c> instead for .NET 9+ native OpenAPI support.
        /// This method uses Swashbuckle which is still supported but the native OpenAPI is preferred.
        /// </para>
        /// </remarks>
        [Obsolete("Use AddMvp24HoursNativeOpenApi() for .NET 9+ native OpenAPI support. Swashbuckle is still supported but native OpenAPI is preferred. Will be removed in a future major version.")]
        public static IServiceCollection AddMvp24HoursWebSwagger(this IServiceCollection services,
            string title, string version = "v1", string xmlCommentsFileName = null,
            bool enableExample = false, SwaggerAuthorizationScheme oAuthScheme = SwaggerAuthorizationScheme.None,
            IEnumerable<Type> authTypes = null)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version });

                if (enableExample)
                {
                    c.ExampleFilters();
                }

                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                if (oAuthScheme == SwaggerAuthorizationScheme.Bearer)
                {
                    BearerBuilder(authTypes, c);
                }
                else if (oAuthScheme == SwaggerAuthorizationScheme.Basic)
                {
                    BasicBuilder(authTypes, c);
                }

                if (oAuthScheme != SwaggerAuthorizationScheme.None && authTypes != null)
                {
                    c.OperationFilter<AuthResponsesOperationFilter>(authTypes);
                }

                // Set the comments path for the Swagger JSON and UI.
                if (!string.IsNullOrEmpty(xmlCommentsFileName))
                {
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFileName);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }
                }
            });

            if (enableExample)
            {
                services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());
            }

            return services;
        }

        /// <summary>
        /// Adds API versioning support with multiple strategies (URL, Header, Query String).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures API versioning with support for:
        /// <list type="bullet">
        /// <item>URL path versioning (e.g., /api/v1/users)</item>
        /// <item>HTTP header versioning (e.g., X-API-Version: 1.0)</item>
        /// <item>Query string versioning (e.g., ?api-version=1.0)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage with default options
        /// services.AddMvp24HoursApiVersioning();
        /// 
        /// // With custom configuration
        /// services.AddMvp24HoursApiVersioning(options =>
        /// {
        ///     options.DefaultApiVersion = new ApiVersion(1, 0);
        ///     options.AssumeDefaultVersionWhenUnspecified = true;
        ///     options.ReportApiVersions = true;
        ///     options.Strategy = ApiVersioningStrategy.UrlPath | ApiVersioningStrategy.Header;
        ///     options.SupportedApiVersions.Add(new ApiVersion(1, 0));
        ///     options.SupportedApiVersions.Add(new ApiVersion(2, 0));
        ///     options.DeprecatedApiVersions.Add(new ApiVersion(1, 0));
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure versioning options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursApiVersioning(
            this IServiceCollection services,
            Action<MvpApiVersioningOptions>? configureOptions = null)
        {
            var options = new MvpApiVersioningOptions();
            configureOptions?.Invoke(options);

            // Configure API versioning
            services.AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = options.DefaultApiVersion;
                opt.AssumeDefaultVersionWhenUnspecified = options.AssumeDefaultVersionWhenUnspecified;
                opt.ReportApiVersions = options.ReportApiVersions;

                // Configure version readers based on strategy
                var readers = new List<IApiVersionReader>();

                if (options.Strategy.HasFlag(ApiVersioningStrategy.UrlPath))
                {
                    readers.Add(new UrlSegmentApiVersionReader());
                }

                if (options.Strategy.HasFlag(ApiVersioningStrategy.Header))
                {
                    readers.Add(new HeaderApiVersionReader(options.HeaderName));
                }

                if (options.Strategy.HasFlag(ApiVersioningStrategy.QueryString))
                {
                    readers.Add(new QueryStringApiVersionReader(options.QueryStringParameterName));
                }

                if (readers.Count > 0)
                {
                    opt.ApiVersionReader = ApiVersionReader.Combine(readers.ToArray());
                }
            });

            // Add API versioning API explorer for Swagger
            services.AddVersionedApiExplorer(setup =>
            {
                setup.GroupNameFormat = "'v'VVV";
                setup.SubstituteApiVersionInUrl = true;
            });

            return services;
        }

        /// <summary>
        /// Adds Swagger/OpenAPI documentation with support for multiple API versions and OpenAPI 3.1.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures Swagger/OpenAPI with:
        /// <list type="bullet">
        /// <item>OpenAPI 3.1 specification</item>
        /// <item>Multiple API versions support</item>
        /// <item>Automatic examples from XML comments</item>
        /// <item>Deprecation warnings</item>
        /// <item>ReDoc integration (optional)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Requires prior call to <see cref="AddMvp24HoursApiVersioning"/> for versioning support.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage
        /// services.AddMvp24HoursApiVersioning();
        /// services.AddMvp24HoursSwaggerWithVersioning(options =>
        /// {
        ///     options.Title = "My API";
        ///     options.Description = "API Description";
        ///     options.Versions.Add(new SwaggerVersionInfo
        ///     {
        ///         Version = "v1",
        ///         Title = "API v1",
        ///         Description = "Version 1 of the API"
        ///     });
        ///     options.Versions.Add(new SwaggerVersionInfo
        ///     {
        ///         Version = "v2",
        ///         Title = "API v2",
        ///         Description = "Version 2 of the API",
        ///         IsDeprecated = false
        ///     });
        ///     options.EnableReDoc = true;
        ///     options.EnableExamples = true;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure Swagger options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// <b>DEPRECATED:</b> Use <c>AddMvp24HoursNativeOpenApiWithVersions()</c> instead for .NET 9+ native OpenAPI support.
        /// This method uses Swashbuckle which is still supported but the native OpenAPI is preferred.
        /// </para>
        /// </remarks>
        [Obsolete("Use AddMvp24HoursNativeOpenApiWithVersions() for .NET 9+ native OpenAPI support. Swashbuckle is still supported but native OpenAPI is preferred. Will be removed in a future major version.")]
        public static IServiceCollection AddMvp24HoursSwaggerWithVersioning(
            this IServiceCollection services,
            Action<SwaggerOptions> configureOptions)
        {
            var options = new SwaggerOptions();
            configureOptions(options);

            // Store options for later use
            services.AddSingleton(options);
            services.Configure<SwaggerOptions>(opt =>
            {
                opt.Title = options.Title;
                opt.Description = options.Description;
                opt.Versions = options.Versions;
                opt.Contact = options.Contact;
                opt.License = options.License;
            });

            // Register the configuration that will run when Swagger is generated
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerGenOptions>();

            services.AddSwaggerGen(c =>
            {
                // Configure OpenAPI 3.1
                c.SwaggerGeneratorOptions.SwaggerDocs.Clear();

                // Add operation filters
                c.OperationFilter<DeprecationOperationFilter>();
                
                if (options.EnableExamples)
                {
                    c.OperationFilter<ExamplesOperationFilter>();
                    c.ExampleFilters();
                }

                // Add authorization
                if (options.AuthorizationScheme == SwaggerAuthorizationScheme.Bearer)
                {
                    BearerBuilder(options.AuthorizationTypes, c);
                }
                else if (options.AuthorizationScheme == SwaggerAuthorizationScheme.Basic)
                {
                    BasicBuilder(options.AuthorizationTypes, c);
                }

                if (options.AuthorizationScheme != SwaggerAuthorizationScheme.None && options.AuthorizationTypes != null)
                {
                    c.OperationFilter<AuthResponsesOperationFilter>(options.AuthorizationTypes);
                }

                // Include XML comments
                if (!string.IsNullOrEmpty(options.XmlCommentsFileName))
                {
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, options.XmlCommentsFileName);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath);
                    }
                }

                // Resolve conflicting actions
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
            });

            if (options.EnableExamples)
            {
                services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());
            }

            return services;
        }

        private static void BasicBuilder(IEnumerable<Type> authTypes, SwaggerGenOptions c)
        {
            c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
            {
                Description = @"Authorization header using the Basic scheme. \r\n\r\n 
                          Enter 'Basic' [space] and then your token in the text input below.
                          \r\n\r\nExample: 'Basic 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Basic"
            });

            if (authTypes == null)
            {
                c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Basic"
                            },
                        },
                        new List<string>()
                    }
                });
            }
        }

        private static void BearerBuilder(IEnumerable<Type> authTypes, SwaggerGenOptions c)
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                          Enter 'Bearer' [space] and then your token in the text input below.
                          \r\n\r\nExample: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            if (authTypes == null)
            {
                c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                {
                    new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new List<string>()
                    }
                });
            }
        }

        /// <summary>
        /// Add configuration cors middleware
        /// </summary>
        public static IServiceCollection AddMvp24HoursWebCors(this IServiceCollection services, Action<CorsOptions> options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<CorsOptions>(options => { });
            }
            return services;
        }

        /// <summary>
        /// Add configuration exception middleware
        /// </summary>
        public static IServiceCollection AddMvp24HoursWebExceptions(this IServiceCollection services, Action<ExceptionOptions> options = null)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<ExceptionOptions>(options => { });
            }
            return services;
        }

        /// <summary>
        /// Adds ProblemDetails support with exception mapping for RFC 7807 compliant error responses.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures the services required for producing ProblemDetails responses
        /// following RFC 7807. It includes:
        /// <list type="bullet">
        /// <item>Default exception to HTTP status code mapping for Mvp24Hours exceptions</item>
        /// <item>Validation exception handling with detailed error information</item>
        /// <item>Configurable options for customization</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursProblemDetails"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage
        /// services.AddMvp24HoursProblemDetails();
        /// 
        /// // With custom options
        /// services.AddMvp24HoursProblemDetails(options =>
        /// {
        ///     options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
        ///     options.ProblemTypeBaseUri = "https://api.example.com/errors";
        ///     options.ExceptionMappings[typeof(CustomException)] = HttpStatusCode.BadRequest;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure ProblemDetails options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursProblemDetails(
            this IServiceCollection services,
            Action<MvpProblemDetailsOptions>? configureOptions = null)
        {
            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<MvpProblemDetailsOptions>(_ => { });
            }

            // Register mappers
            services.TryAddSingleton<DefaultExceptionToProblemDetailsMapper>();
            services.TryAddSingleton<ValidationProblemDetailsMapper>();
            services.TryAddSingleton<IExceptionToProblemDetailsMapper, DefaultExceptionToProblemDetailsMapper>();

            // Register filters
            services.TryAddScoped<ModelStateValidationFilter>();
            services.TryAddScoped<ProblemDetailsResultFilter>();

            return services;
        }

        /// <summary>
        /// Adds ProblemDetails support with custom exception mappings.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This overload allows you to specify additional exception type to HTTP status code mappings.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursProblemDetails(mappings =>
        /// {
        ///     mappings[typeof(MyCustomException)] = HttpStatusCode.BadRequest;
        ///     mappings[typeof(MyOtherException)] = HttpStatusCode.Conflict;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureMappings">Action to configure exception mappings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursProblemDetails(
            this IServiceCollection services,
            Action<Dictionary<Type, HttpStatusCode>> configureMappings)
        {
            var mappings = new Dictionary<Type, HttpStatusCode>();
            configureMappings(mappings);

            return services.AddMvp24HoursProblemDetails(options =>
            {
                foreach (var mapping in mappings)
                {
                    options.ExceptionMappings[mapping.Key] = mapping.Value;
                }
            });
        }

        /// <summary>
        /// Adds a custom exception to ProblemDetails mapper.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method to register custom mappers for specific exception types
        /// that require specialized handling beyond the default mapping.
        /// </para>
        /// </remarks>
        /// <typeparam name="TMapper">The type of the custom mapper.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursExceptionMapper<TMapper>(this IServiceCollection services)
            where TMapper : class, IExceptionToProblemDetailsMapper
        {
            services.AddSingleton<IExceptionToProblemDetailsMapper, TMapper>();
            return services;
        }

        /// <summary>
        /// Configures MVC to use ModelStateValidation filter and disable automatic 400 responses.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures the API behavior options to:
        /// <list type="bullet">
        /// <item>Disable the automatic 400 response for invalid model state</item>
        /// <item>Add the ModelStateValidationFilter globally</item>
        /// </list>
        /// </para>
        /// <para>
        /// This allows the custom filter to produce ProblemDetails responses instead
        /// of the default ASP.NET Core validation responses.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursModelStateValidation(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddControllers(options =>
            {
                options.Filters.Add<ModelStateValidationFilter>();
            });

            return services;
        }

        /// <summary>
        /// Adds all ProblemDetails features including filters and middleware support.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that combines:
        /// <list type="bullet">
        /// <item><see cref="AddMvp24HoursProblemDetails(IServiceCollection, Action{MvpProblemDetailsOptions}?)"/></item>
        /// <item><see cref="AddMvp24HoursModelStateValidation"/></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursProblemDetailsAll(options =>
        /// {
        ///     options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure ProblemDetails options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursProblemDetailsAll(
            this IServiceCollection services,
            Action<MvpProblemDetailsOptions>? configureOptions = null)
        {
            services.AddMvp24HoursProblemDetails(configureOptions);
            services.AddMvp24HoursModelStateValidation();

            return services;
        }

        #region Request Context and Correlation

        /// <summary>
        /// Adds request context services for Correlation/Causation ID propagation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures the services required for distributed tracing context:
        /// <list type="bullet">
        /// <item>Correlation ID extraction and generation</item>
        /// <item>Causation ID tracking for event chains</item>
        /// <item>Request ID generation for unique request identification</item>
        /// <item>Integration with CQRS module's IRequestContext</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursRequestContext"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage
        /// services.AddMvp24HoursRequestContext();
        /// 
        /// // With custom options
        /// services.AddMvp24HoursRequestContext(options =>
        /// {
        ///     options.CorrelationIdHeader = "X-Correlation-ID";
        ///     options.CausationIdHeader = "X-Causation-ID";
        ///     options.IncludeInResponse = true;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure context options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRequestContext(
            this IServiceCollection services,
            Action<RequestContextOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<RequestContextOptions>(_ => { });
            }

            // Register IHttpContextAccessor if not already registered
            if (!services.Exists<IHttpContextAccessor>())
            {
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            }

            // Register the correlation context provider for non-web scenarios
            services.TryAddSingleton<ICorrelationContextProvider, AsyncLocalCorrelationContextProvider>();

            // Register the CorrelationIdHandler for HttpClient propagation
            services.TryAddTransient<CorrelationIdHandler>();

            return services;
        }

        /// <summary>
        /// Adds the CorrelationIdHandler to an HttpClientBuilder for context propagation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This extension method adds the <see cref="CorrelationIdHandler"/> to an HttpClient
        /// so that Correlation ID, Causation ID, and Tenant ID are automatically propagated
        /// to outgoing HTTP requests.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// builder.Services.AddHttpClient("MyApi", client =>
        /// {
        ///     client.BaseAddress = new Uri("https://api.example.com");
        /// })
        /// .AddMvp24HoursCorrelationIdHandler();
        /// </code>
        /// </example>
        /// <param name="builder">The HttpClient builder.</param>
        /// <returns>The HttpClient builder for chaining.</returns>
        public static IHttpClientBuilder AddMvp24HoursCorrelationIdHandler(this IHttpClientBuilder builder)
        {
            return builder.AddHttpMessageHandler<CorrelationIdHandler>();
        }

        #endregion

        #region Request Logging and Telemetry

        /// <summary>
        /// Adds request/response logging services with configurable options.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures structured logging for HTTP requests and responses with:
        /// <list type="bullet">
        /// <item>Configurable logging levels (Basic, Standard, Detailed, Full)</item>
        /// <item>Automatic masking of sensitive headers and body properties</item>
        /// <item>Request/response body capture with size limits</item>
        /// <item>Slow request detection and warning</item>
        /// <item>Path exclusion patterns</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursRequestLogging"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage
        /// services.AddMvp24HoursRequestLogging();
        /// 
        /// // With custom options
        /// services.AddMvp24HoursRequestLogging(options =>
        /// {
        ///     options.LoggingLevel = RequestLoggingLevel.Standard;
        ///     options.LogRequestHeaders = true;
        ///     options.LogSlowRequests = true;
        ///     options.SlowRequestThresholdMs = 3000;
        ///     options.SensitiveHeaders.Add("X-Custom-Secret");
        ///     options.ExcludedPaths.Add("/api/internal/*");
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure logging options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRequestLogging(
            this IServiceCollection services,
            Action<RequestLoggingOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<RequestLoggingOptions>(_ => { });
            }

            services.TryAddSingleton<IRequestLogger, DefaultRequestLogger>();

            return services;
        }

        /// <summary>
        /// Adds a custom request logger implementation.
        /// </summary>
        /// <typeparam name="TLogger">The type of the custom logger.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure logging options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRequestLogging<TLogger>(
            this IServiceCollection services,
            Action<RequestLoggingOptions>? configureOptions = null)
            where TLogger : class, IRequestLogger
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<RequestLoggingOptions>(_ => { });
            }

            services.AddSingleton<IRequestLogger, TLogger>();

            return services;
        }

        /// <summary>
        /// Adds request telemetry services for OpenTelemetry-compatible tracing and metrics.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures telemetry collection for HTTP requests including:
        /// <list type="bullet">
        /// <item>Distributed tracing using Activity API</item>
        /// <item>Request/response metrics (counters, histograms, gauges)</item>
        /// <item>Correlation ID propagation</item>
        /// <item>User and tenant context enrichment</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>OpenTelemetry Integration:</strong>
        /// Configure OpenTelemetry to include Mvp24Hours WebAPI activities and metrics:
        /// <code>
        /// builder.Services.AddOpenTelemetry()
        ///     .WithTracing(builder => builder.AddSource("Mvp24Hours.WebAPI"))
        ///     .WithMetrics(builder => builder.AddMeter("Mvp24Hours.WebAPI"));
        /// </code>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage
        /// services.AddMvp24HoursRequestTelemetry();
        /// 
        /// // With custom options
        /// services.AddMvp24HoursRequestTelemetry(options =>
        /// {
        ///     options.EnableTracing = true;
        ///     options.EnableMetrics = true;
        ///     options.EnrichWithUser = true;
        ///     options.EnrichWithTenant = true;
        ///     options.ExcludedPaths.Add("/api/internal/*");
        ///     options.CustomTags["environment"] = "production";
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure telemetry options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRequestTelemetry(
            this IServiceCollection services,
            Action<RequestTelemetryOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<RequestTelemetryOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds both request logging and telemetry services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that combines:
        /// <list type="bullet">
        /// <item><see cref="AddMvp24HoursRequestLogging(IServiceCollection, Action{RequestLoggingOptions}?)"/></item>
        /// <item><see cref="AddMvp24HoursRequestTelemetry(IServiceCollection, Action{RequestTelemetryOptions}?)"/></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursRequestObservability(
        ///     logging => 
        ///     {
        ///         logging.LoggingLevel = RequestLoggingLevel.Standard;
        ///         logging.LogSlowRequests = true;
        ///     },
        ///     telemetry =>
        ///     {
        ///         telemetry.EnableTracing = true;
        ///         telemetry.EnableMetrics = true;
        ///     });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureLogging">Optional action to configure logging options.</param>
        /// <param name="configureTelemetry">Optional action to configure telemetry options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRequestObservability(
            this IServiceCollection services,
            Action<RequestLoggingOptions>? configureLogging = null,
            Action<RequestTelemetryOptions>? configureTelemetry = null)
        {
            services.AddMvp24HoursRequestLogging(configureLogging);
            services.AddMvp24HoursRequestTelemetry(configureTelemetry);

            return services;
        }

        #endregion

        #region Security

        /// <summary>
        /// Adds security headers middleware services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures security headers including:
        /// <list type="bullet">
        /// <item><strong>HSTS</strong> - Forces HTTPS connections</item>
        /// <item><strong>CSP</strong> - Content Security Policy</item>
        /// <item><strong>X-Frame-Options</strong> - Clickjacking protection</item>
        /// <item><strong>X-Content-Type-Options</strong> - MIME sniffing protection</item>
        /// <item><strong>X-XSS-Protection</strong> - Legacy XSS protection</item>
        /// <item><strong>Referrer-Policy</strong> - Controls referrer information</item>
        /// <item><strong>Permissions-Policy</strong> - Controls browser features</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursSecurityHeaders"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursSecurityHeaders(options =>
        /// {
        ///     options.EnableHsts = true;
        ///     options.HstsMaxAgeSeconds = 31536000;
        ///     options.EnableContentSecurityPolicy = true;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure security headers options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursSecurityHeaders(
            this IServiceCollection services,
            Action<Configuration.SecurityHeadersOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.SecurityHeadersOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds API Key authentication middleware services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures API Key authentication with support for:
        /// <list type="bullet">
        /// <item>Header-based API key</item>
        /// <item>Query string API key (optional)</item>
        /// <item>Multiple valid API keys</item>
        /// <item>Custom validators</item>
        /// <item>Scope-based authorization</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursApiKeyAuthentication"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursApiKeyAuthentication(options =>
        /// {
        ///     options.ApiKeys.Add("my-secret-api-key");
        ///     options.HeaderName = "X-Api-Key";
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure API key options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursApiKeyAuthentication(
            this IServiceCollection services,
            Action<Configuration.ApiKeyAuthenticationOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.ApiKeyAuthenticationOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds request size limiting middleware services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures request body size limits to prevent DoS attacks:
        /// <list type="bullet">
        /// <item>Global default size limit</item>
        /// <item>Per-endpoint size limits</item>
        /// <item>Per-content-type size limits</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursRequestSizeLimit"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursRequestSizeLimit(options =>
        /// {
        ///     options.DefaultMaxBodySize = 10 * 1024 * 1024; // 10MB
        ///     options.EndpointLimits["/api/upload/*"] = 100 * 1024 * 1024; // 100MB
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure size limit options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRequestSizeLimit(
            this IServiceCollection services,
            Action<Configuration.RequestSizeLimitOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.RequestSizeLimitOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds IP filtering middleware services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures IP-based access control with support for:
        /// <list type="bullet">
        /// <item>Whitelist mode - only allow specified IPs</item>
        /// <item>Blacklist mode - block specified IPs</item>
        /// <item>CIDR notation for IP ranges</item>
        /// <item>Path-specific IP rules</item>
        /// <item>Proxy-aware IP extraction</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursIpFiltering"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursIpFiltering(options =>
        /// {
        ///     options.Enabled = true;
        ///     options.Mode = IpFilteringMode.Whitelist;
        ///     options.WhitelistedIps.Add("192.168.1.0/24");
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure IP filtering options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursIpFiltering(
            this IServiceCollection services,
            Action<Configuration.IpFilteringOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.IpFilteringOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds anti-forgery (CSRF) protection middleware services for SPAs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures CSRF protection using the double-submit cookie pattern:
        /// <list type="bullet">
        /// <item>Sets token in a cookie readable by JavaScript</item>
        /// <item>Validates token from custom header matches cookie</item>
        /// <item>Optionally registers a token endpoint</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursAntiForgery"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursAntiForgery(options =>
        /// {
        ///     options.CookieName = "XSRF-TOKEN";
        ///     options.HeaderName = "X-XSRF-TOKEN";
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure anti-forgery options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursAntiForgery(
            this IServiceCollection services,
            Action<Configuration.AntiForgeryOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.AntiForgeryOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds input sanitization middleware services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures input validation and sanitization to protect against:
        /// <list type="bullet">
        /// <item>XSS (Cross-Site Scripting) attacks</item>
        /// <item>SQL Injection patterns</item>
        /// <item>Path Traversal attempts</item>
        /// <item>Command Injection patterns</item>
        /// <item>LDAP Injection patterns</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursInputSanitization"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursInputSanitization(options =>
        /// {
        ///     options.Mode = SanitizationMode.Validate;
        ///     options.EnableXssSanitization = true;
        ///     options.EnableSqlInjectionDetection = true;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure sanitization options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursInputSanitization(
            this IServiceCollection services,
            Action<Configuration.InputSanitizationOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.InputSanitizationOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds all security middleware services with a single call.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that registers all security services:
        /// <list type="bullet">
        /// <item>Security Headers</item>
        /// <item>Request Size Limits</item>
        /// <item>Input Sanitization</item>
        /// <item>Anti-Forgery (CSRF)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursSecurity"/> to add
        /// all security middlewares to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursSecurity(
        ///     securityHeaders: opts => opts.EnableHsts = true,
        ///     sizeLimit: opts => opts.DefaultMaxBodySize = 10 * 1024 * 1024,
        ///     sanitization: opts => opts.EnableXssSanitization = true,
        ///     antiForgery: opts => opts.Enabled = true
        /// );
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="securityHeaders">Optional action to configure security headers options.</param>
        /// <param name="sizeLimit">Optional action to configure size limit options.</param>
        /// <param name="sanitization">Optional action to configure sanitization options.</param>
        /// <param name="antiForgery">Optional action to configure anti-forgery options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursSecurity(
            this IServiceCollection services,
            Action<Configuration.SecurityHeadersOptions>? securityHeaders = null,
            Action<Configuration.RequestSizeLimitOptions>? sizeLimit = null,
            Action<Configuration.InputSanitizationOptions>? sanitization = null,
            Action<Configuration.AntiForgeryOptions>? antiForgery = null)
        {
            services.AddMvp24HoursSecurityHeaders(securityHeaders);
            services.AddMvp24HoursRequestSizeLimit(sizeLimit);
            services.AddMvp24HoursInputSanitization(sanitization);
            services.AddMvp24HoursAntiForgery(antiForgery);

            return services;
        }

        #endregion

        #region Rate Limiting

        /// <summary>
        /// Adds rate limiting middleware services using .NET 7+ built-in RateLimiter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures rate limiting with support for:
        /// <list type="bullet">
        /// <item><strong>Fixed Window</strong> - Counts requests in fixed time windows</item>
        /// <item><strong>Sliding Window</strong> - Smooths fixed window boundaries</item>
        /// <item><strong>Token Bucket</strong> - Allows controlled bursts with smooth refill</item>
        /// <item><strong>Concurrency</strong> - Limits concurrent requests</item>
        /// </list>
        /// </para>
        /// <para>
        /// Rate limiting can be applied by:
        /// <list type="bullet">
        /// <item>Client IP address</item>
        /// <item>Authenticated User ID</item>
        /// <item>API Key</item>
        /// <item>Tenant ID</item>
        /// <item>Custom header value</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursRateLimiting"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage with default policy
        /// services.AddMvp24HoursRateLimiting(options =>
        /// {
        ///     options.AddDefaultPolicy(permitLimit: 100, window: TimeSpan.FromMinutes(1));
        /// });
        /// 
        /// // With multiple policies
        /// services.AddMvp24HoursRateLimiting(options =>
        /// {
        ///     options.AddSlidingWindowPolicy("standard", 100, TimeSpan.FromMinutes(1));
        ///     options.AddTokenBucketPolicy("premium", 1000, TimeSpan.FromSeconds(10), 100);
        ///     options.MapEndpointToPolicy("/api/public/*", "standard");
        ///     options.MapEndpointToPolicy("/api/premium/*", "premium");
        /// });
        /// 
        /// // Rate limit by user and IP
        /// services.AddMvp24HoursRateLimiting(options =>
        /// {
        ///     var policy = options.AddSlidingWindowPolicy("api", 100, TimeSpan.FromMinutes(1));
        ///     policy.KeySource = RateLimitKeySource.ClientIp | RateLimitKeySource.UserId;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure rate limiting options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRateLimiting(
            this IServiceCollection services,
            Action<Configuration.RateLimitingOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.RateLimitingOptions>(_ => { });
            }

            // Register the key generator
            services.TryAddSingleton<RateLimiting.IRateLimitKeyGenerator, RateLimiting.DefaultRateLimitKeyGenerator>();

            // Register the partition resolver
            services.TryAddSingleton<RateLimiting.RateLimitPartitionResolver>();

            return services;
        }

        /// <summary>
        /// Adds rate limiting with a simple fixed window policy.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method for simple rate limiting scenarios.
        /// For more complex configurations, use <see cref="AddMvp24HoursRateLimiting(IServiceCollection, Action{RateLimitingOptions}?)"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // 100 requests per minute per IP
        /// services.AddMvp24HoursRateLimiting(100, TimeSpan.FromMinutes(1));
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="permitLimit">Maximum number of requests allowed in the window.</param>
        /// <param name="window">The time window for the rate limit.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRateLimiting(
            this IServiceCollection services,
            int permitLimit,
            TimeSpan window)
        {
            return services.AddMvp24HoursRateLimiting(options =>
            {
                options.AddDefaultPolicy(permitLimit, window);
            });
        }

        /// <summary>
        /// Adds distributed rate limiting with Redis support.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures distributed rate limiting using Redis for:
        /// <list type="bullet">
        /// <item>Consistent rate limiting across multiple application instances</item>
        /// <item>Shared rate limit state in load-balanced environments</item>
        /// <item>Persistence of rate limit counters</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Fallback Behavior:</strong>
        /// If Redis is unavailable, the middleware can fall back to in-memory rate limiting
        /// based on the <c>FallbackToInMemory</c> option.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursRateLimiting(options =>
        /// {
        ///     options.AddDefaultPolicy(100, TimeSpan.FromMinutes(1));
        /// });
        /// 
        /// services.AddMvp24HoursDistributedRateLimiting(options =>
        /// {
        ///     options.Enabled = true;
        ///     options.ConnectionString = "localhost:6379";
        ///     options.InstanceName = "myapp:ratelimit:";
        ///     options.FallbackToInMemory = true;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure distributed rate limiting options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursDistributedRateLimiting(
            this IServiceCollection services,
            Action<Configuration.DistributedRateLimitingOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.DistributedRateLimitingOptions>(_ => { });
            }

            // Register in-memory fallback
            services.TryAddSingleton<RateLimiting.InMemoryRateLimiter>();

            // Register distributed rate limiter
            services.TryAddSingleton<RateLimiting.IDistributedRateLimiter, RateLimiting.RedisDistributedRateLimiter>();

            return services;
        }

        /// <summary>
        /// Adds distributed rate limiting with Redis connection string.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method for quickly enabling Redis-based distributed rate limiting.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursDistributedRateLimiting("localhost:6379");
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="redisConnectionString">The Redis connection string.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursDistributedRateLimiting(
            this IServiceCollection services,
            string redisConnectionString)
        {
            return services.AddMvp24HoursDistributedRateLimiting(options =>
            {
                options.Enabled = true;
                options.ConnectionString = redisConnectionString;
            });
        }

        /// <summary>
        /// Adds a custom rate limit key generator.
        /// </summary>
        /// <typeparam name="TGenerator">The type of the custom key generator.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRateLimitKeyGenerator<TGenerator>(this IServiceCollection services)
            where TGenerator : class, RateLimiting.IRateLimitKeyGenerator
        {
            services.AddSingleton<RateLimiting.IRateLimitKeyGenerator, TGenerator>();
            return services;
        }

        #endregion

        #region Performance and Caching

        /// <summary>
        /// Adds enhanced request/response compression services with Gzip and Brotli support.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures response compression with support for:
        /// <list type="bullet">
        /// <item>Gzip compression</item>
        /// <item>Brotli compression (preferred when available)</item>
        /// <item>Configurable MIME types</item>
        /// <item>Minimum compression size threshold</item>
        /// <item>HTTPS support (optional)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <c>app.UseResponseCompression()</c> to add the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursCompression(options =>
        /// {
        ///     options.Enabled = true;
        ///     options.UseBrotli = true;
        ///     options.UseGzip = true;
        ///     options.EnableForHttps = false;
        ///     options.MinimumCompressionSize = 1024;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure compression options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursCompression(
            this IServiceCollection services,
            Action<CompressionOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<CompressionOptions>(_ => { });
            }

            services.AddResponseCompression(options =>
            {
                var compressionOptions = new CompressionOptions();
                configureOptions?.Invoke(compressionOptions);

                options.EnableForHttps = compressionOptions.EnableForHttps;
                options.MimeTypes = compressionOptions.MimeTypes.ToList();

                if (compressionOptions.UseBrotli)
                {
                    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
                }

                if (compressionOptions.UseGzip)
                {
                    options.Providers.Add<GzipCompressionProvider>();
                }
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                var compressionOptions = new CompressionOptions();
                configureOptions?.Invoke(compressionOptions);
                options.Level = (CompressionLevel)Math.Clamp(compressionOptions.CompressionLevel, 0, 9);
            });

            if (configureOptions != null)
            {
                var compressionOptions = new CompressionOptions();
                configureOptions(compressionOptions);
                if (compressionOptions.UseBrotli)
                {
                    services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
                    {
                        options.Level = (CompressionLevel)Math.Clamp(compressionOptions.CompressionLevel, 0, 11);
                    });
                }
            }

            return services;
        }

        /// <summary>
        /// Adds request decompression middleware services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures automatic decompression of compressed request bodies.
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursRequestDecompression"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure decompression options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRequestDecompression(
            this IServiceCollection services,
            Action<RequestDecompressionOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<RequestDecompressionOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds response caching services with configurable cache profiles.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures response caching with support for cache profiles.
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursResponseCaching"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure caching options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursResponseCaching(
            this IServiceCollection services,
            Action<MvpResponseCachingOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<MvpResponseCachingOptions>(_ => { });
            }

            services.AddResponseCaching(options =>
            {
                var cachingOptions = new MvpResponseCachingOptions();
                configureOptions?.Invoke(cachingOptions);
                options.MaximumBodySize = (int)cachingOptions.MaximumBodySize;
                options.SizeLimit = (int)cachingOptions.SizeLimit;
                options.UseCaseSensitivePaths = false;
            });

            return services;
        }

        /// <summary>
        /// Adds output caching services (.NET 7+).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>DEPRECATED:</strong> Use <see cref="OutputCachingExtensions.AddMvp24HoursOutputCache"/> instead.
        /// This method only configures options without actually enabling output caching.
        /// </para>
        /// <para>
        /// For full output caching support including policies and Redis backend, use:
        /// <code>
        /// services.AddMvp24HoursOutputCache(options =>
        /// {
        ///     options.AddStandardPolicies();
        /// });
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure output caching options.</param>
        /// <returns>The service collection for chaining.</returns>
        [Obsolete("Use AddMvp24HoursOutputCache from OutputCachingExtensions instead. This method will be removed in a future version.")]
        public static IServiceCollection AddMvp24HoursOutputCaching(
            this IServiceCollection services,
            Action<OutputCachingOptions>? configureOptions = null)
        {
            // Delegate to the new implementation
            return services.AddMvp24HoursOutputCache(configureOptions);
        }

        /// <summary>
        /// Adds ETag and conditional request support services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures ETag generation and conditional request handling.
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursETag"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure ETag options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursETag(
            this IServiceCollection services,
            Action<ETagOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<ETagOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds request timeout services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures request timeout enforcement.
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursRequestTimeout"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure timeout options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursRequestTimeout(
            this IServiceCollection services,
            Action<RequestTimeoutOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<RequestTimeoutOptions>(_ => { });
            }

            return services;
        }

        /// <summary>
        /// Adds Cache-Control header middleware services.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures Cache-Control header policies.
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursCacheControl"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure cache control options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursCacheControl(
            this IServiceCollection services,
            Action<CacheControlOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<CacheControlOptions>(_ => { });
            }

            return services;
        }

        #endregion

        #region Idempotency

        /// <summary>
        /// Adds idempotency middleware services for POST, PUT, and PATCH requests.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures HTTP-level idempotency with support for:
        /// <list type="bullet">
        /// <item><strong>Header-based keys</strong> - Idempotency-Key header</item>
        /// <item><strong>Generated keys</strong> - Hash of request method, path, and body</item>
        /// <item><strong>CQRS Integration</strong> - IIdempotentCommand support</item>
        /// <item><strong>Configurable storage</strong> - Memory or distributed cache</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Idempotency Flow:</strong>
        /// <list type="number">
        /// <item>Extract or generate idempotency key from request</item>
        /// <item>Check if key exists in store</item>
        /// <item>If completed response exists, return cached response with Idempotency-Replayed header</item>
        /// <item>If request is in-flight, return 409 Conflict with Retry-After header</item>
        /// <item>If new request, execute and cache response</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursIdempotency"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage
        /// services.AddMvp24HoursIdempotency();
        /// 
        /// // With custom options
        /// services.AddMvp24HoursIdempotency(options =>
        /// {
        ///     options.HeaderName = "Idempotency-Key";
        ///     options.CacheDuration = TimeSpan.FromHours(24);
        ///     options.StorageType = IdempotencyStorageType.DistributedCache;
        ///     options.RequireIdempotencyKey = false;
        ///     options.IntegrateWithCqrs = true;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure idempotency options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursIdempotency(
            this IServiceCollection services,
            Action<Configuration.IdempotencyOptions>? configureOptions = null)
        {
            var options = new Configuration.IdempotencyOptions();
            configureOptions?.Invoke(options);

            services.Configure<Configuration.IdempotencyOptions>(opt =>
            {
                opt.Enabled = options.Enabled;
                opt.KeySource = options.KeySource;
                opt.HeaderName = options.HeaderName;
                opt.StorageType = options.StorageType;
                opt.CacheDuration = options.CacheDuration;
                opt.MinimumRetryInterval = options.MinimumRetryInterval;
                opt.RequireIdempotencyKey = options.RequireIdempotencyKey;
                opt.CacheResponseHeaders = options.CacheResponseHeaders;
                opt.IncludeKeyInResponse = options.IncludeKeyInResponse;
                opt.RetryAfterHeaderName = options.RetryAfterHeaderName;
                opt.ReplayedHeaderName = options.ReplayedHeaderName;
                opt.InFlightStatusCode = options.InFlightStatusCode;
                opt.InFlightRetryAfterSeconds = options.InFlightRetryAfterSeconds;
                opt.UseProblemDetails = options.UseProblemDetails;
                opt.IdempotentMethods = options.IdempotentMethods;
                opt.ExcludedPaths = options.ExcludedPaths;
                opt.RequiredPaths = options.RequiredPaths;
                opt.MaxRequestBodySizeForHashing = options.MaxRequestBodySizeForHashing;
                opt.CacheKeyPrefix = options.CacheKeyPrefix;
                opt.NonCacheableStatusCodes = options.NonCacheableStatusCodes;
                opt.IntegrateWithCqrs = options.IntegrateWithCqrs;
                opt.EnableLogging = options.EnableLogging;
                opt.InFlightMessage = options.InFlightMessage;
                opt.MissingKeyMessage = options.MissingKeyMessage;
            });

            // Register storage based on options
            switch (options.StorageType)
            {
                case Configuration.IdempotencyStorageType.InMemory:
                    services.TryAddSingleton<Idempotency.IIdempotencyStore, Idempotency.InMemoryIdempotencyStore>();
                    break;

                case Configuration.IdempotencyStorageType.DistributedCache:
                    services.TryAddSingleton<Idempotency.IIdempotencyStore, Idempotency.DistributedCacheIdempotencyStore>();
                    break;

                case Configuration.IdempotencyStorageType.Custom:
                    // Custom storage - application must register IIdempotencyStore
                    break;
            }

            // Register key generator based on CQRS integration option
            if (options.IntegrateWithCqrs)
            {
                services.TryAddSingleton<Idempotency.IIdempotencyKeyGenerator, Idempotency.CqrsIdempotencyKeyGenerator>();
            }
            else
            {
                services.TryAddSingleton<Idempotency.IIdempotencyKeyGenerator, Idempotency.DefaultIdempotencyKeyGenerator>();
            }

            return services;
        }

        /// <summary>
        /// Adds idempotency with in-memory storage.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Warning:</strong> In-memory storage is suitable for development and
        /// single-instance deployments only. For distributed environments, use
        /// <see cref="AddMvp24HoursIdempotency(IServiceCollection, Action{IdempotencyOptions}?)"/>
        /// with <c>StorageType = DistributedCache</c>.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="cacheDuration">Duration to cache idempotency results. Default: 24 hours.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursIdempotencyInMemory(
            this IServiceCollection services,
            TimeSpan? cacheDuration = null)
        {
            return services.AddMvp24HoursIdempotency(options =>
            {
                options.StorageType = Configuration.IdempotencyStorageType.InMemory;
                if (cacheDuration.HasValue)
                {
                    options.CacheDuration = cacheDuration.Value;
                }
            });
        }

        /// <summary>
        /// Adds idempotency with distributed cache storage (Redis, SQL, etc.).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures idempotency with distributed cache storage,
        /// suitable for production environments with multiple application instances.
        /// </para>
        /// <para>
        /// <strong>Prerequisites:</strong>
        /// Register an <c>IDistributedCache</c> implementation before calling this method:
        /// <code>
        /// // For Redis
        /// services.AddStackExchangeRedisCache(options =>
        /// {
        ///     options.Configuration = "localhost:6379";
        /// });
        /// 
        /// // Or for SQL Server
        /// services.AddDistributedSqlServerCache(options =>
        /// {
        ///     options.ConnectionString = connectionString;
        ///     options.SchemaName = "dbo";
        ///     options.TableName = "IdempotencyCache";
        /// });
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="cacheDuration">Duration to cache idempotency results. Default: 24 hours.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursIdempotencyDistributed(
            this IServiceCollection services,
            TimeSpan? cacheDuration = null)
        {
            return services.AddMvp24HoursIdempotency(options =>
            {
                options.StorageType = Configuration.IdempotencyStorageType.DistributedCache;
                if (cacheDuration.HasValue)
                {
                    options.CacheDuration = cacheDuration.Value;
                }
            });
        }

        /// <summary>
        /// Adds a custom idempotency store.
        /// </summary>
        /// <typeparam name="TStore">The type of the custom store.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure idempotency options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursIdempotencyStore<TStore>(
            this IServiceCollection services,
            Action<Configuration.IdempotencyOptions>? configureOptions = null)
            where TStore : class, Idempotency.IIdempotencyStore
        {
            // Configure options
            services.AddMvp24HoursIdempotency(options =>
            {
                options.StorageType = Configuration.IdempotencyStorageType.Custom;
                configureOptions?.Invoke(options);
            });

            // Register custom store
            services.AddSingleton<Idempotency.IIdempotencyStore, TStore>();

            return services;
        }

        /// <summary>
        /// Adds a custom idempotency key generator.
        /// </summary>
        /// <typeparam name="TGenerator">The type of the custom generator.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursIdempotencyKeyGenerator<TGenerator>(
            this IServiceCollection services)
            where TGenerator : class, Idempotency.IIdempotencyKeyGenerator
        {
            services.AddSingleton<Idempotency.IIdempotencyKeyGenerator, TGenerator>();
            return services;
        }

        #endregion

        #region Health Checks

        /// <summary>
        /// Adds health check services with configuration for Mvp24Hours WebAPI.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures health checks with support for:
        /// <list type="bullet">
        /// <item>Database health checks (SQL Server, PostgreSQL, MySQL, MongoDB)</item>
        /// <item>Cache health checks (Redis, Memory)</item>
        /// <item>Messaging health checks (RabbitMQ)</item>
        /// <item>Pipeline health checks</item>
        /// </list>
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursHealthChecks"/> to map
        /// the health check endpoints.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage
        /// services.AddMvp24HoursHealthChecks();
        /// 
        /// // With custom configuration
        /// services.AddMvp24HoursHealthChecks(options =>
        /// {
        ///     options.HealthPath = "/health";
        ///     options.ReadinessPath = "/health/ready";
        ///     options.LivenessPath = "/health/live";
        ///     options.EnableDetailedResponses = true;
        ///     options.Timeout = TimeSpan.FromSeconds(30);
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure health check options.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddMvp24HoursHealthChecks(
            this IServiceCollection services,
            Action<Configuration.HealthCheckOptions>? configureOptions = null)
        {
            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<Configuration.HealthCheckOptions>(_ => { });
            }

            // Add health checks
            return services.AddHealthChecks();
        }

        /// <summary>
        /// Adds cache health check (Redis and Memory cache).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This health check verifies:
        /// <list type="bullet">
        /// <item>Distributed cache connectivity (Redis)</item>
        /// <item>Memory cache availability</item>
        /// <item>Cache read/write operations</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMvp24HoursCacheCheck("cache", options =>
        ///     {
        ///         options.CheckDistributedCache = true;
        ///         options.CheckMemoryCache = true;
        ///     });
        /// </code>
        /// </example>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">The health check name. Default is "cache".</param>
        /// <param name="configureOptions">Optional action to configure cache health check options.</param>
        /// <param name="failureStatus">The failure status. Default is Unhealthy.</param>
        /// <param name="tags">Tags for filtering health checks.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddMvp24HoursCacheCheck(
            this IHealthChecksBuilder builder,
            string name = "cache",
            Action<HealthChecks.CacheHealthCheckOptions>? configureOptions = null,
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            var options = new HealthChecks.CacheHealthCheckOptions();
            configureOptions?.Invoke(options);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new HealthChecks.CacheHealthCheck(
                    sp.GetService<IDistributedCache>(),
                    sp.GetService<IMemoryCache>(),
                    sp.GetRequiredService<ILogger<HealthChecks.CacheHealthCheck>>(),
                    options),
                failureStatus,
                tags ?? new[] { "cache", "ready" },
                timeout));
        }

        /// <summary>
        /// Adds RabbitMQ health check.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This health check verifies RabbitMQ connectivity and channel creation.
        /// Requires <c>IMvpRabbitMQConnection</c> to be registered in DI.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddHealthChecks()
        ///     .AddMvp24HoursRabbitMQCheck("rabbitmq", tags: new[] { "messaging", "ready" });
        /// </code>
        /// </example>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">The health check name. Default is "rabbitmq".</param>
        /// <param name="failureStatus">The failure status. Default is Unhealthy.</param>
        /// <param name="tags">Tags for filtering health checks.</param>
        /// <param name="timeout">Optional timeout for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        /// <remarks>
        /// This method requires a reference to Mvp24Hours.Infrastructure.RabbitMQ project.
        /// If the RabbitMQ project is not referenced, this method will not be available.
        /// </remarks>
        /*
        public static IHealthChecksBuilder AddMvp24HoursRabbitMQCheck(
            this IHealthChecksBuilder builder,
            string name = "rabbitmq",
            HealthStatus failureStatus = HealthStatus.Unhealthy,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            // This method requires Mvp24Hours.Infrastructure.RabbitMQ project reference
            // Uncomment and add the project reference if RabbitMQ support is needed
            throw new NotImplementedException("RabbitMQ health check requires Mvp24Hours.Infrastructure.RabbitMQ project reference.");
        }
        */

        #endregion

        #region Content Negotiation

        /// <summary>
        /// Adds content negotiation services for supporting multiple response formats (JSON, XML).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method configures content negotiation with support for:
        /// <list type="bullet">
        /// <item><strong>JSON</strong> - application/json, text/json</item>
        /// <item><strong>XML</strong> - application/xml, text/xml</item>
        /// <item><strong>ProblemDetails</strong> - application/problem+json, application/problem+xml</item>
        /// <item><strong>Custom Formatters</strong> - Register additional formats</item>
        /// </list>
        /// </para>
        /// <para>
        /// Content negotiation respects the Accept header with quality values (q=0.9),
        /// format query parameter (?format=json), and URL suffix (.json, .xml).
        /// </para>
        /// <para>
        /// Use <see cref="ApplicationBuilderExtensions.UseMvp24HoursContentNegotiation"/> to add
        /// the middleware to the pipeline.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic usage with default options
        /// services.AddMvp24HoursContentNegotiation();
        /// 
        /// // With custom configuration
        /// services.AddMvp24HoursContentNegotiation(options =>
        /// {
        ///     options.DefaultMediaType = "application/json";
        ///     options.RespectQualityValues = true;
        ///     options.Return406WhenNoMatch = false;
        ///     options.EnableFormatParameter = true;
        ///     options.EnableFormatSuffix = false;
        ///     options.AddVaryHeader = true;
        ///     options.UseRfc7807ContentTypeForProblemDetails = true;
        /// });
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure content negotiation options.</param>
        /// <param name="configureBuilder">Optional action to configure custom formatters.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursContentNegotiation(
            this IServiceCollection services,
            Action<ContentNegotiationOptions>? configureOptions = null,
            Action<ContentNegotiation.ContentNegotiationBuilder>? configureBuilder = null)
        {
            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<ContentNegotiationOptions>(_ => { });
            }

            // Create builder for custom formatters
            var builder = new ContentNegotiation.ContentNegotiationBuilder(services);
            configureBuilder?.Invoke(builder);

            // Register formatter registry
            services.TryAddSingleton<ContentNegotiation.IContentFormatterRegistry>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ContentNegotiationOptions>>().Value;
                
                // Collect custom formatter instances
                var customFormatters = new System.Collections.Generic.List<ContentNegotiation.IContentFormatter>();
                
                // Add formatter instances registered via builder
                customFormatters.AddRange(builder.CustomFormatters);
                
                // Resolve formatter types registered via builder
                foreach (var formatterType in builder.CustomFormatterTypes)
                {
                    try
                    {
                        var formatter = sp.GetService(formatterType) as ContentNegotiation.IContentFormatter;
                        if (formatter != null)
                        {
                            customFormatters.Add(formatter);
                        }
                    }
                    catch
                    {
                        // Ignore if formatter cannot be resolved (will be registered later)
                    }
                }
                
                var registry = new ContentNegotiation.ContentFormatterRegistry(options, customFormatters);
                
                // Register any formatter types that weren't resolved yet
                foreach (var formatterType in builder.CustomFormatterTypes)
                {
                    try
                    {
                        var formatter = sp.GetService(formatterType) as ContentNegotiation.IContentFormatter;
                        if (formatter != null)
                        {
                            registry.RegisterFormatter(formatter);
                        }
                    }
                    catch
                    {
                        // Ignore if formatter cannot be resolved
                    }
                }
                
                return registry;
            });

            // Register content negotiator
            services.TryAddSingleton<ContentNegotiation.AcceptHeaderNegotiator>();

            // Register default formatters
            services.TryAddSingleton<ContentNegotiation.JsonContentFormatter>();
            services.TryAddSingleton<ContentNegotiation.XmlContentFormatter>();
            services.TryAddSingleton<ContentNegotiation.ProblemDetailsJsonFormatter>();
            services.TryAddSingleton<ContentNegotiation.ProblemDetailsXmlFormatter>();

            // Register result filter
            services.TryAddScoped<Filters.ContentNegotiationResultFilter>();

            return services;
        }

        /// <summary>
        /// Adds content negotiation with JSON as the default format.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that configures content negotiation with JSON
        /// as the default format when no Accept header is provided.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="enableXml">Whether to enable XML support. Default: true.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursContentNegotiationJson(
            this IServiceCollection services,
            bool enableXml = true)
        {
            return services.AddMvp24HoursContentNegotiation(options =>
            {
                options.DefaultMediaType = "application/json";

                if (!enableXml)
                {
                    options.SupportedMediaTypes.RemoveAll(m =>
                        m.MediaType.Contains("xml", StringComparison.OrdinalIgnoreCase));
                    options.FormatMappings.Remove("xml");
                }
            });
        }

        /// <summary>
        /// Adds content negotiation with XML as the default format.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a convenience method that configures content negotiation with XML
        /// as the default format when no Accept header is provided.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="enableJson">Whether to enable JSON support. Default: true.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursContentNegotiationXml(
            this IServiceCollection services,
            bool enableJson = true)
        {
            return services.AddMvp24HoursContentNegotiation(options =>
            {
                options.DefaultMediaType = "application/xml";

                if (!enableJson)
                {
                    options.SupportedMediaTypes.RemoveAll(m =>
                        m.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase));
                    options.FormatMappings.Remove("json");
                }
            });
        }

        /// <summary>
        /// Adds a custom content formatter to the content negotiation system.
        /// </summary>
        /// <typeparam name="TFormatter">The type of the custom formatter.</typeparam>
        /// <remarks>
        /// <para>
        /// Use this method to register custom formatters for specific media types.
        /// The formatter must implement <see cref="ContentNegotiation.IContentFormatter"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddMvp24HoursContentNegotiation()
        ///     .AddContentFormatter&lt;CsvContentFormatter&gt;();
        /// </code>
        /// </example>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddContentFormatter<TFormatter>(this IServiceCollection services)
            where TFormatter : class, ContentNegotiation.IContentFormatter
        {
            services.AddSingleton<ContentNegotiation.IContentFormatter, TFormatter>();

            // Register with the registry if already created
            services.AddSingleton<TFormatter>();

            return services;
        }

        /// <summary>
        /// Configures MVC to use content negotiation result filter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method adds the content negotiation result filter to MVC controllers.
        /// Call this after <see cref="AddMvp24HoursContentNegotiation"/> to enable
        /// automatic content format transformation for MVC actions.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursContentNegotiationMvc(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add<Filters.ContentNegotiationResultFilter>();
            });

            return services;
        }

        /// <summary>
        /// Adds content negotiation services configured for strict RFC 7231 compliance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This configuration enables strict content negotiation:
        /// <list type="bullet">
        /// <item>Returns 406 Not Acceptable when no matching format is found</item>
        /// <item>Respects quality values in Accept header</item>
        /// <item>Uses RFC 7807 content types for ProblemDetails</item>
        /// <item>Disables format parameter and URL suffix</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvp24HoursStrictContentNegotiation(this IServiceCollection services)
        {
            return services.AddMvp24HoursContentNegotiation(options =>
            {
                options.Return406WhenNoMatch = true;
                options.RespectQualityValues = true;
                options.UseRfc7807ContentTypeForProblemDetails = true;
                options.EnableFormatParameter = false;
                options.EnableFormatSuffix = false;
                options.AddVaryHeader = true;
            });
        }

        #endregion
    }
}
