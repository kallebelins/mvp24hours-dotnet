//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace Mvp24Hours.Infrastructure.Helpers
{
    /// <summary>
    /// Contains functions to handle files
    /// </summary>
    public static class DirectoryHelper
    {
        public static string GetExecutingDirectory()
        {
            UriBuilder uri = new(uri: Assembly.GetExecutingAssembly().Location);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Checks if directory exists or creates it if it doesn't exist.
        /// </summary>
        /// <param name="path">The directory path to check or create.</param>
        /// <param name="logger">Optional logger for telemetry. If null, no logging is performed.</param>
        /// <returns>True if directory exists or was created successfully; otherwise, false.</returns>
        public static bool ExistsOrCreate(string path, ILogger? logger = null)
        {
            if (path == null) { return false; }
            try
            {
                if (!Directory.Exists(path))
                {
                    logger?.LogDebug("Creating directory. Path: {DirectoryPath}", path);
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to create directory. Path: {DirectoryPath}", path);
            }
            return false;
        }
    }

    /// <summary>
    /// Injectable service for directory operations with logging support.
    /// </summary>
    public class DirectoryService
    {
        private readonly ILogger<DirectoryService> _logger;

        public DirectoryService(ILogger<DirectoryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks if directory exists or creates it if it doesn't exist.
        /// </summary>
        /// <param name="path">The directory path to check or create.</param>
        /// <returns>True if directory exists or was created successfully; otherwise, false.</returns>
        public bool ExistsOrCreate(string path)
        {
            return DirectoryHelper.ExistsOrCreate(path, _logger);
        }

        /// <summary>
        /// Gets the executing directory.
        /// </summary>
        /// <returns>The directory path where the executing assembly is located.</returns>
        public string GetExecutingDirectory()
        {
            return DirectoryHelper.GetExecutingDirectory();
        }
    }
}
