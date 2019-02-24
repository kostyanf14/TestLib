using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TestLib.Worker;

namespace TestLib.WorkerService
{
	public partial class WorkerService : ServiceBase
	{
		public WorkerService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			Application.Get().Start();
		}

		protected override void OnStop()
		{
			Application.Get().End();
		}
	}
}
