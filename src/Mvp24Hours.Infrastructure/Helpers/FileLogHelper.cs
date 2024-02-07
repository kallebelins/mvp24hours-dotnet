//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Newtonsoft.Json;
using System;
using System.IO;

namespace Mvp24Hours.Helpers
{
    /// <summary>
    /// Contains functions to handle log files
    /// </summary>
    public static class FileLogHelper
    {
        /// <summary>
        /// Writes log with model characteristics in the parameter
        /// </summary>
        public static void WriteLog<T>(T dto, string logPath, string suffixFilename = null, string header = null)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                return;
            }

            string filename = $"{DateTime.Today:yyyy_MM_dd}_{Guid.NewGuid()}.log";
            if (!string.IsNullOrEmpty(suffixFilename))
            {
                filename = $"{suffixFilename.ToLower()}_{filename}";
            }
            var folder = $"{logPath}/{DateTime.Today:yyyy_MM_dd}/";
            WriteDisk(dto, folder, filename, $"{header} Hora : {DateTime.Now:HH:mm:ss.fff}", true);
        }
        /// <summary>
        /// Writes log with model characteristics in the parameter using token to map location (system folder)
        /// </summary>
        public static void WriteLogToken<T>(string token, string fileName, T obj, string logPath)
        {
            lock (obj)
            {
                if (string.IsNullOrEmpty(logPath))
                {
                    return;
                }

                var folder = $"{logPath}/{token}/";
                Directory.CreateDirectory(folder);

                var fullPath = $"{folder}{fileName}.json";
                File.WriteAllText(fullPath, JsonConvert.SerializeObject(obj));
            }
        }
        /// <summary>
        /// Performs log reading based on token (system folder path)
        /// </summary>
        public static T ReadLogToken<T>(string token, string fileName, string logPath)
        {
            var fullPath = $"{logPath}/{token}/{fileName}.json";
            if (!File.Exists(fullPath))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(File.ReadAllText(fullPath));
        }
        private static void WriteDisk<T>(T obj, string folder, string fileName, string header = null, bool append = false)
        {
            lock (obj)
            {
                Directory.CreateDirectory(folder);
                string fullpath = folder + fileName;
                using var sw = new StreamWriter(fullpath, append);
                if (!string.IsNullOrEmpty(header))
                {
                    sw.Write(header.PadLeft(5, '-').PadRight(5, '-') + "\r\n");
                }

                sw.Write(JsonConvert.SerializeObject(obj) + "\r\n");
            }
        }
    }
}
