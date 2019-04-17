using System;

namespace TestLib.WorkerService
{
	internal class TimeOutHelper
	{
		private int currentTimeOut = 1;

		public int GetTimeOut()
			=> currentTimeOut = Math.Min(currentTimeOut * 2, 64);

		public void Zero() => currentTimeOut = 1;
	}
}
