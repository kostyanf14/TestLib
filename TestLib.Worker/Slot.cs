using NLog;
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
				if (!client.TakeSubmissions(submission.Id))
					continue;
				logger.Info("Testing slot {0} taken submission with id {1}", 1, submission.Id);
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

				Worker worker = new Worker(slotNumber);
				if (worker.Testing(submission, problem, solution))
					client.ReleaseSubmissions(submission.Id);
				else
					client.FailSubmissions(submission.Id);
			}
		}

		uint slotNumber;
		CancellationToken token;
		HttpCodelabsApiClient client;
	}
}
