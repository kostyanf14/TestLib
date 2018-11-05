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

	public class Update
	{
		public UpdateType Type;

		public int VersionMajorPart;
		public int VersionMinorPart;
		public int VersionBuildPart;
		public int VersionPrivatePart;

		public bool Mandatory;

		public static (bool valid, UpdateType type) CheckUpdateFileNames(string[] files)
		{
			string[] fullUpdate =
			{
				"TestLib.Worker.exe",
				"TestLib.Worker.exe.config",
				"TestLib.Tester.dll",
				"NLog.dll",
				"NLog.config",
				"Newtonsoft.Json.dll",
				"LiteDB.dll",
			};

			if (files.Length != fullUpdate.Length)
				return (false, UpdateType.None);

			Array.Sort(fullUpdate);
			Array.Sort(files);

			for (int i = 0; i < fullUpdate.Length; i++)
			{
				if (fullUpdate[i] != files[i])
					return (false, UpdateType.None);
			}

			return (true, UpdateType.Full);
		}
	}
}