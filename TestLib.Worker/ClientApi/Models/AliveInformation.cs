using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TestLib.Worker.ClientApi.Models
{
	internal class AliveInformation
	{
		[JsonProperty(PropertyName = "status")]
		public WorkerStatus Status;

		[JsonProperty(PropertyName = "task_status")]
		public string[] TaskStatuses;

		public AliveInformation(WorkerStatus status, params string[] taskStatuses)
		{
			Status = status;
			TaskStatuses = taskStatuses ?? throw new ArgumentNullException(nameof(taskStatuses));
		}
	}
}
