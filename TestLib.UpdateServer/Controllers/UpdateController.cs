using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using NLog;
using TestLib.UpdateServer.App_Start;
using TestLib.UpdateServer.Models;

namespace TestLib.UpdateServer.Controllers
{
	public class UpdateController : ApiController
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private static int id;

		static UpdateController()
		{
			id = 0;
		}

		// GET: api/Update/
		public async Task<HttpResponseMessage> Get(HttpRequestMessage request, string version, string platform)
		{
			var config = App_Start.Configuration.Get();

			UpdatePlatform platformInfo;
			if (!Enum.TryParse(platform, true, out platformInfo) || platformInfo == UpdatePlatform.None)
			{
				logger.Warn("Can't parse platform {0}", platform);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			string latestVersionFilePath = Path.Combine(
					config.ReleasesDirectory, platformInfo.ToString(), config.ReleaseLatestVersionFileName);

			Version parsedVersion;
			if (version == "latest")
				parsedVersion = Version.Parse(File.ReadAllText(latestVersionFilePath));
			else
			if (!Version.TryParse(version, out parsedVersion))
			{
				logger.Warn("Can't parse version {0}", version);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			var stream = new MemoryStream();
			using (var file = File.OpenRead(Path.Combine(
					config.ReleasesDirectory, platformInfo.ToString(), $"{UpdateType.Full}.{parsedVersion}.zip")))
				file.CopyTo(stream);

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Content = new ByteArrayContent(stream.ToArray());
			response.Content.Headers.ContentDisposition =
				new ContentDispositionHeaderValue("attachment")
				{
					FileName = parsedVersion.ToString() + ".zip"
				};
			response.Content.Headers.ContentType =
				new MediaTypeHeaderValue("application/octet-stream");

			return response;
		}

		// POST: api/Update
		public async Task<HttpResponseMessage> Post(HttpRequestMessage request, string platform, string token)
		{
			var config = App_Start.Configuration.Get();

			int currentRequestId = Interlocked.Increment(ref id);
			logger.Debug("Request {0}: New file was uploded", currentRequestId);

			if (!Helpers.CheckRequestToken(token))
            {
				logger.Warn("Bad request token {0}", token);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			if (HttpContext.Current.Request.Files.Count != 1)
			{
				logger.Warn("No file in request {0}", platform);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			var postedFile = HttpContext.Current.Request.Files[0];

			UpdatePlatform platformInfo;
			if (!Enum.TryParse(platform, true, out platformInfo) || platformInfo == UpdatePlatform.None)
			{
				logger.Warn("Can't parse platform {0}", platform);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			string platformReleasesPath = Path.Combine(config.ReleasesDirectory, platformInfo.ToString());
			if (!Directory.Exists(platformReleasesPath))
				Directory.CreateDirectory(platformReleasesPath);

			string tempDir = Path.Combine(config.TempDirectory, DateTime.Now.ToBinary().ToString());
			string tempFile = $"{tempDir}.tmp";

			using (Stream fileStream = File.Create(tempFile))
				await postedFile.InputStream.CopyToAsync(fileStream);

			logger.Debug("Request {0}: File was copied to {1}", currentRequestId, tempFile);

			try
			{
				ZipFile.ExtractToDirectory(tempFile, tempDir);
			}
			catch (InvalidDataException ex)
			{
				logger.Warn(ex, "Request {0}: Can't extract uploaded file. May be uploaded file is not archive.", currentRequestId);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			var updateFiles = Directory.GetFiles(tempDir);
			var updateFileNames = updateFiles.Select(f => Path.GetFileName(f)).ToArray();

			var updateInfo = Update.CheckUpdateFileNames(updateFileNames);
			if (!updateInfo.valid)
			{
				logger.Warn("Request {0}: Uploaded arcive contains unknown files.", currentRequestId);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			if (updateInfo.type != UpdateType.Full)
			{
				logger.Warn("Request {0}: {1} is unsupported", currentRequestId, updateInfo.type);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}


			var exeFile = updateFiles.First(x => x.Contains(".exe"));
			var fvi = FileVersionInfo.GetVersionInfo(exeFile);
			var version = $"{fvi.ProductMajorPart}.{fvi.ProductMinorPart}.{fvi.ProductBuildPart}.{fvi.ProductPrivatePart}";

			string latestVersionFilePath = Path.Combine(platformReleasesPath, config.ReleaseLatestVersionFileName);
			try
			{
				Version latestVersion;
				if (File.Exists(latestVersionFilePath))
					latestVersion = Version.Parse(File.ReadAllText(latestVersionFilePath));
				else
					latestVersion = Version.Parse("0.0.0.0");

				var currentVersion = Version.Parse(version);
				if (currentVersion > latestVersion)
				{
					File.WriteAllText(latestVersionFilePath, version);
				}
			}
			catch (Exception ex)
			{
				logger.Warn(ex, "Request {0}: Read/Write latest version failed", currentRequestId);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			if (version == "0.0.0.0")
			{
				logger.Warn("Request {0}: File {1} version is incorrect", currentRequestId, exeFile);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}

			var releaseFile = Path.Combine(platformReleasesPath, $"{updateInfo.type}.{version}.zip");
			try
			{
				File.Move(tempFile, releaseFile);
			}
			catch (Exception ex)
			{
				logger.Warn(ex, "Request {0}: Move failed", currentRequestId);

				return request.CreateResponse(HttpStatusCode.BadRequest);
			}
			
			return request.CreateResponse(HttpStatusCode.Created);
		}
	}
}
