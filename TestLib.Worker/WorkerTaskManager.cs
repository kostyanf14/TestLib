using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TestLib.Worker.ClientApi;
using TestLib.Worker.ClientApi.Models;

namespace TestLib.Worker
{
	internal class WorkerTaskManager
	{
		private Logger logger;
		private Slot[] slots;
		private Task[] workerTasks;
		private Task aliveStatusSenderTask;
		private CancellationTokenSource sendingCancellationTokenSource;
		private CancellationTokenSource slotsCancellationTokenSource;
		private CancellationTokenSource aliveStatusSenderCancellationTokenSource;
		private bool started;

		public WorkerTaskManager(Configuration configuration)
		{
			logger = LogManager.GetCurrentClassLogger();

			slots = new Slot[configuration.WorkerSlotCount];
			workerTasks = new Task[configuration.WorkerSlotCount + 1];

			aliveStatusSenderCancellationTokenSource = new CancellationTokenSource();
			aliveStatusSenderTask = Task.Run(
				() => sendAliveStatus(), aliveStatusSenderCancellationTokenSource.Token);

			started = false;
		}

		public void PrintStatus()
		{
			Console.WriteLine("==========STATUS==========");

			foreach (var item in GetStatus())
				Console.WriteLine(item);
		}

		private string[] GetStatus()
		{
			string[] res = new string[Application.Get().Configuration.WorkerSlotCount + 1];

			res[0] = $"SendingTestingResult: {workerTasks[0]?.Status.ToString()}";
			for (uint i = 1; i <= Application.Get().Configuration.WorkerSlotCount; i++)
				res[i] = $"Slot {i}: {workerTasks[i]?.Status.ToString()}";

			return res;
		}

		public void Start()
		{
			if (started)
				return;

			StartSendingTestingResult();
			StartSlots();

			started = true;
		}
		public void Stop()
		{
			if (!started)
				return;

			slotsCancellationTokenSource.Cancel();
			sendingCancellationTokenSource.Cancel();

			StopSlots();
			StopSendingTestingResult(true);

			started = false;
		}
		public void End()
		{
			Stop();
			aliveStatusSenderCancellationTokenSource.Cancel();

			try
			{ Task.WaitAll(aliveStatusSenderTask); }
			catch { }
		}

		#region Privates
		private void StartSendingTestingResult()
		{
			sendingCancellationTokenSource = new CancellationTokenSource();

			workerTasks[0] =
					Task.Run(() => SendTestingResult(), sendingCancellationTokenSource.Token);
		}

		private void StartSlots()
		{
			slotsCancellationTokenSource = new CancellationTokenSource();

			for (uint i = 1; i <= Application.Get().Configuration.WorkerSlotCount; i++)
				StartSlot(i);
		}

		private void StopSendingTestingResult(bool flushQuery)
		{
			sendingCancellationTokenSource.Cancel();

			try
			{ Task.WaitAll(workerTasks[0]); }
			catch { }
		}

		private void StopSlots()
		{
			slotsCancellationTokenSource.Cancel();

			try
			{ Task.WaitAll(new ArraySegment<Task>(workerTasks, 1, workerTasks.Length - 1).Array); }
			catch { }
		}

		private void SendTestingResult()
		{
			Application app = Application.Get();
			IApiClient client = new HttpCodelabsApiClient();
			var logger = LogManager.GetCurrentClassLogger();

			sendingCancellationTokenSource.Token.Register(() => app.RequestMessages.Enqueue(null));

			while (true)
			{
				var request = app.RequestMessages.Dequeue();
				if (request is null)
					break;

				try
				{
					client.SendRequest(request);
				}
				catch (Exception ex)
				{
					logger.Error("Error sending request {0} to server: {1}. Some data will be lose", request.RequestUri, ex);
				}
			}

			sendingCancellationTokenSource.Token.ThrowIfCancellationRequested();
		}

		private void StartSlot(uint id)
		{
			slots[id - 1] = slots[id - 1] ?? new Slot(id, slotsCancellationTokenSource.Token);
			workerTasks[id] =
				Task.Run(() => slots[id - 1].Do(), slotsCancellationTokenSource.Token);
		}

		private void sendAliveStatus()
		{
			Application app = Application.Get();
			IApiClient client = new HttpCodelabsApiClient();

			while (!aliveStatusSenderCancellationTokenSource.IsCancellationRequested)
			{
				try
				{
					int disabled = 0;
					int failed = 0;

					foreach (var item in workerTasks)
					{
						switch (item?.Status)
						{
							case TaskStatus.Running:
								break;
							case TaskStatus.WaitingToRun:
							case TaskStatus.Canceled:
							case TaskStatus.Created:
								disabled++;
								break;
							case TaskStatus.Faulted:
							default:
								failed++;
								break;
						}
					}

					WorkerStatus status;
					if (failed > 0)
						status = WorkerStatus.Failed;
					else if (disabled > 0)
						status = WorkerStatus.Disabled;
					else
						status = WorkerStatus.Ok;

					client.Alive(app.Configuration.WorkerId, new AliveInformation(status, GetStatus()));
				}
				catch (Exception ex)
				{
					logger.Error("Sending alive information failed with error {0}. Exeption: {1}",
						ex.GetType().Name, ex.Message);
				}
				finally
				{
					Thread.Sleep(60 * 1000);
				}
			}

			aliveStatusSenderCancellationTokenSource.Token.ThrowIfCancellationRequested();
		}
		#endregion
	}
}