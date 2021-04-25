using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace TestLib.Worker.Updater
{
	internal static class Program
	{
		private static Version getAssemblyVersion(string exePath)
		{
			var fvi = FileVersionInfo.GetVersionInfo(exePath);

			return new Version(fvi.ProductMajorPart, fvi.ProductMinorPart,
				fvi.ProductBuildPart, fvi.ProductPrivatePart);
		}

		private static void log(string msg)
		{
			File.AppendAllText("updater_log.txt", string.Format("[{0}] {1}\n", DateTime.Now.ToString(), msg));
		}

		[STAThread]
		private static void Main(string[] args)
		{
			//args[0] - pid
			//args[1] - Directory
			//args[2] - "TestLib.Worker.exe"

			if (args.Length != 3)
			{
				log(string.Format("Count of arguments must be 3. Incorrect argument list."));
				return;
			}

			Process process;
			int processId = int.Parse(args[0]);
			try
			{
				process = Process.GetProcessById(processId);
			}
			catch (Exception ex)
			{
				process = null;
				log(string.Format("Failed to get process by Id {0}. Waiting skipped. Exception: {1}", processId, ex));
			}
			try
			{
				if (process != null)
					process.WaitForExit();
			}
			catch (Exception ex)
			{
				log(string.Format("Error waiting for process {0}({1}) end. Exception: {2}", process?.ProcessName, process?.Id, ex));
				return;
			}

			WebClient client = new WebClient();

			string exeDir = args[1];
			string exePath = Path.Combine(exeDir, args[2]);

			Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
			string latestUpdateUrl =
				config?.AppSettings?.Settings["latest_update_url"]?.Value;

			if (string.IsNullOrEmpty(latestUpdateUrl))
			{
				log(string.Format("Apllication update server is not spesified. Update is impossible."));
				return;
			}

			JToken jsonVersion = JToken.Parse(client.DownloadString(latestUpdateUrl));
			string stringVersion = (string)jsonVersion["version"];
			string latestBinaryUrl = (string)jsonVersion["binary_url"];
			if (!Version.TryParse(stringVersion, out var latestVersion))
			{
				log(string.Format("Can't parse latest version {0} from update server {1}. Update not available.", stringVersion, latestUpdateUrl));
				return;
			}

			var currentVersion = getAssemblyVersion(exePath);
			bool need = latestVersion > currentVersion;

			var msg = string.Format("Latest version {0}, current version {1}: update is{2}necessary.",
				latestVersion, currentVersion, need ? " " : " not ");
			log(msg);

			if (!need)
			{
				return;
			}

			var tmpFile = Path.GetTempFileName();

			try
			{
				client.DownloadFile(latestBinaryUrl, tmpFile);
			}
			catch (Exception ex)
			{
				log(string.Format("Failed to download latest binary from {0}. Exception: {1}", latestBinaryUrl, ex));
				return;
			}

			var archive = ZipFile.OpenRead(tmpFile);
			foreach (var entry in archive.Entries)
			{
				var path = Path.Combine(exeDir, entry.FullName);

				if (entry.FullName.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				entry.ExtractToFile(path, true);
			}
		}
	}
}