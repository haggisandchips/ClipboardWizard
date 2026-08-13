using ClipboardWizard.Model;
using System;
using System.IO;
using System.Text.Json;

namespace ClipboardWizard.Service
{
    public class WindowSettingsService : IWindowSettingsService
    {
        private readonly string _filePath;

        public WindowSettingsService(string filePath)
        {
            _filePath = filePath;
        }

        public WindowSettings Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(_filePath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // Best-effort, like the rest of this app's local persistence - a corrupt or
                // unreadable settings file just means the window falls back to its default
                // placement, not a startup failure.
                Logger.LogError(nameof(Load), ex);
                return null;
            }
        }

        public void Save(WindowSettings settings)
        {
            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_filePath, JsonSerializer.Serialize(settings));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Runs on shutdown, unprompted by the user - log only, per this app's
                // convention for background/automatic paths.
                Logger.LogError(nameof(Save), ex);
            }
        }
    }
}
