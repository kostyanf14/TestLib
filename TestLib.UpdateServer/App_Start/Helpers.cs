using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace TestLib.UpdateServer.App_Start
{
	public static class Helpers
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void StartApplication(HttpServerUtility server)
		{
			var logPath = server.MapPath("~/Logs");
			if (!Directory.Exists(logPath))
				Directory.CreateDirectory(logPath);

			logger.Info("Application initialization complete");
		}

		private static byte[] HashHMAC(byte[] key, byte[] message)
		{
			var hash = new HMACSHA256(key);
			return hash.ComputeHash(message);
		}

		public static bool CheckRequestToken(string token)
        {
			var tokenParts = token.Split('$');
			if (tokenParts.Length != 2)
				return false;

			string validHash = BitConverter.ToString(
				HashHMAC(
					Encoding.ASCII.GetBytes(Configuration.Get().AuthSecret),
					Encoding.ASCII.GetBytes(tokenParts.First())))
				.Replace("-", "");

			return validHash.ToUpper() == tokenParts.Last().ToUpper();
		}
	}
}