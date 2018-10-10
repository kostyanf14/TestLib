using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TestLib.Worker.ClientApi.Models
{
	internal class Submission
	{
		public Submission() { }

		public Submission(ulong id, string sourceUrl, byte compilerId, byte checkerCompilerId, ulong problemId, DateTime problemUpdatedAt, uint memoryLimit, uint timeLimit)
		{
			Id = id;
			SourceUrl = sourceUrl ?? throw new ArgumentNullException(nameof(sourceUrl));
			CompilerId = compilerId;
			CheckerCompilerId = checkerCompilerId;
			ProblemId = problemId;
			ProblemUpdatedAt = problemUpdatedAt;
			MemoryLimit = memoryLimit;
			TimeLimit = timeLimit;
            RealTimeLimit = 10 * TimeLimit;
        }

        public Submission(ulong id, string sourceUrl, byte compilerId, byte checkerCompilerId, ulong problemId, DateTime problemUpdatedAt, uint memoryLimit, uint timeLimit, uint realTimeLimit)
        {
            Id = id;
            SourceUrl = sourceUrl ?? throw new ArgumentNullException(nameof(sourceUrl));
            CompilerId = compilerId;
            CheckerCompilerId = checkerCompilerId;
            ProblemId = problemId;
            ProblemUpdatedAt = problemUpdatedAt;
            MemoryLimit = memoryLimit;
            TimeLimit = timeLimit;
            RealTimeLimit = realTimeLimit;
        }

        public ulong Id { get; set; }
		public string SourceUrl { get; set; }
		
		public byte CompilerId { get; set; }

		public byte CheckerCompilerId { get; set; }
		public ulong ProblemId { get; set; }
		public DateTime ProblemUpdatedAt { get; set; }
		public uint MemoryLimit { get; set; }
		public uint TimeLimit { get; set; }
		public uint RealTimeLimit { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
	}
}
