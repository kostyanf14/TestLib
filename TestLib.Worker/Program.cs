using NLog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TestLib.Worker.ClientApi;

namespace TestLib.Worker
{
	internal class Program
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static void Main(string[] args)
		{
			logger.Info("TestLib.Worker started");
			Application app = Application.Get();

			app.Start();

			string cmd = null;
			for (; cmd != "exit";)
			{
				cmd = Console.ReadLine().ToLower();

				if (cmd == "status")
					app.Status();

				if (cmd == "start")
				{ app.Start(); app.Status(); }

				if (cmd == "stop")
				{ app.Stop(); app.Status(); }

				if (cmd == "version")
					Console.WriteLine("Version: {0}", app.Version());

				if (cmd == "clear problem")
				{
					app.Problems.Clear();
					Console.WriteLine("Problem cache was cleared");
				}
			}

			app.Stop();
			app.LoggerManaged.Destroy();
		}
	}
}
