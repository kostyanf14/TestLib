using NLog;
using System;

namespace TestLib.Worker
{
	internal class Program
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		private static void Main(string[] args)
		{
			var x = System.Net.HttpStatusCode.NotFound.ToString();

			logger.Info("TestLib.Worker started");
			Application app = Application.Get();
			if (!app.Init())
				return;

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
					Console.WriteLine("Version: {0}", app.GetVersion());

				if (cmd == "clear problem")
				{
					app.Problems.Clear();
					Console.WriteLine("Problem cache was cleared");
				}

				if (cmd == "update")
					if (app.Update() == CheckUpdateStatus.Restart)
						cmd = "exit";
			}

			app.End();
		}
	}
}
