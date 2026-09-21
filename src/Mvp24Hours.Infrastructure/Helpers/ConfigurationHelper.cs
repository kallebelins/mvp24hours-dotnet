//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Mvp24Hours.Helpers;

/// <summary>
/// Class that helps reading API settings
/// </summary>
/// <remarks>
/// <para>
/// <b>Deprecated.</b> This helper is a process-wide service locator for configuration. It keeps two
/// pieces of static mutable state (<c>_environment</c> and <c>AppSettings</c>), and when nothing has
/// been set it builds its own configuration by reading <c>appsettings.json</c> from
/// <see cref="Directory.GetCurrentDirectory()"/> — which is the process working directory, not
/// necessarily the content root. That makes the resolved values depend on how the process was
/// started, bypasses every source the host already composed (environment variables, user secrets,
/// command line, key vaults), and cannot be isolated per test.
/// </para>
/// <para>
/// Bind options at the host instead, using <c>IConfiguration</c> and <c>IOptions&lt;T&gt;</c>:
/// </para>
/// <code>
/// // Before:
/// string? cs = ConfigurationHelper.AppSettings.GetConnectionString("DataContext");
/// MySettings? s = ConfigurationHelper.GetSettings&lt;MySettings&gt;("MySection");
///
/// // After (Program.cs):
/// builder.Services.Configure&lt;MySettings&gt;(builder.Configuration.GetSection("MySection"));
/// builder.Services.AddDbContext&lt;DataContext&gt;(o =&gt;
///     o.UseSqlServer(builder.Configuration.GetConnectionString("DataContext")));
///
/// // After (consumer): inject IConfiguration or IOptions&lt;MySettings&gt;
/// public sealed class MyService(IOptions&lt;MySettings&gt; options) { }
/// </code>
/// <para>
/// See <c>docs/en-us/configuration-reference.md</c> for the full guidance.
/// </para>
/// </remarks>
[Obsolete("Bind options via IConfiguration/IOptions<T> at the host. Will be removed in v12.")]
public static class ConfigurationHelper
{
    #region [ Envionment ]

    private static IHostEnvironment? _environment;

    /// <summary>
    /// Defines the host environment of the application that is running
    /// </summary>
    public static void SetEnvironment(IHostEnvironment environment)
    {
        _environment = environment;
        if (environment != null)
        {
            LoadSettings();
        }
    }

    /// <summary>
    /// Gets the host environment of the application that is running
    /// </summary>
    public static IHostEnvironment? GetEnvironment()
    {
        return _environment;
    }

    #endregion

    #region [ Settings ]


    /// <summary>
    /// 
    /// </summary>
    public static IConfigurationRoot AppSettings
    {
        get
        {
            if (field == null)
            {
                LoadSettings();
            }
            return field!;
        }

        private set;
    }

    private static void LoadSettings()
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true, reloadOnChange: true);
        IHostEnvironment? env = GetEnvironment();
        if (env != null)
        {
            builder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
        }
        AppSettings = builder.Build();
    }

    /// <summary>
    /// Get the settings of the application that is running
    /// </summary>
    public static string? GetSettings(string key)
    {
        return GetSection(key)?.Value;
    }

    /// <summary>
    /// Get an instance of the settings of the running application
    /// </summary>
    public static T? GetSettings<T>(string key) where T : class
    {
        return GetSection(key)?.Get<T>();
    }

    /// <summary>
    /// Get the section of the application that is running
    /// </summary>
    public static IConfigurationSection? GetSection(string key)
    {
        if (AppSettings != null)
        {
            return AppSettings.GetSection(key);
        }
        return default;
    }

    #region [ Configuration Settings ]

    /// <summary>
    /// Records native .NET core configuration
    /// </summary>
    public static void SetConfiguration(IConfiguration configuration)
    {
        AppSettings = (IConfigurationRoot)configuration;
    }

    #endregion

    #endregion
}
