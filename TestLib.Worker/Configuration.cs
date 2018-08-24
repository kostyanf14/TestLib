using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace TestLib.Worker
{
	internal class Configuration
	{
		public Configuration(NameValueCollection config)
		{
			TestingWorkDirectory = config.Get("workarea") ?? ".\\workarea\\";

			BaseAddress = new Uri(config.Get("base_address") ?? "http://localhost:8080/");
			BaseApiAddress = new Uri(BaseAddress, config.Get("api_path") ?? "/api");
			ApiAuthToken = config.Get("api_token");
			CompilersRealTimeLimit = config.Get("compilers_real_time_limit").ToUInt32OrDefault(60 * 1000);

			InputFileName = config.Get("input_file_name") ?? "input.txt";
			OutputFileName = config.Get("output_file_name") ?? "output.txt";
			AnswerFileName = config.Get("answer_file_name") ?? "answer.txt";
			ReportFileName = config.Get("report_file_name") ?? "report.txt";
			CompilerLogFileName = config.Get("compiler_log_file_name") ?? "compiler_log.txt";

			GetSubmissionDelayMs = config.Get("get_submission_delay").ToInt32OrDefault(500);
		}

		public string TestingWorkDirectory { get; private set; }

		public Uri BaseAddress { get; private set; }
		public Uri BaseApiAddress { get; private set; }
		public string ApiAuthToken { get; private set; }
		public uint CompilersRealTimeLimit { get; private set; }
		public string InputFileName { get; private set; }
		public string OutputFileName { get; private set; }
		public string AnswerFileName { get; private set; }
		public string ReportFileName { get; private set; }
		public string CompilerLogFileName { get; private set; }
		public int GetSubmissionDelayMs { get; private set; }
	}
}
