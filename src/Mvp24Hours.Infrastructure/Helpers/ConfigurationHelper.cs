//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace Mvp24Hours.Helpers
{
    /// <summary>
    /// Class that helps reading API settings
    /// </summary>
    public static class ConfigurationHelper
    {
        #region [ Envionment ]

        private static IHostEnvironment _environment;

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
        public static IHostEnvironment GetEnvironment()
        {
            return _environment;
        }

        #endregion

        #region [ Settings ]

        private static IConfigurationRoot _appSettings;

        /// <summary>
        /// 
        /// </summary>
        public static IConfigurationRoot AppSettings
        {
            get
            {
                if (_appSettings == null)
                {
                    LoadSettings();
                }
                return _appSettings;
            }
        }

        private static void LoadSettings()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true, reloadOnChange: true);
            var env = GetEnvironment();
            if (env != null)
            {
                builder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
            }
            _appSettings = builder.Build();
        }

        /// <summary>
        /// Get the settings of the application that is running
        /// </summary>
        public static string GetSettings(string key)
        {
            return GetSection(key)?.Value;
        }

        /// <summary>
        /// Get an instance of the settings of the running application
        /// </summary>
        public static T GetSettings<T>(string key)
        {
            return GetSection(key).Get<T>();
        }

        /// <summary>
        /// Get the section of the application that is running
        /// </summary>
        public static IConfigurationSection GetSection(string key)
        {
            if (AppSettings != null)
            {
                return AppSettings?.GetSection(key);
            }
            return default;
        }

        #region [ Configuration Settings ]

        /// <summary>
        /// Records native .NET core configuration
        /// </summary>
        public static void SetConfiguration(IConfiguration configuration)
        {
            _appSettings = (IConfigurationRoot)configuration;
        }

        #endregion

        #endregion
    }
}
