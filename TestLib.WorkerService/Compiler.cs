using System.Collections.Generic;

namespace TestLib.Worker
{
    public class Compiler
    {
        public byte Id { get; set; }
        public string FileExt { get; set; }
        public string Name { get; set; }
		public uint CompilersRealTimeLimit { get; set; }
		public List<Command> Commands { get; set; }
        public Command RunCommand { get; set; }
    }
}