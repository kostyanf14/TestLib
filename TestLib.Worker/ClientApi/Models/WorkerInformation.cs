using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TestLib.Worker.ClientApi.Models
{
	internal enum ApiType : byte
	{
		HTTP = 0,
		WS = 1,
	}
	internal enum WorkerStatus : byte
	{
		Disabled = 0,
		Ok = 1,
		Failed = 2,
		Stale = 3,
		Stopped = 4,
	}

	internal class WorkerInformation
	{
		[JsonProperty(PropertyName = "name")]
		public string Name;
		[JsonProperty(PropertyName = "ips")]
		public string[] IPs;
		[JsonProperty(PropertyName = "api_version")]
		public uint? ApiVersion;
		[JsonProperty(PropertyName = "api_type")]
		public ApiType? ApiType;
		[JsonProperty(PropertyName = "webhook_supported")]
		public bool? WebhookSupported;

		[JsonProperty(PropertyName = "status")]
		public WorkerStatus? Status;
		[JsonProperty(PropertyName = "task_status")]
		public string[] TaskStatuses;

		[JsonProperty(PropertyName = "alive_at")]
		public DateTime AliveAt = DateTime.Now;

		public WorkerInformation(WorkerStatus status)
		{
			Status = status;
			TaskStatuses = new string[] { };
		}

		public WorkerInformation(string name, string[] ps)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			IPs = ps ?? throw new ArgumentNullException(nameof(ps));

			Status = WorkerStatus.Disabled;
		}

		public WorkerInformation(WorkerStatus status, string[] taskStatuses)
		{
			Status = status;
			TaskStatuses = taskStatuses ?? throw new ArgumentNullException(nameof(taskStatuses));
		}

		public WorkerInformation(string name, string[] ips, uint apiVersion, ApiType apiType, bool webhookSupported)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			IPs = ips ?? throw new ArgumentNullException(nameof(ips));
			ApiVersion = apiVersion;
			ApiType = apiType;
			WebhookSupported = webhookSupported;

			Status = WorkerStatus.Disabled;
		}
	}
}
