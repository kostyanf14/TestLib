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
		public static int ToInt32OrDefault(this string value, int defaultValue = 0) =>
             int.TryParse(value, out int result) ? result : defaultValue;

		public static uint ToUInt32OrDefault(this string value, uint defaultValue = 0) =>
             uint.TryParse(value, out uint result) ? result : defaultValue;
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
			Requests = new BlockingQueue<RequestMessage>(config.Get("result_sending_cache_size").ToUInt32OrDefault(2048));

			Configuration = new Configuration(config);
		}
		public FileProvider FileProvider { get; private set; }
		public ProblemCache Problems { get; private set; }
		public BlockingQueue<RequestMessage> Requests { get; private set; }
		public CompilerManager Compilers { get; private set; }

		public Configuration Configuration { get; private set; }
	}
}
