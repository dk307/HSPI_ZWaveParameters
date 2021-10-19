using System.IO;

#nullable enable

namespace Hspi
{
    /// <summary>
    /// Class to store static data
    /// </summary>
    internal static class PlugInData
    {
        
        /// <summary>
        /// The plugin name
        /// </summary>
        public const string Hs3PlugInName = @"ZWaveInformation";

        /// <summary>
        /// The plugin Id
        /// </summary>
        public const string PlugInId = @"ZWaveInformation";

        /// <summary>
        /// The plugin name
        /// </summary>
        public const string PlugInName = @"ZWave Information";
        /// <summary>
        /// The plugin Id
        /// </summary>
        public const string SettingFileName = @"HSPI_ZWaveInformation.ini";

        public readonly static string HomeSeerDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

    }
}