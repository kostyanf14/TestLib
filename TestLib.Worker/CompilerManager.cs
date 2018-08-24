using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TestLib.Worker
{
    internal class CompilerManager
    {
        private string directory;
		private HashSet<byte> compilersIds = null;
		private Dictionary<byte, Compiler> compilers = new Dictionary<byte, Compiler>();

        public CompilerManager(string _dir)
        {
            directory = _dir;

            var files = Directory.GetFiles(_dir);
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (info.Extension != ".cfg")
                {
                    //Logger.Warning (bad file)
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