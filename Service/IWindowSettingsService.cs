using ClipboardWizard.Model;

namespace ClipboardWizard.Service
{
    public interface IWindowSettingsService
    {
        /// <summary>Returns the last-saved window settings, or null if none exist yet.</summary>
        WindowSettings Load();

        void Save(WindowSettings settings);
    }
}
