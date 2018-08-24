using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TestLib.Worker.ClientApi.Models
{
	internal class TestResult
	{
		[JsonProperty(PropertyName = "submission_id")]
		public ulong SubmissionId;
		[JsonProperty(PropertyName = "test_id")]
		public ulong TestId;
		[JsonProperty(PropertyName = "status")]
		public TestingResult Result;
		[JsonProperty(PropertyName = "log")]
		public string CompilerLog;
		[JsonProperty(PropertyName = "memory")]
		public UInt32 UsedMemmory;
		[JsonProperty(PropertyName = "time")]
		public UInt32 WorkTime;
	}
}
