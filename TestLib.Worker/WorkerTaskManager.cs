using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TestLib.Worker.ClientApi;

namespace TestLib.Worker
{
	internal class WorkerTaskManager
	{
		private Slot[] slots;
		private Task[] workerTasks;
		private CancellationTokenSource sendingCancellationTokenSource;
		private CancellationTokenSource slotsCancellationTokenSource;
		private bool started;

		public WorkerTaskManager(Configuration configuration)
		{
			slots = new Slot[configuration.WorkerSlotCount];
			workerTasks = new Task[configuration.WorkerSlotCount + 1];

			started = false;
		}

		public void Status()
		{
			Console.WriteLine("==========STATUS==========");

			Console.WriteLine("SendingTestingResult: {0}", workerTasks[0]?.Status.ToString());
			for (uint i = 1; i <= Application.Get().Configuration.WorkerSlotCount; i++)
				Console.WriteLine("Slot {0}: {1}", i, workerTasks[i]?.Status.ToString());
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
		public void Restart()
		{
			Stop();
			Start();
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
		#endregion
	}
}