using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using NLog;

namespace TestLib.Worker
{
    internal class CompilerManager
    {
		static Logger logger = LogManager.GetCurrentClassLogger();

		private HashSet<byte> compilersIds = null;
		private Dictionary<byte, Compiler> compilers = new Dictionary<byte, Compiler>();
		private string directory;

		public CompilerManager(string _dir)
        {
			logger.Debug("ctor");

			directory = _dir;
		}
		public bool Init()
		{
			logger.Info("Initialization started. Compiler directory {0}.", directory);
			if (!Directory.Exists(directory))
			{
				logger.Error("Initialization failed. Directory does not exist.", directory);

				return false;
			}

			var files = Directory.GetFiles(directory);
			foreach (var file in files)
			{
				var info = new FileInfo(file);
				if (info.Extension != ".cfg")
				{
					logger.Warn("Incorect compiler configuration file extension. File {0} was skipped", info.Name);
					continue;
				}

				Compiler compiler;
				XmlSerializer serializer = new XmlSerializer(typeof(Compiler));
				using (FileStream fs = info.OpenRead())
				using (XmlReader reader = XmlReader.Create(fs))
					compiler = (Compiler)serializer.Deserialize(reader);

				compiler.Commands.Sort((x, y) => x.Order - y.Order);
				compilers.Add(compiler.Id, compiler);
			}

			return true;
		}

		public bool HasCompiler(byte id) => 
			compilers.ContainsKey(id);
        public bool CheckCompiler(byte id, string name) => 
			HasCompiler(id) && compilers[id].Name == name;
        public Compiler GetCompiler(byte id) => compilers[id];
		public HashSet<byte> GetCompilers() => 
			compilersIds ?? (compilersIds = new HashSet<byte>(compilers.Keys));
	}
}