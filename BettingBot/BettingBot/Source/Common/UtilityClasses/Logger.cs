using System.Reflection;
using NLog;

namespace BettingBot.Source.Common.UtilityClasses
{
    public class Logger
    {
        public static void Create()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "ErrorLog.log",
                Layout = "${longdate}: ${level:uppercase=true} | ${logger} | ${message}"
            };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            
            NLog.LogManager.Configuration = config;
        }

        public static NLog.Logger Get(MethodBase method) => LogManager.GetLogger(method.DeclaringType?.FullName);

        public static void Close()
        {
            NLog.LogManager.Shutdown();
        }
    }
}
