using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TestLib.UpdateServer.Models
{
	public enum UpdateType
	{
		None,
		Tester,
		Worker,
		Full,
	}

	public enum UpdatePlatform
	{
		None,
		x86,
		x64,
	}

	public class Update
	{
		public UpdateType Type;

		public static (bool valid, UpdateType type) CheckUpdateFileNames(string[] files)
		{
			string[] fullUpdate =
			{
				"TestLib.WorkerService.exe",
				"TestLib.WorkerService.exe.config",
				"TestLib.Tester.dll",
				"NLog.dll",
				"NLog.config",
				"Newtonsoft.Json.dll",
			};

			if (files.Length != fullUpdate.Length)
				return (false, UpdateType.None);

			Array.Sort(fullUpdate);
			Array.Sort(files);

			if (files.SequenceEqual(fullUpdate))
				return (true, UpdateType.Full);

			return (false, UpdateType.None);
		}
	}
}