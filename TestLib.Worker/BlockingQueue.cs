using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestLib.Worker
{
	internal sealed class BlockingQueue<T>
	{
		private readonly Queue<T> queue = new Queue<T>();
		private readonly uint maxSize;
		public BlockingQueue(uint maxSize) { this.maxSize = maxSize; }

		public void Enqueue(T item)
		{
			lock (queue)
			{
				while (queue.Count >= maxSize)
					Monitor.Wait(queue);

				queue.Enqueue(item);
				if (queue.Count == 1)
					Monitor.PulseAll(queue); // wake up any blocked dequeue
			}
		}
		public T Dequeue()
		{
			T item = default(T);
			lock (queue)
			{
				while (queue.Count == 0)
					Monitor.Wait(queue);

				item = queue.Dequeue();
				if (queue.Count == maxSize - 1)
					Monitor.PulseAll(queue); // wake up any blocked enqueue
			}
			return item;
		}

		public int Count { get => queue.Count; }
	}
}
