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

		[STAThread]
		private static void Main(string[] args)
		{
			//args[0] - pid
			//args[1] - Directory
			//args[2] - "TestLib.Worker.exe"

			if (args.Length != 3)
			{
				File.AppendAllText("log.txt", 
					string.Format("Count of arguments must be 3. Incorrect argument list."));
				return;
			}

			Process process = null;
			int processId = int.Parse(args[0]);
			try
			{
				process = Process.GetProcessById(processId);
			}
			catch (Exception ex)
			{
				process = null;
				File.AppendAllText("log.txt",
					string.Format("Failed to get process by Id {0}. Waiting skipped. Ex: {1}",
					processId, ex));
			}
			try
			{
				if (process != null)
					process.WaitForExit();
			}
			catch (Exception ex)
			{
				File.AppendAllText("log.txt",
					string.Format("Error waiting for process {0}({1}) end: {2}",
					process?.ProcessName, process?.Id, ex));
				return;
			}

			WebClient client = new WebClient();

			string exeDir = args[1];
			string exePath = Path.Combine(exeDir, args[2]);

			Configuration config = ConfigurationManager.OpenExeConfiguration(exePath);
			string latestVersionUrl =
				config?.AppSettings?.Settings["update_latest_version_url"]?.Value;
			string latestProgramUrl =
				config?.AppSettings?.Settings["update_latest_program_url"]?.Value;

			if (string.IsNullOrEmpty(latestVersionUrl) || string.IsNullOrEmpty(latestProgramUrl))
            {
				File.AppendAllText("log.txt",
					string.Format("Apllication update server is not spesified. Update is impossible.",
					latestVersionUrl, latestProgramUrl));

				return;
			}

			string stringVersion = client.DownloadString(latestVersionUrl).Replace("\"", "");
			if (!Version.TryParse(stringVersion, out var latestVersion))
			{
				File.AppendAllText("log.txt",
					string.Format("Can't parse latest version {0} from update server {1}. Update not available.",
					stringVersion, latestVersionUrl));

				return;
			}

			var currentVersion = getAssemblyVersion(exePath);
			bool need = latestVersion > currentVersion;

			var msg = string.Format("Latest version {0}, current version {1}: update is{2}necessary.",
				latestVersion, currentVersion, need ? " " : " not ");
			File.AppendAllText("log.txt", msg);

			if (!need)
			{
				return;
			}

			var tmpFile = Path.GetTempFileName();
			client.DownloadFile(latestProgramUrl, tmpFile);

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