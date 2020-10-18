using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web;

namespace TestLib.UpdateServer.App_Start
{
    public class Configuration
    {
        private static Configuration manager;
        public static Configuration Get() => manager ?? (manager = new Configuration());

        public string AuthSecret { get; private set; }

        public void Init()
        {
            NameValueCollection config = ConfigurationManager.AppSettings;

            AuthSecret = config.Get("auth_secret") ?? "947ac1e4-c0bc-40aa-adc8-81c9ae440096";
        }
    }
}