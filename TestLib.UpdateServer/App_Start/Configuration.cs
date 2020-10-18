using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace TestLib.UpdateServer.App_Start
{
    public class Configuration
    {
        private static Configuration manager;
        public static Configuration Get() => manager ?? (manager = new Configuration());

        public string AuthSecret { get; private set; }
        public string TempDirectory { get; private set; }
        public string ReleasesDirectory { get; private set; }
        public string ReleaseLatestVersionFileName { get; private set; }

        public void Init(HttpServerUtility server)
        {
            NameValueCollection config = ConfigurationManager.AppSettings;

            AuthSecret = config.Get("auth_secret") ?? "947ac1e4-c0bc-40aa-adc8-81c9ae440096";
            TempDirectory = config.Get("temp_dir") ?? server.MapPath("~/App_Data/Temp"); ;
            ReleasesDirectory = config.Get("releases_dir") ?? server.MapPath("~/App_Data/Releases");
            ReleaseLatestVersionFileName = config.Get("release_latest_version_file_name") ?? "latest.ver";

            if (!Directory.Exists(TempDirectory))
                Directory.CreateDirectory(TempDirectory);
            if (!Directory.Exists(ReleasesDirectory))
                Directory.CreateDirectory(ReleasesDirectory);

        }
    }
}