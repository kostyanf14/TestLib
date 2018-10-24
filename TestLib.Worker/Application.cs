using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TestLib.Worker.ClientApi;
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

		private Application()
		{
			logger = LogManager.GetCurrentClassLogger();
			logger.Info("Application initialization started. Version: {0}", Version());

			LoggerManaged = new LoggerManaged();
			apiClient = new HttpCodelabsApiClient();

			NameValueCollection config = ConfigurationManager.AppSettings;
			Configuration = new Configuration(config);

			FileProvider = new FileProvider(Configuration.FileCacheFolder);
			Compilers = new CompilerManager(Configuration.CompilersConfigFolder);

			Problems = new ProblemCache(Configuration.ProblemsCacheSize);
			RequestMessages = new BlockingQueue<RequestMessage>(Configuration.ResultSendingCacheSize);

			workerTasks = new WorkerTaskManager(Configuration);
		}

		public bool Init()
		{
			LoggerManaged.InitNativeLogger(new LoggerManaged.LogEventHandler(LogManager.GetLogger("Native").Log));

			FileProvider.Init();

			if (!Compilers.Init())
				return false;

			if (Configuration.WorkerId == Guid.Empty)
				if (!signUp())
				{
					logger.Error("Application sign up failed");
					return false;
				}

			if (!apiClient.SignIn(Configuration.WorkerId))
			{
				logger.Error("Application sign in failed");
				return false;
			}

			return true;
		}

		public Version Version()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

			return new Version(fvi.ProductMajorPart, fvi.ProductMinorPart,
				fvi.ProductBuildPart, fvi.ProductPrivatePart);
		}

		public void Start()
		{
			workerTasks.Start();
		}
		public void Stop()
		{
			workerTasks.Stop();
		}
		public void Restart()
		{
			Stop();
			Start();
		}
		public void Status() => workerTasks.PrintStatus();
		public void End()
		{
			Stop();
			workerTasks.End();
			LoggerManaged.Destroy();
			apiClient.SignOut(Configuration.WorkerId);
		}

		private bool signUp()
		{
			WorkerInformation wi = new WorkerInformation(Configuration.WorkerName,
				Dns.GetHostAddresses(Dns.GetHostName()).Select(ip => ip.ToString()).ToArray());

			return (Configuration.WorkerId = apiClient.SignUp(wi)) != Guid.Empty;
		}

		#region Variables
		public FileProvider FileProvider { get; private set; }
		public ProblemCache Problems { get; private set; }
		public BlockingQueue<RequestMessage> RequestMessages { get; private set; }
		public CompilerManager Compilers { get; private set; }

		public Configuration Configuration { get; private set; }
		public LoggerManaged LoggerManaged { get; private set; }

		private WorkerTaskManager workerTasks;
		private IApiClient apiClient;
		private Logger logger;
		#endregion
	}
}
