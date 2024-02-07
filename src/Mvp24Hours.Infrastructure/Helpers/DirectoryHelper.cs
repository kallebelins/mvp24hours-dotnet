//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
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

        public static bool ExistsOrCreate(string path)
        {
            if (path == null) { return false; }
            try
            {
                if (!Directory.Exists(path))
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "directory-helper-existsorcreate-create", $"directory-path:{path}");
                    Directory.CreateDirectory(path);
                }
                return true;
            }
            catch (Exception ex)
            {
                TelemetryHelper.Execute(TelemetryLevels.Error, "directory-helper-existsorcreate-failure", ex);
            }
            return false;
        }
    }
}
