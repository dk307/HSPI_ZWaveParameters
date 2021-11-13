using HomeSeer.Jui.Views;

namespace Hspi
{
    internal class SettingsPages
    {
        public SettingsPages(SettingsCollection collection)
        {
            DebugLoggingEnabled = collection[LoggingSettingPageId].GetViewById<ToggleView>(LoggingDebugId).IsEnabled;
            LogtoFileEnabled = collection[LoggingSettingPageId].GetViewById<ToggleView>(LogToFileId).IsEnabled;
        }

        public bool DebugLoggingEnabled { get; private set; }

        public bool LogtoFileEnabled { get; private set; }

        public static Page CreateDefault(bool enableDefaultLogging = false, bool logToFileEnable = false)
        {
            var loggingSettings = PageFactory.CreateSettingsPage(LoggingSettingPageId, "Logging");
            loggingSettings = loggingSettings.WithToggle(LoggingDebugId, "Enable Debug Logging", enableDefaultLogging);
            loggingSettings = loggingSettings.WithToggle(LogToFileId, "Log to file", logToFileEnable);

            return loggingSettings.Page;
        }

        public bool OnSettingChange(AbstractView changedView)
        {
            if (changedView.Id == LoggingDebugId)
            {
                DebugLoggingEnabled = ((ToggleView)changedView).IsEnabled;
                return true;
            }

            if (changedView.Id == LogToFileId)
            {
                LogtoFileEnabled = ((ToggleView)changedView).IsEnabled;
                return true;
            }
            return false;
        }

        internal const string LoggingDebugId = "DebugLogging";
        internal const string LogToFileId = "LogToFile";
        internal const string LoggingSettingPageId = "setting_page_id";
    }
}