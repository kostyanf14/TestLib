using NLog;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestLib.Worker.ClientApi;

namespace TestLib.Worker
{
	class Program
	{
		static Logger logger = LogManager.GetCurrentClassLogger();

		static void SendResults(CancellationToken token)
		{
			IApiClient client = new HttpCodelabsApiClient();
			Application app = Application.Get();

			while (!token.IsCancellationRequested)
			{
				var result = app.TestingResults.Dequeue();
				client.SendTestingResult(result);
			}
		}

		static void Main(string[] args)
		{
			Application app = Application.Get();
			LoggerManaged loggerManaged = new LoggerManaged();

			logger.Info("TestLib.Worker started");
			loggerManaged.InitNativeLogger(new LoggerManaged.LogEventHandler(logger.Log));

			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			Task[] workerTasks = new Task[app.Configuration.WorkerSlotCount + 1];

			workerTasks[0] = 
				Task.Run(() => { SendResults(cancellationTokenSource.Token); }, cancellationTokenSource.Token);

			for (uint i = 1; i <= app.Configuration.WorkerSlotCount; i++)
				workerTasks[i] = 
					Task.Run(() => { new Slot(i, cancellationTokenSource.Token).Do(); }, cancellationTokenSource.Token);

			for (; ; )
			{
				var cmd = Console.ReadLine();

				if (cmd == "exit")
				{
					cancellationTokenSource.Cancel();
					break;
				}
			}

			Task.WaitAll(workerTasks);

			loggerManaged.Destroy();
		}
	}
}
