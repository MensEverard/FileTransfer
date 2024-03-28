using NLog;
using NLog.Config;
using NLog.Targets;

namespace FileTransferClient
{
    class MyLogger
    {
        public static Logger Log { get; } = LogManager.GetLogger(" - ");
        private static LoggingConfiguration config = new LoggingConfiguration();

        static MyLogger()
        {
            // Targets where to log to: File and Console
            FileTarget Infologfile = new FileTarget("logfile") { FileName = "Info.log" };
            FileTarget Debuglogfile = new FileTarget("logfile") { FileName = "Debug.log" };
            FileTarget Errorlogfile = new FileTarget("logfile") { FileName = "Error.log" };
            

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Info, Infologfile);
            config.AddRule(LogLevel.Debug, LogLevel.Debug, Debuglogfile);
            config.AddRule(LogLevel.Error, LogLevel.Fatal, Errorlogfile);

            // Apply config           
            LogManager.Configuration = config;
        }
    }

}















