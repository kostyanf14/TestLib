using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;

namespace TestLib.UpdateServer.App_Start
{
	public static class Helpers
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private static string s_tempPath;
		private static string s_releasesPath;

		public static string TempPath { get => s_tempPath; private set => s_tempPath = value; }
		public static string ReleasesPath { get => s_releasesPath; set => s_releasesPath = value; }
		public static string ReleasesLatestVersionFilePath { get => Path.Combine(s_releasesPath, "latest.ver"); }


		static void StartApplication()
		{

		}

		public static void StartApplication(HttpServerUtility server)
		{
			var logPath = server.MapPath("~/Logs");
			if (!Directory.Exists(logPath))
				Directory.CreateDirectory(logPath);

			s_tempPath = server.MapPath("~/Temp");
			if (!Directory.Exists(s_tempPath))
				Directory.CreateDirectory(s_tempPath);

			s_releasesPath = server.MapPath("~/App_Data/Releases");
			if (!Directory.Exists(s_releasesPath))
				Directory.CreateDirectory(s_releasesPath);

			logger.Info("Application initialization complete");
		}
	}
}