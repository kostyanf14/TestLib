using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using TestLib.Worker.ClientApi.Models;
using TestLib.WorkerService;

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

		private IEnumerable<Submission> readSubmissions(bool retryOnFailed = true, HashSet<byte> compilers = null)
		{
			string endpoint = buildEndpoint("submissions");
			string jsonSubmissions;
			List<Submission> submissions = null;

			TimeOutHelper time = new TimeOutHelper();
			do
			{
				try
				{
					jsonSubmissions = client.GetStringAsync(endpoint).Result;
					JArray parsedSubmissions = JArray.Parse(jsonSubmissions);
					submissions = new List<Submission>();
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

					break;
				}
				catch (Exception ex)
				{
					logger.Error(ex, "GetSubmissions failed with exception.");
					Thread.Sleep(time.GetTimeOut() * 1000);
				}

			} while (retryOnFailed);

			return submissions;
		}
		public IEnumerable<Submission> GetSubmissions(bool retryOnFailed = true)
			=> readSubmissions(retryOnFailed: retryOnFailed);
		public IEnumerable<Submission> GetSuitableSubmissions(HashSet<byte> compilers, bool retryOnFailed = true)
			=> readSubmissions(retryOnFailed, compilers);

		private ProblemFile downloadFile(string url)
		{
			Uri uri = new Uri(Application.Get().Configuration.BaseApiAddress, url);

			logger.Debug("Statring download file from {0}", uri);

			ProblemFile file = new ProblemFile();
			for (int i = 0; i < 2; i++)
			{
				try
				{
					file.Content = client.GetAsync(uri).Result.Content.ReadAsByteArrayAsync().Result;
					break;
				}
				catch (Exception ex)
				{
					file.Content = null;
					logger.Error(ex, "Download file failed.");
				}
			}

			if (file.Content is null)
			{
				return null;
			}

			return file;
		}
		public ProblemFile DownloadSolution(Submission submission) =>
			downloadFile(submission.SourceUrl);

		public Problem DownloadProblem(ulong problemId)
		{
			string endpoint = buildEndpoint("problems", problemId);
			string jsonProblem = client.GetStringAsync(endpoint).Result;

			logger.Debug("Download problem response length: {0}", jsonProblem.Length);

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

				if (t.Input is null)
				{
					return null;
				}
				else
				{
					Application.Get().FileProvider.SaveFile(t.Input);
				}

				if (!(parsedProblem["tests"][i]["answer_url"] is null))
				{
					t.Answer = downloadFile(
						(string)parsedProblem["tests"][i]["answer_url"]
					);

					if (t.Answer is null)
					{
						return null;
					}
					else
					{
						Application.Get().FileProvider.SaveFile(t.Answer);
					}
				}

				tests[i] = t;
			}

			ProblemFile checker = downloadFile((string)parsedProblem["checker_source_url"]);
			if (checker is null)
			{
				return null;
			}

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

		public bool SendRequest(RequestMessage message, bool retryOnFailed = true)
		{
			TimeOutHelper time = new TimeOutHelper();
			do
			{
				try
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

					if (!retryOnFailed || responseMessage.StatusCode == HttpStatusCode.NoContent)
					{
						return responseMessage.StatusCode == HttpStatusCode.NoContent;
					}
				}
				catch (HttpRequestException ex)
				{
					logger.Error(ex, "SendRequest failed with exception.");
				}

				Thread.Sleep(time.GetTimeOut() * 1000);

			} while (retryOnFailed);

			return false;
		}

		public Guid SignUp(WorkerInformation worker)
		{
			string endpoint = buildEndpoint("workers");

			worker.ApiType = GetApiType();
			worker.ApiVersion = GetVersion();
			worker.WebhookSupported = GetWebhookSupported();

			var responseMessage = client.PostAsync(
				endpoint, new { worker }.AsJson(skipNull: true)).Result;

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
		public UpdateWorkerStatus SignIn(Guid id) => UpdateWorker(id, new WorkerInformation(WorkerStatus.Ok));
		public bool SignOut(Guid id) =>
			UpdateWorker(id, new WorkerInformation(WorkerStatus.Disabled)) == UpdateWorkerStatus.Ok;

		public UpdateWorkerStatus UpdateWorker(Guid id, WorkerInformation worker)
		{
			string endpoint = buildEndpoint("workers", id.ToString());

			var responseMessage = client.PutAsync(
				endpoint, new { worker }.AsJson(skipNull: true)).Result;

			logger.Debug("Worker update request was send {0}",
			   responseMessage.StatusCode == HttpStatusCode.NoContent ?
			   "successfully" : "failed");

			if (responseMessage.StatusCode != HttpStatusCode.NoContent)
			{
				if (responseMessage.StatusCode == HttpStatusCode.NotFound)
				{
					logger.Error("Worker update failed. Status code: NotFound. Worker id {0} is incorrect. Error message: {1}",
						id, responseMessage.Content?.ReadAsStringAsync()?.Result);

					return UpdateWorkerStatus.LoginIncorrect;
				}
				else
				{
					logger.Error("Worker update failed. Status code: {0}. Error message: {1}",
						responseMessage.StatusCode, responseMessage.Content?.ReadAsStringAsync()?.Result);

					return UpdateWorkerStatus.Failed;
				}
			}
			else
			{
				return UpdateWorkerStatus.Ok;
			}
		}

		public uint GetVersion() => 2;
		public ApiType GetApiType() => ApiType.HTTP;
		public bool GetWebhookSupported() => false;

		private string buildEndpoint(string method, ulong id, string action = null, string parameters = null)
			=> buildEndpoint(method, id.ToString(), action, parameters);
		private string buildEndpoint(string method, string id = null, string action = null, string parameters = null)
		{
			string endpoint = $"{Application.Get().Configuration.BaseApiAddress}/{method}";

			if (!(id is null))
			{
				endpoint += $"/{id}";
			}

			if (!(action is null))
			{
				endpoint += $"/{action}";
			}

			endpoint += $"?access_token={Application.Get().Configuration.ApiAuthToken}";

			if (!(parameters is null))
			{
				endpoint += $"&{parameters}";
			}

			return endpoint;
		}
	}
}
