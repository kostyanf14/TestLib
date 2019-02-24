using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;

namespace TestLib.Worker
{
	internal class Configuration
	{
		public class UpdateConfiguration
		{
			public string LatestVersionUrl;
			public string LatestProgramUrl;
		}
			

		private Guid _workerId;

		public Configuration(NameValueCollection config)
		{
			Update = new UpdateConfiguration();

			WorkerName = config.Get("worker_name") ?? "TestLib.Worker";

			TestingWorkDirectory = config.Get("workarea") ?? ".\\workarea\\";

			FileCacheFolder = config.Get("cache_folder") ?? ".\\cache\\";
			CompilersConfigFolder = config.Get("compilers_config_folder") ?? ".\\compilers\\";
			ProblemsCacheSize = config.Get("problems_cache_size").ToUInt32OrDefault(1);
			ResultSendingCacheSize = config.Get("result_sending_cache_size").ToUInt32OrDefault(2048);

			BaseAddress = new Uri(config.Get("base_address") ?? "http://localhost:8080/");
			BaseApiAddress = new Uri(BaseAddress, config.Get("api_path") ?? "/api");
			ApiAuthToken = config.Get("api_token");

			InputFileName = config.Get("input_file_name") ?? "input.txt";
			OutputFileName = config.Get("output_file_name") ?? "output.txt";
			AnswerFileName = config.Get("answer_file_name") ?? "answer.txt";
			ReportFileName = config.Get("report_file_name") ?? "report.txt";
			CompilerLogFileName = config.Get("compiler_log_file_name") ?? "compiler_log.txt";

			GetSubmissionDelayMs = config.Get("get_submission_delay").ToInt32OrDefault(500);
			WorkerSlotCount = config.Get("worker_slot_count").ToUInt32OrDefault(1);

			Update.LatestVersionUrl = config.Get("update_latest_version_url") ?? "http://localhost:8081/api/version";
			Update.LatestProgramUrl = config.Get("update_latest_program_url") ?? "http://localhost:8081/api/update?version=latest";

			fid = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				Process.GetCurrentProcess().ProcessName);

			if (!Directory.Exists(fid))
				Directory.CreateDirectory(fid);

			fid = Path.Combine(fid, "fid");

			if (File.Exists(fid))
				if (!Guid.TryParse(File.ReadAllText(fid), out _workerId))
				{
					File.Delete(fid);
					_workerId = Guid.Empty;
				}
		}

		public string WorkerName { get; }
		public string TestingWorkDirectory { get; }
		public string FileCacheFolder { get; }
		public string CompilersConfigFolder { get; }
		public uint ProblemsCacheSize { get; }
		public uint ResultSendingCacheSize { get; }
		public Uri BaseAddress { get; }
		public Uri BaseApiAddress { get; }
		public string ApiAuthToken { get; }
		public string InputFileName { get; }
		public string OutputFileName { get; }
		public string AnswerFileName { get; }
		public string ReportFileName { get; }
		public string CompilerLogFileName { get; }
		public int GetSubmissionDelayMs { get; }
		public uint WorkerSlotCount { get; }
		public UpdateConfiguration Update { get; }
		public Guid WorkerId
		{
			get => _workerId;
			set => File.WriteAllText(fid, (_workerId = value).ToString());
		}

		private string fid;
	}
}
