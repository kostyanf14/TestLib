using NLog;

namespace TestLib.Worker
{
	internal static class LoggerExtention
	{
		public static void Log(this Logger logger, LoggerManaged sender, LoggerManaged.LogLevel logLevel, string message)
		{
			switch (logLevel)
			{
				case LoggerManaged.LogLevel.Trace:
					logger.Trace(message);
					break;
				case LoggerManaged.LogLevel.Debug:
					logger.Debug(message);
					break;
				case LoggerManaged.LogLevel.Info:
					logger.Info(message);
					break;
				case LoggerManaged.LogLevel.Warn:
					logger.Warn(message);
					break;
				case LoggerManaged.LogLevel.Error:
					logger.Error(message);
					break;
				case LoggerManaged.LogLevel.Fatal:
					logger.Fatal(message);
					break;
				case LoggerManaged.LogLevel.Off:
					break;
			}
		}
	}
}
