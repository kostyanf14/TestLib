using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using TestLib.Worker.ClientApi.Models;

namespace TestLib.Worker.ClientApi
{
	internal class HttpCodelabsApiClient : IApiClient
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();
		private HttpClient client;
		public HttpCodelabsApiClient()
		{
			client = new HttpClient();

			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.ConnectionClose = false;

			//client.Headers.Add(HttpRequestHeader.Accept, "*/*");

			logger.Info("ApiClient initialized");
		}

		private IEnumerable<Submission> readSubmissions(HashSet<byte> compilers = null)
		{
			string endpoint = buildEndpoint("submissions");
			string jsonSubmissions = client.GetStringAsync(endpoint).Result;

			JArray parsedSubmissions = JArray.Parse(jsonSubmissions);
			List<Submission> submissions = new List<Submission>();
			logger.Debug("Returned {0} submissions", parsedSubmissions.Count);

			byte compilerId = 1;
			byte checkerCompilerId = 1;
			for (int i = 0; i < parsedSubmissions.Count; i++)
			{
				compilerId = (byte)parsedSubmissions[i]["compiler_id"];
				checkerCompilerId = (byte)parsedSubmissions[i]["problem"]["checker_compiler_id"];

				if (compilers is null
					|| (compilers.Contains(compilerId) && compilers.Contains(checkerCompilerId)))
				{
					submissions.Add(
						new Submission(
							id: (ulong)parsedSubmissions[i]["id"],
							sourceUrl: (string)parsedSubmissions[i]["source_url"],
							compilerId: compilerId,
							checkerCompilerId: checkerCompilerId,
							problemId: (ulong)parsedSubmissions[i]["problem"]["id"],
							problemUpdatedAt: (DateTime)parsedSubmissions[i]["problem"]["updated_at"],
							memoryLimit: (UInt32)parsedSubmissions[i]["memory_limit"],
							timeLimit: (UInt32)parsedSubmissions[i]["time_limit"]
						)
					);
				}
			}

			logger.Debug("Selected {0} submissions", submissions.Count);
			return submissions;
		}
		public IEnumerable<Submission> GetSubmissions()
			=> readSubmissions();
		public IEnumerable<Submission> GetSuitableSubmissions(HashSet<byte> compilers)
			=> readSubmissions(compilers);

		private ProblemFile downloadFile(string url)
		{
			Uri uri = new Uri(Application.Get().Configuration.BaseApiAddress, url);

			ProblemFile solution = new ProblemFile();
			solution.Content = client.GetAsync(uri).Result.Content.ReadAsByteArrayAsync().Result;
			return solution;
		}
		public ProblemFile DownloadSolution(Submission submission) =>
			downloadFile(submission.SourceUrl);

		public Problem DownloadProblem(ulong problemId)
		{
			string endpoint = buildEndpoint("problems", problemId);
			string jsonProblem = client.GetStringAsync(endpoint).Result;
			JToken parsedProblem = JToken.Parse(jsonProblem);

			Test[] tests = new Test[parsedProblem["tests"].Count()];
			for (int i = 0; i < parsedProblem["tests"].Count(); i++)
			{
				Test t = new Test();

				t.Id = (UInt64)parsedProblem["tests"][i]["id"];
				t.Num = (string)parsedProblem["tests"][i]["num"];
				t.Input = downloadFile(
					(string)parsedProblem["tests"][i]["input_url"]
				);

				if (!(parsedProblem["tests"][i]["answer_url"] is null))
				{
					t.Answer = downloadFile(
					(string)parsedProblem["tests"][i]["answer_url"]
				);
				}

				tests[i] = t;
			}

			ProblemFile checker = downloadFile((string)parsedProblem["checker_source_url"]);

			return new Problem(
				id: (ulong)parsedProblem["id"],
				tests: tests,
				checker: checker,
				checkerCompilerId: (byte)parsedProblem["checker_compiler_id"],
				lastUpdate: (DateTime)parsedProblem["updated_at"]
			);
		}

		public bool SendSubmissionLog(SubmissionLog log)
		{
			string endpoint = buildEndpoint("submissions", log.SubmissionId, "logs");
			var responseMessage = client.PostAsync(endpoint, log.AsJson()).Result;

			logger.Debug("{1} log for submission {0} send {2}", log.SubmissionId,
			   log.Type.ToString(), responseMessage.StatusCode == HttpStatusCode.NoContent ?
				"successfully" : "failed");

			if (responseMessage.StatusCode != HttpStatusCode.NoContent)
			{
				logger.Error("Sending submission log failed. Server error message: {0}",
					responseMessage.Content?.ReadAsStringAsync()?.Result);
			}

			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}

		public RequestMessage GetTakeSubmissionsRequestMessage(ulong id) =>
			new RequestMessage(buildEndpoint("submissions", id, "take"), null);

		public RequestMessage GetFailSubmissionsRequestMessage(ulong id) =>
			new RequestMessage(buildEndpoint("submissions", id, "fail"), null);

		public RequestMessage GetReleaseSubmissionsRequestMessage(ulong id, WorkerResult result) =>
			 new RequestMessage(buildEndpoint("submissions", id, "release"), new { release = new { test_result = (byte)result } }.AsJson());

		public RequestMessage GetSendTestingResultRequestMessage(TestResult result) =>
			 new RequestMessage(buildEndpoint("results"), result.AsJson());

		public RequestMessage GetSendLogRequestMessage(SubmissionLog log) =>
			 new RequestMessage(buildEndpoint("submissions", log.SubmissionId, "logs"), log.AsJson());

		public bool SendRequest(RequestMessage message)
		{
			var responseMessage = client.PostAsync(message.RequestUri, message.Data).Result;

			logger.Debug("Request {0} send {1}", message.RequestUri,
			   responseMessage.StatusCode == HttpStatusCode.NoContent ?
			   "successfully" : "failed");

			if (responseMessage.StatusCode != HttpStatusCode.NoContent)
			{
				logger.Error("Request {0} server error message: {1}. Status code: {2}", message.RequestUri,
					responseMessage.Content?.ReadAsStringAsync()?.Result,
					responseMessage.StatusCode);
			}

			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}

		public Guid SignUp(WorkerInformation worker)
		{
			string endpoint = buildEndpoint("workers");

			worker.ApiType = GetApiType();
			worker.ApiVersion = GetVersion();
			worker.WebhookSupported = GetWebhookSupported();

			var responseMessage = client.PostAsync(
				endpoint, new { worker }.AsJson()).Result;

			logger.Debug("Sign up request was send {0}",
			   responseMessage.StatusCode == HttpStatusCode.Created ?
			   "successfully" : "failed");

			if (responseMessage.StatusCode != HttpStatusCode.Created)
			{
				logger.Error("Sign up failed. Status code: {0}. Error message: {1}",
					responseMessage.StatusCode,
					responseMessage.Content?.ReadAsStringAsync()?.Result);

				return Guid.Empty;
			}

			return new Guid(
				JObject.Parse(
					responseMessage.Content.ReadAsStringAsync().Result
				)["id"].Value<string>());
		}
		public bool SignIn(Guid id)
		{
			string endpoint = buildEndpoint("workers", id.ToString(), "session");
			var responseMessage = client.PostAsync(endpoint, null).Result;

			logger.Debug("Sign in request was send {0}",
			   responseMessage.StatusCode == HttpStatusCode.NoContent ?
			   "successfully" : "failed");

			if (responseMessage.StatusCode != HttpStatusCode.NoContent)
			{
				logger.Error("Sign in failed with error message: {0}. Status code: {1}",
					responseMessage.Content?.ReadAsStringAsync()?.Result,
					responseMessage.StatusCode);
			}

			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}
		public bool SignOut(Guid id)
		{
			string endpoint = buildEndpoint("workers", id.ToString(), "session");
			var responseMessage = client.DeleteAsync(endpoint).Result;

			logger.Debug("Sign out request was send {0}",
			   responseMessage.StatusCode == HttpStatusCode.NoContent ?
			   "successfully" : "failed");

			if (responseMessage.StatusCode != HttpStatusCode.NoContent)
			{
				logger.Error("Sign out failed with error message: {0}. Status code: {1}",
					responseMessage.Content?.ReadAsStringAsync()?.Result,
					responseMessage.StatusCode);
			}

			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}
		public bool Alive(Guid id, AliveInformation alive)
		{
			string endpoint = buildEndpoint("workers", id.ToString(), "alive");

			var responseMessage = client.PostAsync(
				endpoint, new { alive }.AsJson()).Result;

			logger.Debug("Alive request was send {0}",
			   responseMessage.StatusCode == HttpStatusCode.Created ?
			   "successfully" : "failed");

			if (responseMessage.StatusCode != HttpStatusCode.Created)
			{
				logger.Error("Alive failed. Status code: {0}. Error message: {1}",
					responseMessage.StatusCode,
					responseMessage.Content?.ReadAsStringAsync()?.Result);
			}

			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}

		public uint GetVersion() => 1;
		public ApiType GetApiType() => ApiType.HTTP;
		public bool GetWebhookSupported() => false;

		private string buildEndpoint(string method, ulong id, string action = null, string parameters = null)
			=> buildEndpoint(method, id.ToString(), action, parameters);
		private string buildEndpoint(string method, string id = null, string action = null, string parameters = null)
		{
			string endpoint = $"{Application.Get().Configuration.BaseApiAddress}/{method}";

			if (!(id is null))
				endpoint += $"/{id}";

			if (!(action is null))
				endpoint += $"/{action}";

			endpoint += $"?access_token={Application.Get().Configuration.ApiAuthToken}";

			if (!(parameters is null))
				endpoint += $"&{parameters}";

			return endpoint;
		}
	}
}
