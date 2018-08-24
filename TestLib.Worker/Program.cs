using NLog;
using System;
using System.Collections.Specialized;
using System.Linq;
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
	class Slot
	{

	}

	class Program
	{
		static string WorkerName = "Worker teachk23";
		static int SlotCount = 4;

		static Logger logger = LogManager.GetCurrentClassLogger();
		static void Main(string[] args)
		{
			Application app = Application.Get();
			LoggerManaged loggerManaged = new LoggerManaged();

			logger.Info("TestLib.Worker started");
			loggerManaged.InitNativeLogger(new LoggerManaged.LogEventHandler(logger.Log));

			loggerManaged.Check();


			HttpCodelabsApiClient client = new HttpCodelabsApiClient();
			CancellationTokenSource cancellationToken = new CancellationTokenSource();

			Task.Run(() =>
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					var x = app.TestingResults.Dequeue();
					client.SendTestingResult(x);
				}
			}, cancellationToken.Token);

			while (!cancellationToken.IsCancellationRequested)
			{
				var submissions = client.GetSuitableSubmissions(app.Compilers.GetCompilers());
				if (!submissions.Any())
				{
					Thread.Sleep(app.Configuration.GetSubmissionDelayMs);
					continue;
				}
				loggerManaged.Check();

				var submission = submissions.First();
				if (!client.TakeSubmissions(submission.Id))
					continue;
				logger.Info("Testing slot {0} taken submission with id {1}", 1, submission.Id);
				logger.Debug("Submission: {0}", submission);

				ProblemFile solution = client.DownloadSolution(submission);
				app.FileProvider.SaveFile(solution);

				Problem problem = null;
				if (!app.Problems.CheckProblem(submission.ProblemId, submission.ProblemUpdatedAt))
				{
					logger.Debug("Need download problem with id {0}", submission.ProblemId);
					app.Problems.AddProblem(problem = client.DownloadProblem(submission.ProblemId));
				}
				else
					problem = app.Problems.GetProblem(submission.ProblemId);


				Worker worker = new Worker(1);
				loggerManaged.Check();
				if (worker.Testing(submission, problem, solution))
					client.ReleaseSubmissions(submission.Id);
				else
					client.FailSubmissions(submission.Id);

				var z = loggerManaged.AsJson().ToString();
				System.Console.WriteLine(z);

			}

			loggerManaged.Destroy();

		}
	}
}
