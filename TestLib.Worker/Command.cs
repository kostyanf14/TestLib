using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib.Worker
{
    public class Command
    {
        public int Order { get; set; }
        public string Program { get; set; }
        public string Arguments { get; set; }
        public string CheckerArguments { get; set; }
	}
}
