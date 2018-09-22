using NLog;
using System;
using System.Linq;
using System.Threading;
using TestLib.Worker.ClientApi;

namespace TestLib.Worker
{
	class Slot
	{
		static Logger logger = LogManager.GetCurrentClassLogger();

		public Slot(uint slotNumber, CancellationToken token)
		{
			this.slotNumber = slotNumber;
			this.token = token;
			client = new HttpCodelabsApiClient();

			logger.Info("Slot {0} created", slotNumber);
		}

		public void Do()
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
					continue;

				logger.Info("Testing slot {0} taken submission with id {1}", slotNumber, submission.Id);
				logger.Debug("Submission: {0}", submission);

				ProblemFile solution = client.DownloadSolution(submission);
				app.FileProvider.SaveFile(solution);

				Problem problem = null;
				if (!Application.Get().Problems.CheckProblem(submission.ProblemId, submission.ProblemUpdatedAt))
				{
					logger.Debug("Need download problem with id {0}", submission.ProblemId);
					app.Problems.AddProblem(problem = client.DownloadProblem(submission.ProblemId));
				}
				else
					problem = app.Problems.GetProblem(submission.ProblemId);

				Worker worker = new Worker(slotNumber, client);
                WorkerResult result;

                try
                {
                    result = worker.Testing(submission, problem, solution);
                }
                catch(Exception ex)
                {
                    logger.Error("Slot {0} worker testing failed with exception {1}. Error {2}", slotNumber, ex.GetType().Name, ex);
                    result = WorkerResult.TestingError;
                }

                switch (result)
                {
                    case WorkerResult.Ok:
                    case WorkerResult.CompilerError:
                        app.Requests.Enqueue(client.GetReleaseSubmissionsRequestMessage(submission.Id, result));
                        break;
                    case WorkerResult.TestingError:
                        app.Requests.Enqueue(client.GetFailSubmissionsRequestMessage(submission.Id));
                        break;
                }
                           
			}

			token.ThrowIfCancellationRequested();
		}

		uint slotNumber;
		CancellationToken token;
		HttpCodelabsApiClient client;
	}
}
