using System;
using System.Collections.Generic;

namespace TestLib.Worker
{
	internal class ProblemCache
	{
		private readonly uint MaxSize;
		private LinkedList<ulong> cachedProblems = new LinkedList<ulong>();
		private Dictionary<ulong, Problem> problems = new Dictionary<ulong, Problem>();
		private Dictionary<ulong, LinkedListNode<ulong>> problemsNode = new Dictionary<ulong, LinkedListNode<ulong>>();
		private readonly object sync = new object();

		public ProblemCache(uint maxSize = 1)
		{
			MaxSize = maxSize;
		}

		public Problem FetchProblem(ulong id, DateTime updatedAt, Func<Problem> func)
		{
			var contains = problems.TryGetValue(id, out var problem);

			if (contains && problem.LastUpdate == updatedAt)
			{
				return getProblem(id);
			}
			else
			{
				lock (sync)
				{
					contains = problems.TryGetValue(id, out problem);

					if (contains && problem.LastUpdate == updatedAt)
					{
						return getProblem(id);
					}
					else
					{
						problem = func();
						addOrUpdateProblem(problem, contains);
						return getProblem(problem.Id);
					}
				}
			}
		}

		public void Clear()
		{
			lock (sync)
			{
				foreach (var problem in problems)
				{
					Application.Get().FileProvider.RemoveProblem(problem.Value);
				}

				cachedProblems.Clear();
				problems.Clear();
				problemsNode.Clear();
			}
		}

		private void addOrUpdateProblem(Problem problem, bool update)
		{
			if (update) { updateProblem(problem); } else { addProblem(problem); }
		}

		private void addProblem(Problem problem)
		{
			if (cachedProblems.Count == MaxSize)
			{
				var lastProblem = cachedProblems.Last.Value;
				cachedProblems.RemoveLast();

				Application.Get().FileProvider.RemoveProblem(problems[lastProblem]);
				problems.Remove(lastProblem);
				problemsNode.Remove(lastProblem);
			}

			cachedProblems.AddFirst(problem.Id);
			problemsNode.Add(problem.Id, cachedProblems.First);
			problems.Add(problem.Id, problem);
			Application.Get().FileProvider.SaveProblem(problem);
			GC.Collect();
		}

		private void updateProblem(Problem newProblem)
		{
			LinkedListNode<ulong> node = problemsNode[newProblem.Id];
			cachedProblems.Remove(node);
			cachedProblems.AddFirst(node);

			var currentProblem = problems[newProblem.Id];
			if (currentProblem.LastUpdate != newProblem.LastUpdate)
			{
				Application.Get().FileProvider.RemoveProblem(currentProblem);
				Application.Get().FileProvider.SaveProblem(newProblem);

				problems[newProblem.Id] = newProblem;
			}
		}

		private Problem getProblem(ulong id)
		{
			var problem = problems[id];
			var node = problemsNode[id];

			cachedProblems.Remove(node);
			cachedProblems.AddFirst(node);


			return problem;
		}
	}
}
