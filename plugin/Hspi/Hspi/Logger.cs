using HomeSeer.PluginSdk;
using NLog;
using NLog.Targets;
using System;
using System.Globalization;
using System.IO;

#nullable enable

namespace Hspi
{
    internal static class Logger
    {
 
        public static void ConfigureLogging(bool enableLogging,
                                            bool logToFile,
                                            IHsController? hsController = null)
        {
            var config = new NLog.Config.LoggingConfiguration
            {
                DefaultCultureInfo = CultureInfo.InvariantCulture
            };
            var logconsole = new ConsoleTarget("logconsole");

            LogLevel minLevel = enableLogging ? LogLevel.Debug : LogLevel.Info;
            config.AddRule(enableLogging ? LogLevel.Debug : LogLevel.Info, LogLevel.Fatal, logconsole);

            if (hsController != null)
            {
                var hsTarget = new HomeSeerTarget(hsController);
                config.AddRule(minLevel, LogLevel.Fatal, hsTarget);
            }

            if (logToFile)
            {
                string hsDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                string logFile = Path.Combine(hsDir, "logs", PlugInData.PlugInId, "file.log");

                var fileTarget = new FileTarget()
                {
                    FileNameKind = FilePathKind.Absolute,
                    CreateDirs = true,
                    MaxArchiveDays = 7,
                    MaxArchiveFiles = 7,
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveAboveSize = 10 * 1024 * 1024,
                    ConcurrentWrites = false,
                    ArchiveNumbering = ArchiveNumberingMode.Rolling,
                    FileName = logFile,
                    KeepFileOpen = true,
                };

                config.AddRule(minLevel, LogLevel.Fatal, fileTarget);
            }
            NLog.LogManager.Configuration = config;
        }

 
        [Target("homeseer")]
        public sealed class HomeSeerTarget : Target
        {
            public HomeSeerTarget(IHsController hsController)
            {
                this.loggerWeakReference = new WeakReference<IHsController>(hsController);
            }

            protected override void Write(LogEventInfo logEvent)
            {
                if (loggerWeakReference.TryGetTarget(out var logger))
                {
                    if (logEvent.Level.Equals(LogLevel.Debug))
                    {
                        logger?.WriteLog(HomeSeer.PluginSdk.Logging.ELogType.Debug, logEvent.FormattedMessage, PlugInData.PlugInName);
                    }
                    else if (logEvent.Level.Equals(LogLevel.Info))
                    {
                        logger?.WriteLog(HomeSeer.PluginSdk.Logging.ELogType.Info, logEvent.FormattedMessage, PlugInData.PlugInName);
                    }
                    else if (logEvent.Level.Equals(LogLevel.Warn))
                    {
                        logger?.WriteLog(HomeSeer.PluginSdk.Logging.ELogType.Warning, logEvent.FormattedMessage, PlugInData.PlugInName, "#D58000");
                    }
                    else if (logEvent.Level >= LogLevel.Error)
                    {
                        logger?.WriteLog(HomeSeer.PluginSdk.Logging.ELogType.Error, logEvent.FormattedMessage, PlugInData.PlugInName, "#FF0000");
                    }
                }
            }

            private readonly WeakReference<IHsController> loggerWeakReference;
        }
    }
}