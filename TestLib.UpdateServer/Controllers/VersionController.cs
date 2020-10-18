using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TestLib.UpdateServer.App_Start;
using TestLib.UpdateServer.Models;

namespace TestLib.UpdateServer.Controllers
{
	public class VersionController : ApiController
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public HttpResponseMessage Get(HttpRequestMessage request, string platform)
		{
			var config = App_Start.Configuration.Get();

			UpdatePlatform platformInfo;
			if (!Enum.TryParse(platform, true, out platformInfo) || platformInfo == UpdatePlatform.None)
			{
				logger.Warn("Can't parse platform {0}", platform);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			string latestVersionFilePath = Path.Combine(config.ReleasesDirectory, platformInfo.ToString(),
				config.ReleaseLatestVersionFileName);

			if (!File.Exists(latestVersionFilePath))
			{
				logger.Warn("Latest version for platform {0} not found", platform);

				return request.CreateResponse(HttpStatusCode.NotFound);
			}

			Version parsedVersion = Version.Parse(File.ReadAllText(latestVersionFilePath));

			return request.CreateResponse(HttpStatusCode.OK, parsedVersion.ToString());
		}
	}
}
