using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib.Worker
{
    internal class ProblemCache
    {
        readonly uint MaxSize;
        public ProblemCache(uint maxSize = 1)
        {
            MaxSize = maxSize;
        }

        public void AddProblem(Problem problem)
        {
            LinkedListNode<ulong> node = cachedProblems.Find(problem.Id);
            if (node != null)
            {
                cachedProblems.Remove(node);
                cachedProblems.AddFirst(node);

				if (problems[problem.Id].LastUpdate != problem.LastUpdate)
				{
					Application.Get().FileProvider.RemoveProblem(problems[problem.Id]);
					Application.Get().FileProvider.SaveProblem(problem);

					problems[problem.Id] = problem;
				}
			}
            else
            {
                if (cachedProblems.Count == MaxSize)
                {
                    Application.Get().FileProvider.RemoveProblem(problems[cachedProblems.Last.Value]);
                    problems.Remove(cachedProblems.Last.Value);
                    problemsNode.Remove(cachedProblems.Last.Value);
                    cachedProblems.RemoveLast();
                }

                cachedProblems.AddFirst(problem.Id);
                problemsNode.Add(problem.Id, cachedProblems.First);
                problems.Add(problem.Id, problem);
                Application.Get().FileProvider.SaveProblem(problem);
            }
        }
        public Problem GetProblem(ulong id)
        {
            if (problems.ContainsKey(id))
            {
                cachedProblems.Remove(problemsNode[id]);
                cachedProblems.AddFirst(problemsNode[id]);

                return problems[id];
            }
            return null;
        }

		public bool CheckProblem(ulong id, DateTime updatedAt = default(DateTime)) => 
			problems.ContainsKey(id) && problems[id].LastUpdate == updatedAt;

		public void Clear()
		{
			lock (problems)
			{
				foreach (var problem in problems)
					Application.Get().FileProvider.RemoveProblem(problem.Value);

				cachedProblems.Clear();
				problems.Clear();
				problemsNode.Clear();
			}
		}

        LinkedList<ulong> cachedProblems = new LinkedList<ulong>();
        Dictionary<ulong, Problem> problems = new Dictionary<ulong, Problem>();
        Dictionary<ulong, LinkedListNode<ulong>> problemsNode = new Dictionary<ulong, LinkedListNode<ulong>>();
    }
}
