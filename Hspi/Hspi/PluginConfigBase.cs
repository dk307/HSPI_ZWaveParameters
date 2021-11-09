using HomeSeer.PluginSdk;
using System;
using System.Globalization;

#nullable enable

namespace Hspi
{
    internal class PluginConfigBase
    {
        protected PluginConfigBase(IHsController HS)
        {
            this.HS = HS;
            debugLogging = GetValue(DebugLoggingKey, false);
            logToFile = GetValue(LogToFileKey, false);
        }

        public bool DebugLogging
        {
            get
            {
                return debugLogging;
            }

            set
            {
                SetValue(DebugLoggingKey, value, ref debugLogging);
            }
        }

        public bool LogToFile
        {
            get
            {
                return logToFile;
            }

            set
            {
                SetValue(LogToFileKey, value, ref logToFile);
            }
        }

        protected static string? DecryptString(string? password)
        {
            return password;
        }

        protected static string? EncryptString(string? password)
        {
            // Chose not to do anything as encryption key anyway has to stored in the code/machine
            return password;
        }

        protected void ClearSection(string id)
        {
            HS.ClearIniSection(id, PlugInData.SettingFileName);
        }

        protected T GetValue<T>(string key, T defaultValue)
        {
            return GetValue(key, defaultValue, DefaultSection);
        }

        protected T GetValue<T>(string key, T defaultValue, string section)
        {
            string stringValue = HS.GetINISetting(section, key, null, fileName: PlugInData.SettingFileName);

            if (stringValue != null)
            {
                try
                {
                    T result = (T)System.Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);
                    return result;
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        protected void SetValue<T>(string key, T value, string section = DefaultSection)
        {
            string stringValue = System.Convert.ToString(value, CultureInfo.InvariantCulture);
            HS.SaveINISetting(section, key, stringValue, fileName: PlugInData.SettingFileName);
        }

        protected void SetValue<T>(string key, Nullable<T> value, string section = DefaultSection) where T : struct
        {
            string stringValue = value.HasValue ? System.Convert.ToString(value.Value, CultureInfo.InvariantCulture) : string.Empty;
            HS.SaveINISetting(section, key, stringValue, fileName: PlugInData.SettingFileName);
        }

        protected void SetValue<T>(string key, T value, ref T oldValue)
        {
            SetValue<T>(key, value, ref oldValue, DefaultSection);
        }

        protected void SetValue<T>(string key, T value, ref T oldValue, string section)
        {
            if (!object.Equals(value, oldValue))
            {
                string stringValue = System.Convert.ToString(value, CultureInfo.InvariantCulture);
                HS.SaveINISetting(section, key, stringValue, fileName: PlugInData.SettingFileName);
                oldValue = value;
            }
        }

        private const string LogToFileKey = "LogToFile";
        private const string DebugLoggingKey = "DebugLogging";
        private const string DefaultSection = "Settings";
        private readonly IHsController HS;
        private bool debugLogging;
        private bool logToFile;
    };
}