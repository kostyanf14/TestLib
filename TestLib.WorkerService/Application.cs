using NLog;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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

	internal enum CheckUpdateStatus
	{
		None,
		Ok,
		Restart,
		Failed,
	}

	internal sealed class Application
	{
		private static Application application;
		public bool Initialized { get; private set; }
		public static Application Get() => application ?? (application = new Application());

		private Application()
		{
			logger = LogManager.GetCurrentClassLogger();
			logger.Info("Initiated the creation of application. Version: {0}", GetVersion());

			LoggerManaged = new LoggerManaged();
			apiClient = new HttpCodelabsApiClient();

			NameValueCollection config = ConfigurationManager.AppSettings;
			Configuration = new Configuration(config);

			FileProvider = new FileProvider(Configuration.FileCacheFolder);
			Compilers = new CompilerManager(Configuration.CompilersConfigFolder);

			Problems = new ProblemCache(Configuration.ProblemsCacheSize);
			RequestMessages = new BlockingQueue<RequestMessage>(Configuration.ResultSendingCacheSize);

			workerTasks = new WorkerTaskManager();
		}

		public bool Init() => Initialized = init();
		private bool init()
		{
			LoggerManaged.InitNativeLogger(new LoggerManaged.LogEventHandler(LogManager.GetLogger("Native").Log));

			logger.Debug("Current directory is {0}", Directory.GetCurrentDirectory());
			logger.Debug("AppData folder is {0}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
			logger.Debug("Current user is {0}", System.Security.Principal.WindowsIdentity.GetCurrent().Name);


			FileProvider.Init();

			if (!Compilers.Init())
			{
				logger.Error("Compilers initialization failed");

				return false;
			}

			if (Configuration.WorkerId == Guid.Empty)
			{
				if (!signUp())
				{
					logger.Error("Application sign up failed");
					return false;
				}
			}

			var status = apiClient.SignIn(Configuration.WorkerId);
			if (status == UpdateWorkerStatus.LoginIncorrect)
			{
				logger.Error("Application sign in failed. Worker id is incorrect. Trying re-sign up");

				if (!signUp())
				{
					logger.Error("Application sign up failed");
					return false;
				}
			}
			else if (status != UpdateWorkerStatus.Ok)
			{
				logger.Error("Application sign in failed");
				return false;
			}

			if (!workerTasks.Init(Configuration))
			{
				apiClient.SignOut(Configuration.WorkerId);
				return false;
			}

			return true;
		}

		public Version GetVersion()
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

		public CheckUpdateStatus Update()
		{
			using (WebClient client = new WebClient())
			{
				try
				{
					string stringVersion = client.DownloadString(Configuration.Update.LatestVersionUrl).Replace("\"", "");
					if (!Version.TryParse(stringVersion, out var version))
					{
						logger.Warn("Can't parse latest version {0} from update server {1}. Update not available.",
							stringVersion, Configuration.Update.LatestVersionUrl);

						return CheckUpdateStatus.Failed;
					}

					Version current = GetVersion();
					if (version > current)
					{
						logger.Info(string.Format("Latest version {0}, current version {1}: update is necessary.", version, current));

						var args = new string[]
						{
							Process.GetCurrentProcess().Id.ToString(),
							Directory.GetCurrentDirectory(),
							AppDomain.CurrentDomain.FriendlyName,
						};
						Process.Start("TestLib.Worker.Updater.exe", string.Join(" ", args));

						return CheckUpdateStatus.Restart;
					}
					else
					{
						logger.Info(string.Format("Latest version {0}, current version {1}: update is not necessary.", version, current));

						return CheckUpdateStatus.Ok;
					}
				}
				catch (Exception ex)
				{
					logger.Error(ex, "Error while update.");

					return CheckUpdateStatus.Failed;
				}
			}
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
