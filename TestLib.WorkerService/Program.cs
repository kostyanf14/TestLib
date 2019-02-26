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

				if (args.Length > 0)
				{
					switch (args[0])
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

							Application.Get().Update();
							break;
						default:
							logger.Info("Unknown command.");
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
