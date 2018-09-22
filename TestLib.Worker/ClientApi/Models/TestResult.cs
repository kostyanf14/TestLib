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
		public string Log;
		[JsonProperty(PropertyName = "memory")]
		public double UsedMemmory;
		[JsonProperty(PropertyName = "time")]
		public double WorkTime;
	}
}
