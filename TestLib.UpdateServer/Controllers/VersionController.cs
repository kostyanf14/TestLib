using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using TestLib.UpdateServer.App_Start;

namespace TestLib.UpdateServer.Controllers
{
	public class VersionController : ApiController
	{
		public string Get()
		{
			Version parsedVersion =
				Version.Parse(File.ReadAllText(Helpers.ReleasesLatestVersionFilePath));

			return parsedVersion.ToString();
		}
	}
}
