using System.Collections.Generic;
using TestLib.Worker.ClientApi.Models;

namespace TestLib.Worker.ClientApi
{
	interface IApiClient
	{
		Problem DownloadProblem(ulong problemId);
		ProblemFile DownloadSolution(Submission submission);
		bool FailSubmissions(ulong id);
		IEnumerable<Submission> GetSubmissions();
		bool ReleaseSubmissions(ulong id);
		bool SendTestingResult(TestResult result);
		bool TakeSubmissions(ulong id);
	}
}