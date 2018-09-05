using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;
using TestLib.Worker.ClientApi.Models;

namespace TestLib.Worker.ClientApi
{
	class HttpCodelabsApiClient : IApiClient
	{
		static Logger logger = LogManager.GetCurrentClassLogger();

		HttpClient client;
		public HttpCodelabsApiClient()
		{
			client = new HttpClient();

			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
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
					submissions.Add(
						new Submission(
							id: (ulong)parsedSubmissions[i]["id"],
							sourceUrl: (string)parsedSubmissions[i]["source_url"],
							compilerId: compilerId,
							sourceUrlType: (string)parsedSubmissions[i]["source_url_type"],
							checkerCompilerId: checkerCompilerId,
							problemId: (ulong)parsedSubmissions[i]["problem"]["id"],
							problemUpdatedAt: (DateTime)parsedSubmissions[i]["problem"]["updated_at"],
							memoryLimit: (UInt32)parsedSubmissions[i]["memory_limit"],
							timeLimit: (UInt32)parsedSubmissions[i]["time_limit"]
						)
					);
			}

			logger.Debug("Selected {0} submissions", submissions.Count);
			return submissions;
		}
		public IEnumerable<Submission> GetSubmissions()
			=> readSubmissions();
		public IEnumerable<Submission> GetSuitableSubmissions(HashSet<byte> compilers)
			=> readSubmissions(compilers);

		public bool TakeSubmissions(ulong id)
		{
			string endpoint = buildEndpoint("submissions", id, "take");
			var responseMessage = client.PostAsync(endpoint, null).Result;

			logger.Debug("Submission {0} taken {1}", id,
				responseMessage.StatusCode == HttpStatusCode.NoContent ?
				"successfully" : "failed");
			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}
		public bool ReleaseSubmissions(ulong id)
		{
			string endpoint = buildEndpoint("submissions", id, "release");
			var responseMessage = client.PostAsync(endpoint, null).Result;

			logger.Debug("Submission {0} released {1}", id,
				responseMessage.StatusCode == HttpStatusCode.NoContent ?
				"successfully" : "failed");
			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}
		public bool FailSubmissions(ulong id)
		{
			string endpoint = buildEndpoint("submissions", id, "fail");
			var responseMessage = client.PostAsync(endpoint, null).Result;

			logger.Debug("Submission {0} failed {1}", id,
				responseMessage.StatusCode == HttpStatusCode.NoContent ?
				"successfully" : "failed");
			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}

		private ProblemFile downloadFile(string url, string urlType = null)
		{
			Uri uri = null;
			if (urlType is null || urlType is "ralative")
				uri = new Uri(Application.Get().Configuration.BaseApiAddress, url);
			else
				uri = new Uri(url);


			ProblemFile solution = new ProblemFile();
			solution.Content = client.GetAsync(uri).Result.Content.ReadAsByteArrayAsync().Result;
			return solution;
		}
		public ProblemFile DownloadSolution(Submission submission) =>
			downloadFile(submission.SourceUrl, submission.SourceUrlType);

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
					(string)parsedProblem["tests"][i]["input_url"],
					(string)parsedProblem["tests"][i]["input_url_type"]
				);

				if (!(parsedProblem["tests"][i]["answer_url"] is null))
					t.Answer = downloadFile(
					(string)parsedProblem["tests"][i]["answer_url"],
					(string)parsedProblem["tests"][i]["answer_url_type"]
				);

				tests[i] = t;
			}

			string checkerUrlType = (string)parsedProblem["checker_source_url_type"];
			string checkerUrl = (string)parsedProblem["checker_source_url"];
			ProblemFile checker = downloadFile(checkerUrl, checkerUrlType);

			return new Problem(
				id: (ulong)parsedProblem["id"],
				tests: tests,
				checker: checker,
				checkerCompilerId: (byte)parsedProblem["checker_compiler_id"],
				lastUpdate: (DateTime)parsedProblem["updated_at"]
			);
		}

		public bool SendTestingResult(TestResult result)
		{
			string endpoint = buildEndpoint("results");
			var responseMessage = client.PostAsync(endpoint, result.AsJson()).Result;

			logger.Debug("Testing result for submission {0} test {1} send {2}", result.SubmissionId,
				result.TestId, responseMessage.StatusCode == HttpStatusCode.NoContent ?
				"successfully" : "failed");

			if (responseMessage.StatusCode != HttpStatusCode.NoContent)
				logger.Error("Sending testing result failed. Server error message: {0}",
					responseMessage.RequestMessage.Content?.ReadAsStringAsync()?.Result);

			return responseMessage.StatusCode == HttpStatusCode.NoContent;
		}


        private string buildEndpoint(string method, ulong? id = null, string action = null, string parameters = null)
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
