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

		private static void SendResults(CancellationToken token)
		{
			IApiClient client = new HttpCodelabsApiClient();
			var logger = LogManager.GetCurrentClassLogger();
			Application app = Application.Get();

			token.Register(() => app.Requests.Enqueue(null));

			while (true)
			{
				var request = app.Requests.Dequeue();
				if (request is null)
					break;

				try
				{
					client.SendRequest(request);
				}
				catch (Exception ex)
				{
					logger.Error("Error sending request {0} to server: {1}. Some data will be lose", request.RequestUri, ex);
				}
			}

			token.ThrowIfCancellationRequested();
		}

		static CancellationTokenSource cancellationTokenSource = null;
		static Task[] workerTasks = null;

		static void start()
		{
			cancellationTokenSource = new CancellationTokenSource();

			workerTasks[0] =
					Task.Run(() => SendResults(cancellationTokenSource.Token), cancellationTokenSource.Token);

			for (uint i = 1; i <= Application.Get().Configuration.WorkerSlotCount; i++)
			{
				var s = new Slot(i, cancellationTokenSource.Token);
				workerTasks[i] =
					Task.Run(() => s.Do(), cancellationTokenSource.Token);
			}
		}
		private static void stop()
		{
			cancellationTokenSource.Cancel();
			writeStatus();

			Console.WriteLine("Waiting stoping all tasks");
			try
			{ Task.WaitAll(workerTasks); }
			catch { }
		}

		static void writeStatus()
		{
			Console.WriteLine("==========STATUS==========");
			for (uint i = 0; i <= Application.Get().Configuration.WorkerSlotCount; i++)
				Console.WriteLine("Task {0}: {1}", i, workerTasks[i]?.Status.ToString());
		}

		static Version version()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			Console.WriteLine("Version: {0}", fvi.ProductVersion);

			return new Version(fvi.ProductMajorPart, fvi.ProductMinorPart,
				fvi.ProductBuildPart, fvi.ProductPrivatePart);
		}

		private static void Main(string[] args)
		{
			Application app = Application.Get();
			logger.Info("TestLib.Worker started");

			workerTasks = new Task[app.Configuration.WorkerSlotCount + 1];
			start();

			for (; ; )
			{
				var cmd = Console.ReadLine().ToLower();

				if (cmd == "exit")
				{
					stop();
					break;
				}

				if (cmd == "status")
					writeStatus();

				if (cmd == "start")
					start();

				if (cmd == "stop")
					stop();

				if (cmd == "version")
				{
					version();
					continue;
				}

				if (cmd == "clear problem")
				{
					app.Problems.Clear();
					Console.WriteLine("Problem cache was cleared");
				}

				writeStatus();
			}

			app.LoggerManaged.Destroy();
		}
	}
}
