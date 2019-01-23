using System;
using System.Collections.Generic;
using TestLib.Worker.ClientApi.Models;

namespace TestLib.Worker.ClientApi
{
	interface IApiClient
	{
		Guid SignUp(WorkerInformation worker);
		bool SignIn(Guid id);
		bool SignOut(Guid id);
		bool Alive(Guid id, AliveInformation alive);

		Problem DownloadProblem(ulong problemId);
		ProblemFile DownloadSolution(Submission submission);
		IEnumerable<Submission> GetSubmissions();

		RequestMessage GetTakeSubmissionsRequestMessage(ulong id);
		RequestMessage GetFailSubmissionsRequestMessage(ulong id);
		RequestMessage GetReleaseSubmissionsRequestMessage(ulong id, WorkerResult result);
		RequestMessage GetSendTestingResultRequestMessage(TestResult result);
		RequestMessage GetSendLogRequestMessage(SubmissionLog log);

		bool SendRequest(RequestMessage message);

		uint GetVersion();
		ApiType GetApiType();
		bool GetWebhookSupported();
	}
}