//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Helpers;
using System;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Mvp24Hours.Patterns.Test.Setup
{
    public static class Startup
    {
        private static WireMockServer _server;
        private static readonly object _lock = new object();

        public static WireMockServer GetMockServer()
        {
            lock (_lock)
            {
                if (_server == null || !_server.IsStarted)
                {
                    _server = WireMockServer.Start();
                    ConfigureMockEndpoints(_server);
                }
                return _server;
            }
        }

        private static void ConfigureMockEndpoints(WireMockServer server)
        {
            // GET /users
            server.Given(Request.Create().WithPath("/users").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(@"[
                        {""id"": 1, ""name"": ""Leanne Graham"", ""username"": ""Bret"", ""email"": ""Sincere@april.biz""},
                        {""id"": 2, ""name"": ""Ervin Howell"", ""username"": ""Antonette"", ""email"": ""Shanna@melissa.tv""}
                    ]"));

            // GET /posts/1
            server.Given(Request.Create().WithPath("/posts/1").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(@"{""id"": 1, ""title"": ""test title"", ""body"": ""test body"", ""userId"": 1}"));

            // POST /posts
            server.Given(Request.Create().WithPath("/posts").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(201)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(@"{""id"": 101, ""title"": ""foo"", ""body"": ""bar"", ""userId"": 1}"));

            // PUT /posts/1
            server.Given(Request.Create().WithPath("/posts/1").UsingPut())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(@"{""id"": 1, ""title"": ""foo1"", ""body"": ""bar1"", ""userId"": 1}"));

            // DELETE /posts/1
            server.Given(Request.Create().WithPath("/posts/1").UsingDelete())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{}"));

            // PATCH /posts/1
            server.Given(Request.Create().WithPath("/posts/1").UsingPatch())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(@"{""id"": 1, ""title"": ""foo1"", ""body"": ""test body"", ""userId"": 1}"));

            // GET /notFound - returns 404
            server.Given(Request.Create().WithPath("/notFound").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(404)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(@"{""error"": ""Not Found""}"));
        }

        public static IServiceProvider InitializeHttp()
        {
            var server = GetMockServer();
            var baseUrl = server.Url;

            var services = new ServiceCollection()
                            .AddSingleton(ConfigurationHelper.AppSettings);

            services.AddHttpClient("jsonUrl", client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            });

            services.AddHttpClient("HttpClientTest", client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            });

            return services.BuildServiceProvider();
        }

        public static void StopMockServer()
        {
            lock (_lock)
            {
                if (_server != null && _server.IsStarted)
                {
                    _server.Stop();
                    _server.Dispose();
                    _server = null;
                }
            }
        }
    }
}
