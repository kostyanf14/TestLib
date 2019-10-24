using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TestLib.Worker
{
	class FileProvider
	{
		public readonly string directory;
		static Logger logger = LogManager.GetCurrentClassLogger();
		public FileProvider()
		{
			logger.Debug("ctor");

			directory = "c:\\cache\\";
		}
		public FileProvider(string _dir)
		{
			logger.Debug("ctor");

			directory = _dir;
		}

		public void Init()
		{
			logger.Info("Initialization started");

			CleanDirectory(directory);
		}

		public void SaveProblem(Problem problem)
		{
			SaveFile(problem.Checker);

			foreach (var test in problem.Tests)
			{
				SaveFile(test.Input);

				if (test.Answer.Content != null)
				{
					SaveFile(test.Answer);
				}
			}
		}

		public void RemoveProblem(Problem problem)
		{
			RemoveFile(problem.Checker);

			foreach (var test in problem.Tests)
			{
				RemoveFile(test.Input);
				RemoveFile(test.Answer);
			}
		}

		public void SaveFile(ProblemFile file)
		{
			if (file.Content != null)
			{
				if (string.IsNullOrEmpty(file.id))
					file.id = Guid.NewGuid().ToString();

				File.WriteAllBytes(
					Path.Combine(directory, file.id),
					file.Content);

				file.Content = null;
			}
		}
		public void Copy(ProblemFile file, string path)
		{
			if (file.Content == null)
				File.Copy(Path.Combine(directory, file.id), path, true);
			else
				File.WriteAllBytes(path, file.Content);
		}
		public void RemoveFile(ProblemFile file)
		{
			if (file.Content == null && file.id != null)
				File.Delete(Path.Combine(directory, file.id));
			else
				file.Content = null;
		}

		public void LoadContent(ProblemFile file)
		{
			file.Content = null;
			file.Content = File.ReadAllBytes(
				Path.Combine(directory, file.id));
		}

		private static void CleanDirectory(string directory)
		{
			DirectoryInfo dirInfo = new DirectoryInfo(directory);

			if (dirInfo.Exists)
			{
				foreach (FileInfo file in dirInfo.GetFiles())
					DeleteFile(file.FullName);

				foreach (DirectoryInfo dir in dirInfo.GetDirectories())
					DeleteDirectory(dir.FullName);
			}
			else
				Directory.CreateDirectory(dirInfo.FullName);
		}

		private static void DeleteDirectory(string directory)
		{
			DirectoryInfo dirInfo = new DirectoryInfo(directory);

			if (dirInfo.Exists)
			{
				CleanDirectory(directory);
				dirInfo.Delete(true);
			}
		}

		private static void DeleteFile(string file)
		{
			FileInfo fileInfo = new FileInfo(file);

			if (fileInfo.Exists)
			{
				ResetFileAttributes(fileInfo.FullName);
				fileInfo.Delete();
			}
		}

		private static void ResetFileAttributes(string file)
		{
			if (File.Exists(file))
				File.SetAttributes(file, FileAttributes.Normal);
		}
	}
}
