using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using TestLib.UpdateServer.App_Start;

namespace TestLib.UpdateServer
{
	public class WebApiApplication : HttpApplication
	{
		protected void Application_Start()
		{
			GlobalConfiguration.Configure(WebApiConfig.Register);

			Helpers.StartApplication(Server);
		}
	}
}
