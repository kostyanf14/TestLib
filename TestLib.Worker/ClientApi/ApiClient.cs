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
                            sourceUrlType: (string)parsedSubmissions[i]["source_url_type"],
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

        private ProblemFile downloadFile(string url, string urlType = null)
        {
            Uri uri = null;
            if (urlType is null || urlType is "ralative")
            {
                uri = new Uri(Application.Get().Configuration.BaseApiAddress, url);
            }
            else
            {
                uri = new Uri(url);
            }

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
                {
                    t.Answer = downloadFile(
                    (string)parsedProblem["tests"][i]["answer_url"],
                    (string)parsedProblem["tests"][i]["answer_url_type"]
                );
                }

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

        private string buildEndpoint(string method, ulong? id = null, string action = null, string parameters = null)
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

        public RequestMessage GetTakeSubmissionsRequestMessage(ulong id) =>
            new RequestMessage(buildEndpoint("submissions", id, "take"), null);

        public RequestMessage GetFailSubmissionsRequestMessage(ulong id) =>
            new RequestMessage(buildEndpoint("submissions", id, "fail"), null);

        public RequestMessage GetReleaseSubmissionsRequestMessage(ulong id) =>
             new RequestMessage(buildEndpoint("submissions", id, "release"), null);

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
                logger.Error("Request {0} server error message: {1}", message.RequestUri,
                    responseMessage.Content?.ReadAsStringAsync()?.Result);
            }

            return responseMessage.StatusCode == HttpStatusCode.NoContent;
        }
    }
}
