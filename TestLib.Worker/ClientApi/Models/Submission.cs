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
		private string sourceUrlType;

		public Submission() { }

		public Submission(ulong id, string sourceUrl, string sourceUrlType, byte compilerId, byte checkerCompilerId, ulong problemId, DateTime problemUpdatedAt, uint memoryLimit, uint timeLimit)
		{
			Id = id;
			SourceUrl = sourceUrl ?? throw new ArgumentNullException(nameof(sourceUrl));
			SourceUrlType = sourceUrlType;
			CompilerId = compilerId;
			CheckerCompilerId = checkerCompilerId;
			ProblemId = problemId;
			ProblemUpdatedAt = problemUpdatedAt;
			MemoryLimit = memoryLimit;
			TimeLimit = timeLimit;
		}

		public ulong Id { get; set; }
		public string SourceUrl { get; set; }
		public string SourceUrlType
		{
			get => sourceUrlType;
			set => sourceUrlType = value is null ? "ralative" : value;
			//NullOrWhitespace
		}
		public byte CompilerId { get; set; }

		public byte CheckerCompilerId { get; set; }
		public ulong ProblemId { get; set; }
		public DateTime ProblemUpdatedAt { get; set; }
		public UInt32 MemoryLimit { get; set; }
		public UInt32 TimeLimit { get; set; }

		public override string ToString() => JsonConvert.SerializeObject(this);
	}
}
