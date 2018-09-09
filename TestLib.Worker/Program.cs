using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TestLib.Worker.ClientApi;

namespace TestLib.Worker
{
    internal class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static void SendResults(CancellationToken token)
        {
            IApiClient client = new HttpCodelabsApiClient();
            var logger = LogManager.GetCurrentClassLogger();
            Application app = Application.Get();

            token.Register(() => app.Requests.Enqueue(null));

            while (!token.IsCancellationRequested)
            {
                var request = app.Requests.Dequeue();
                if (request is null)
                {
                    break;
                }

                try
                {
                    client.SendRequest(request);
                }
                catch  (Exception ex)
                {
                    logger.Error("Error sending request {0} to server: {1}. Some data will be lose", request.RequestUri, ex);
                }
            }

            token.ThrowIfCancellationRequested();
        }

        private static void Main(string[] args)
        {
            Application app = Application.Get();
            LoggerManaged loggerManaged = new LoggerManaged();

            logger.Info("TestLib.Worker started");
            loggerManaged.InitNativeLogger(new LoggerManaged.LogEventHandler(LogManager.GetLogger("Internal").Log));

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task[] workerTasks = new Task[app.Configuration.WorkerSlotCount + 1];

            workerTasks[0] =
                Task.Run(() => SendResults(cancellationTokenSource.Token), cancellationTokenSource.Token);

            for (uint i = 1; i <= app.Configuration.WorkerSlotCount; i++)
            {
                workerTasks[i] =
                    Task.Run(() => new Slot(i, cancellationTokenSource.Token).Do(), cancellationTokenSource.Token);
            }

            for (; ; )
            {
                var cmd = Console.ReadLine();

                if (cmd == "exit")
                {
                    cancellationTokenSource.Cancel();
                    break;
                }
               // if ()
            }

            try { Task.WaitAll(workerTasks); }
            catch { }

            cancellationTokenSource.Dispose();
            loggerManaged.Destroy();
        }
    }
}
