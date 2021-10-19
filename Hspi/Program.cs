namespace Hspi
{
    /// <summary>
    /// Class for the main program.
    /// </summary>
    public static class Program
    {
        private static void Main(string[] args)
        {
            Logger.ConfigureLogging(false, false);
            logger.Info("Starting...");

            try
            {
                using var plugin = new HSPI_ZWaveInformation.HSPI();
                plugin.Connect(args);
            }
            finally
            {
                logger.Info("Bye!!!");
            }
        }

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}