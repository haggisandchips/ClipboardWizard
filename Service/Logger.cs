using System;
using System.IO;

namespace ClipboardWizard.Service
{
    /// <summary>
    /// Minimal append-only file logger for failures the user doesn't need to see in the
    /// moment but that should be diagnosable after the fact.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new();

        public static string LogPath { get; set; }

        public static void LogError(string context, Exception exception)
        {
            if (string.IsNullOrEmpty(LogPath))
            {
                return;
            }

            string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{context}] {exception}{Environment.NewLine}";

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogPath, entry);
                }
            }
            catch (IOException)
            {
                // Logging is best-effort; a failure here must never crash the app.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
