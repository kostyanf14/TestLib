using NLog;
using System;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;
using TestLib.Worker;

namespace TestLib.WorkerService
{
	internal static class Program
	{
		private static Logger logger;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		private static void Main(string[] args)
		{
			logger = LogManager.GetCurrentClassLogger();

			if (Environment.UserInteractive)
			{
				logger.Info("Application must run as service, but run at user mode with {0} arguments.", args.Length);

				for (int args_i = 0; ; args_i++)
				{
					string command = args_i < args.Length ? args[args_i] : Console.ReadLine();
					if (string.IsNullOrWhiteSpace(command))
						continue;

					switch (command)
					{
						case "install":
							logger.Info("Starting install service");

							ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
							break;
						case "uninstall":
							logger.Info("Starting uninstall service");

							ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
							break;
						case "update":
							logger.Info("Starting checking update");

							if (Application.Get().Update() == CheckUpdateStatus.Restart)
								return;
							break;
						case "start":
							if (!Application.Get().Initialized)
								if (!Application.Get().Init())
									break;
							Application.Get().Start();
							break;
						case "stop":
							Application.Get().Stop();
							break;
						case "exit":
							Application.Get().Stop();
							return;
						default:
							logger.Info("Command '{0}' is unknown", command);
							break;
					}
				}
			}
			else
			{
				logger.Info("Application run as service");

				if (!Application.Get().Init())
					return;

				ServiceBase[] ServicesToRun;
				ServicesToRun = new ServiceBase[]
				{
					new WorkerService()
				};
				ServiceBase.Run(ServicesToRun);
			}
		}
	}
}
