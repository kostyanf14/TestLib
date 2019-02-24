﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TestLib.Worker;

namespace TestLib.WorkerService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
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
