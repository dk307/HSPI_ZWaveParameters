using HomeSeer.Jui.Views;

#nullable enable

namespace Hspi
{
    internal class SettingsPages
    {
        public SettingsPages(SettingsCollection collection)
        {
            DebugLoggingEnabled = collection[SettingPageId].GetViewById<ToggleView>(LoggingDebugId).IsEnabled;
            LogtoFileEnabled = collection[SettingPageId].GetViewById<ToggleView>(LogToFileId).IsEnabled;
            PreferOnlineDatabase = collection[SettingPageId].GetViewById<ToggleView>(PreferOnlineDatabaseId).IsEnabled;
            ShowSubParameteredValuesAsHex = collection[SettingPageId].GetViewById<ToggleView>(ShowSubParameteredValuesAsHexId).IsEnabled;
        }

        public bool DebugLoggingEnabled { get; private set; }

        public bool LogtoFileEnabled { get; private set; }

        public bool PreferOnlineDatabase { get; private set; }
        public bool ShowSubParameteredValuesAsHex { get; private set; }

        public static Page CreateDefault(bool preferOnlineDatabaseDefault = false,
                                         bool enableDebugLoggingDefault = false,
                                         bool logToFileDefault = false,
                                         bool showSubParameteredValuesAsHexDefault = false)
        {
            var settings = PageFactory.CreateSettingsPage(SettingPageId, "Settings");
            settings = settings.WithToggle(PreferOnlineDatabaseId, "Prefer online database", preferOnlineDatabaseDefault);
            settings = settings.WithToggle(LoggingDebugId, "Enable debug logging", enableDebugLoggingDefault);
            settings = settings.WithToggle(LogToFileId, "Log to file", logToFileDefault);
            settings = settings.WithToggle(ShowSubParameteredValuesAsHexId, "Show Parameters with Bitmask as Hexadecimal", showSubParameteredValuesAsHexDefault);

            return settings.Page;
        }

        public bool OnSettingChange(AbstractView changedView)
        {
            if (changedView.Id == PreferOnlineDatabaseId)
            {
                PreferOnlineDatabase = ((ToggleView)changedView).IsEnabled;
                return true;
            }

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

            if (changedView.Id == ShowSubParameteredValuesAsHexId)
            {
                ShowSubParameteredValuesAsHex = ((ToggleView)changedView).IsEnabled;
                return true;
            }
            return false;
        }

        internal const string LoggingDebugId = "DebugLogging";
        internal const string LogToFileId = "LogToFile";
        internal const string PreferOnlineDatabaseId = "PreferOnlineDatabase";
        internal const string ShowSubParameteredValuesAsHexId = "ShowSubParameteredValuesAsHex";
        internal const string SettingPageId = "setting_page_id";
    }
}