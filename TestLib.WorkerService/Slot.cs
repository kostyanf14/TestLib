using NLog;
using System;
using System.Linq;
using System.Threading;
using TestLib.Worker.ClientApi;

namespace TestLib.Worker
{
	internal class Slot
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public Slot(uint slotNumber)
		{
			this.slotNumber = slotNumber;
			client = new HttpCodelabsApiClient();

			logger.Info("Slot {0} created", slotNumber);
		}

		public void Do(CancellationToken token)
		{
			Application app = Application.Get();

			while (!token.IsCancellationRequested)
			{
				var submissions = client.GetSuitableSubmissions(app.Compilers.GetCompilers());
				if (!submissions.Any())
				{
					Thread.Sleep(app.Configuration.GetSubmissionDelayMs);
					continue;
				}

				var submission = submissions.First();
				if (!client.SendRequest(client.GetTakeSubmissionsRequestMessage(submission.Id)))
				{
					continue;
				}

				logger.Info("Testing slot {0} taken submission with id {1}", slotNumber, submission.Id);
				logger.Debug("Submission: {0}", submission);

				ProblemFile solution = client.DownloadSolution(submission);
				app.FileProvider.SaveFile(solution);

				Problem problem = app.Problems.FetchProblem(
					submission.ProblemId, submission.ProblemUpdatedAt,
					() =>
					{
						logger.Debug("Need download problem with id {0}", submission.ProblemId);
						return client.DownloadProblem(submission.ProblemId);
					}
				);

				Worker worker = new Worker(slotNumber, client);
				WorkerResult result;

				try
				{
					result = worker.Testing(submission, problem, solution);
				}
				catch (Exception ex)
				{
					logger.Error("Slot {0} worker testing failed with exception {1}. Error {2}", slotNumber, ex.GetType().Name, ex);
					result = WorkerResult.TestingError;
				}

				switch (result)
				{
					case WorkerResult.Ok:
					case WorkerResult.CompilerError:
						app.RequestMessages.Enqueue(client.GetReleaseSubmissionsRequestMessage(submission.Id, result));
						break;
					case WorkerResult.TestingError:
						app.RequestMessages.Enqueue(client.GetFailSubmissionsRequestMessage(submission.Id));
						break;
				}

			}

			token.ThrowIfCancellationRequested();
		}

		private readonly uint slotNumber;
		private HttpCodelabsApiClient client;
	}
}
