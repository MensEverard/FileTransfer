using NLog;
using NLog.Config;
using NLog.Targets;

namespace FileTransferClient
{
    class MyLogger
    {
        public static Logger Log { get; } = LogManager.GetLogger("Global");
        private static LoggingConfiguration config = new LoggingConfiguration();

        static MyLogger()
        {
            // Targets where to log to: File and Console
            FileTarget logfile = new FileTarget("logfile") { FileName = "log.txt" };

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // Apply config           
            LogManager.Configuration = config;
        }
    }

}















