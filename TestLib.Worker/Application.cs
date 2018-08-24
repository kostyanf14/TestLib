using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TestLib.Worker.ClientApi.Models;

namespace TestLib.Worker
{
	public static class StringConvertionExtension
	{
		public static Int32 ToInt32OrDefault(this string value, Int32 defaultValue = 0) =>
			 Int32.TryParse(value, out Int32 result) ? result : defaultValue;

		public static UInt32 ToUInt32OrDefault(this string value, UInt32 defaultValue = 0) =>
			 UInt32.TryParse(value, out UInt32 result) ? result : defaultValue;
	}

	internal sealed class Application
	{
		private static Application application;
		public static Application Get() => application ?? (application = new Application());

		Application()
		{
			Logger logger = LogManager.GetCurrentClassLogger();
			logger.Info("Application initialization started");
			NameValueCollection config = ConfigurationManager.AppSettings;

			FileProvider = new FileProvider(config.Get("cache_folder") ?? ".\\cache\\");
			Compilers = new CompilerManager(config.Get("compilers_config_folder") ?? ".\\compilers\\");

			Problems = new ProblemCache(config.Get("problems_cache_size").ToUInt32OrDefault(1));
			TestingResults = new BlockingQueue<TestResult>(config.Get("testing_result_sending_cache_size").ToUInt32OrDefault(2048));

			Configuration = new Configuration(config);
		}
		public FileProvider FileProvider { get; private set; }
		public ProblemCache Problems { get; private set; }
		public BlockingQueue<TestResult> TestingResults { get; private set; }
		public CompilerManager Compilers { get; private set; }

		public Configuration Configuration { get; private set; }
	}
}
