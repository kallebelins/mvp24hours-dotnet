using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Mcp.Configuration;

namespace Mvp24Hours.Mcp;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var options = new McpOptions
        {
            RepoRoot = Environment.GetEnvironmentVariable("MVP24HOURS_REPO_ROOT")
        };

        if (args.Contains("--http", StringComparer.OrdinalIgnoreCase))
        {
            await RunHttpAsync(args, options);
        }
        else
        {
            await RunStdioAsync(options);
        }
    }

    private static async Task RunStdioAsync(McpOptions options)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services.AddMvp24HoursDevKit(options);
        builder.Services
            .AddMcpServer(serverOptions =>
            {
                serverOptions.ServerInfo = new() { Name = "mvp24hours-devkit", Version = "1.0.0" };
            })
            .WithStdioServerTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly()
            .WithPromptsFromAssembly();

        await builder.Build().RunAsync();
    }

    private static async Task RunHttpAsync(string[] args, McpOptions options)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMvp24HoursDevKit(options);
        builder.Services
            .AddMcpServer(serverOptions =>
            {
                serverOptions.ServerInfo = new() { Name = "mvp24hours-devkit", Version = "1.0.0" };
            })
            .WithHttpTransport(transport => transport.Stateless = true)
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly()
            .WithPromptsFromAssembly();

        var app = builder.Build();
        app.MapMcp();

        var port = 5199;
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--port" && int.TryParse(args[i + 1], out var p))
            {
                port = p;
            }
        }

        var urlsArg = args.FirstOrDefault(a => a.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase));
        var url = urlsArg?["--urls=".Length..] ?? $"http://localhost:{port}/mcp";
        await app.RunAsync(url);
    }
}
