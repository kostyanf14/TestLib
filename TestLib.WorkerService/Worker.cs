using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TestLib.Worker.ClientApi;
using TestLib.Worker.ClientApi.Models;

namespace TestLib.Worker
{
	internal class Worker
	{
		private static readonly Logger logger;
		private static readonly Regex re;
		private static readonly string compilerLogFilename;
		private static readonly string checkerBinaryFilename;
		private static readonly string solutionBinaryFilename;

		private readonly uint slotNum = 0;
		private readonly IApiClient apiClient;

		static Worker()
		{
			logger = LogManager.GetCurrentClassLogger();
			re = new Regex(@"\$\((\w+)\)", RegexOptions.Compiled);

			compilerLogFilename = "compiler.log";
			checkerBinaryFilename = "checker.exe";
			solutionBinaryFilename = "solution.exe";
		}

		public Worker(uint slotNum, IApiClient client)
		{
			this.slotNum = slotNum;
			apiClient = client;
		}

		private Dictionary<string, string> GenerateReplacementDictionary(string workDirecrory,
			string binaryFilename, string binaryFullPath,
			string sourceFilename = null, string sourceFullPath = null,
			string compilerLogFilename = null, string compilerLogFullPath = null,
			string inputFileName = null, string inputFilePath = null,
			string outputFileName = null, string outputFilePath = null,
			string answerFileName = null, string answerFilePath = null,
			string reportFileName = null, string reportFilePath = null)
		{
			var replacement = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
					{
						{"$(BinaryFullPath)", binaryFullPath },
						{"$(BinaryFilename)", binaryFilename },
						{"$(WorkDirecrory)", workDirecrory },
					};

			if (compilerLogFullPath != null)
			{
				replacement.Add("$(SourceFilename)", sourceFilename);
				replacement.Add("$(SourceFilenameWithOutExtention)", sourceFilename.Remove(sourceFilename.Length - 4, 4));
				//SourceFilenameWithOutExtention
			}
			if (compilerLogFilename != null)
			{
				replacement.Add("$(SourceFullPath)", sourceFullPath);
			}

			if (compilerLogFilename != null)
			{
				replacement.Add("$(CompilerLogFilename)", compilerLogFilename);
			}

			if (compilerLogFullPath != null)
			{
				replacement.Add("$(CompilerLogFullPath)", compilerLogFullPath);
			}

			if (inputFileName != null)
			{
				replacement.Add("$(InputFileName)", inputFileName);
			}

			if (inputFilePath != null)
			{
				replacement.Add("$(InputFilePath)", inputFilePath);
			}

			if (outputFileName != null)
			{
				replacement.Add("$(OutputFileName)", outputFileName);
			}

			if (outputFilePath != null)
			{
				replacement.Add("$(OutputFilePath)", outputFilePath);
			}

			if (answerFileName != null)
			{
				replacement.Add("$(AnswerFileName)", answerFileName);
			}

			if (answerFilePath != null)
			{
				replacement.Add("$(AnswerFilePath)", answerFilePath);
			}

			if (reportFileName != null)
			{
				replacement.Add("$(ReportFileName)", reportFileName);
			}

			if (reportFilePath != null)
			{
				replacement.Add("$(ReportFilePath)", reportFilePath);
			}

			return replacement;
		}

		private bool compile(string workdir, Compiler compiler,
			Dictionary<string, string> replacement, string compilerLogFullPath,
			bool isChecker = false)
		{
			for (int i = 0; i < compiler.Commands.Count; i++)
			{
				Tester tester = new Tester();

				string program = compiler.Commands[i].Program.ReplaceByDictionary(re, replacement);
				string args = compiler.Commands[i].Arguments.ReplaceByDictionary(re, replacement);

				if (isChecker)
				{
					args = $"{args} {compiler.Commands[i].CheckerArguments.ReplaceByDictionary(re, replacement)}";
				}

				tester.SetProgram(program, $"\"{program}\" {args}");

				tester.SetWorkDirectory(workdir);
				tester.SetRealTimeLimit(compiler.CompilersRealTimeLimit);
				tester.RedirectIOHandleToFile(IOHandleType.Output, compilerLogFullPath);
				tester.RedirectIOHandleToHandle(IOHandleType.Error, tester.GetIORedirectedHandle(IOHandleType.Output));

				if (tester.Run(false))
				{
					logger.Info("Slot {0}: Compiler run successfully", slotNum);
				}
				else
				{
					logger.Error("Slot {0}: Can't run compiler", slotNum);

					tester.Destroy();
					return false;
				}

				if (tester.Wait() == WaitingResult.Ok)
				{
					logger.Debug("Slot {0}: Waited successfully", slotNum);
				}
				else
				{
					logger.Error("Slot {0}: Wait failed", slotNum);

					tester.Destroy();
					return false;
				}

				uint exitCode = tester.GetExitCode();
				if (exitCode == 0)
				{
					logger.Info("Slot {0}: Compiler exited successfully", slotNum);
				}
				else
				{
					logger.Error("Slot {0}: Compiler exit with code {0}", slotNum, exitCode);

					tester.Destroy();
					return false;
				}

				tester.Destroy();
			}

			logger.Info("File {0} compiled successfully", replacement["$(SourceFilename)"]);
			return true;
		}

		private bool compileChecker(string workdir, ProblemFile checker, Compiler compiler)
		{
			string sourceFilename = $"checker{compiler.FileExt}";
			string sourceFullPath = Path.Combine(workdir, sourceFilename);
			string compilerLogFullPath = Path.Combine(workdir, Application.Get().Configuration.CompilerLogFileName);

			Application.Get().FileProvider.Copy(checker, sourceFullPath);

			var replacement = GenerateReplacementDictionary(
					   sourceFullPath: sourceFullPath,
					   sourceFilename: sourceFilename,
					   binaryFullPath: Path.Combine(workdir, checkerBinaryFilename),
					   binaryFilename: checkerBinaryFilename,
					   workDirecrory: workdir,
					   compilerLogFilename: Application.Get().Configuration.CompilerLogFileName,
					   compilerLogFullPath: compilerLogFullPath
				   );

			return compile(workdir, compiler, replacement, compilerLogFullPath, true);
		}

		private bool compileSolution(string workdir, ProblemFile solution, Compiler compiler)
		{
			string sourceFilename = $"solution{compiler.FileExt}";
			string sourceFullPath = Path.Combine(workdir, sourceFilename);
			string compilerLogFullPath = Path.Combine(workdir, Application.Get().Configuration.CompilerLogFileName);

			Application.Get().FileProvider.Copy(solution, sourceFullPath);

			var replacement = GenerateReplacementDictionary(
					   sourceFullPath: sourceFullPath,
					   sourceFilename: sourceFilename,
					   binaryFullPath: Path.Combine(workdir, solutionBinaryFilename),
					   binaryFilename: solutionBinaryFilename,
					   workDirecrory: workdir,
					   compilerLogFilename: Application.Get().Configuration.CompilerLogFileName,
					   compilerLogFullPath: compilerLogFullPath
				   );

			return compile(workdir, compiler, replacement, compilerLogFullPath, false);
		}

		public WorkerResult Testing(Submission submission, Problem problem, ProblemFile solution)
		{
			string workdir = new DirectoryInfo(Path.Combine(Application.Get().Configuration.TestingWorkDirectory, Guid.NewGuid().ToString())).FullName;
			Directory.CreateDirectory(workdir);
			logger.Info("Slot {0} starting testing at {1}", slotNum, workdir);

			Compiler checkerCompiler = Application.Get().Compilers.GetCompiler(problem.CheckerCompilerId);
			Compiler solutionCompiler = Application.Get().Compilers.GetCompiler(submission.CompilerId);

			string inputFileFullPath = Path.Combine(workdir, Application.Get().Configuration.InputFileName);
			string outputFileFullPath = Path.Combine(workdir, Application.Get().Configuration.OutputFileName);
			string answerFileFullPath = Path.Combine(workdir, Application.Get().Configuration.AnswerFileName);
			string reportFileFullPath = Path.Combine(workdir, Application.Get().Configuration.ReportFileName);
			string compilerLogFileFullPath = Path.Combine(workdir, Application.Get().Configuration.CompilerLogFileName);

			if (!compileChecker(workdir, problem.Checker, checkerCompiler))
			{
				SubmissionLog log = new SubmissionLog();
				log.SubmissionId = submission.Id;
				log.Type = SubmissionLogType.Checker;
				log.Data = File.ReadAllText(compilerLogFileFullPath);

				Application.Get().RequestMessages.Enqueue(apiClient.GetSendLogRequestMessage(log));
				return WorkerResult.TestingError;
			}

			{
				bool st = compileSolution(workdir, solution, solutionCompiler);
				SubmissionLog log = new SubmissionLog();
				log.SubmissionId = submission.Id;
				log.Type = SubmissionLogType.Source;
				log.Data = File.ReadAllText(compilerLogFileFullPath);

				if (!string.IsNullOrWhiteSpace(log.Data))
				{
					Application.Get().RequestMessages.Enqueue(apiClient.GetSendLogRequestMessage(log));
				}

				if (!st)
				{
					return WorkerResult.CompilerError;
				}
			}

			for (uint i = 0; i < problem.Tests.Length; i++)
			{
				logger.Info("Slot {0}: Starting testing test with num {1}", slotNum, problem.Tests[i].Num);

				logger.Info("Slot {0}: Preparion solution start enviroment", slotNum);
				logger.Debug("Slot {0}: Copy input file.", slotNum);
				Application.Get().FileProvider.Copy(problem.Tests[i].Input, inputFileFullPath);

				TestResult testResult = new TestResult();
				testResult.SubmissionId = submission.Id;
				testResult.TestId = problem.Tests[i].Id;

				{
					Tester tester = new Tester();

					var replacement = GenerateReplacementDictionary(
						binaryFullPath: Path.Combine(workdir, solutionBinaryFilename),
						binaryFilename: solutionBinaryFilename,
						workDirecrory: workdir);

					string program = solutionCompiler.RunCommand.Program.ReplaceByDictionary(re, replacement);
					string args = solutionCompiler.RunCommand.Arguments.ReplaceByDictionary(re, replacement);

					tester.SetProgram(program, $"\"{program}\" {args}");

					tester.SetWorkDirectory(workdir);
					tester.SetRealTimeLimit(submission.RealTimeLimit);
					tester.SetMemoryLimit(submission.MemoryLimit);
					tester.RedirectIOHandleToFile(IOHandleType.Input, inputFileFullPath);
					tester.RedirectIOHandleToFile(IOHandleType.Output, outputFileFullPath);
					tester.RedirectIOHandleToHandle(IOHandleType.Error, tester.GetIORedirectedHandle(IOHandleType.Output));

					logger.Info("Slot {0}: Run solution", slotNum);
					if (tester.Run(true))
					{
						logger.Info("Slot {0}: Solution run successfully", slotNum);
					}
					else
					{
						logger.Error("Slot {0}: Can't run solution", slotNum);
						return WorkerResult.TestingError;
					}

					var waitStatus = tester.Wait();
					if (waitStatus == WaitingResult.Ok)
					{
						logger.Debug("Slot {0}: Waited successfully", slotNum);
					}
					else if (waitStatus == WaitingResult.Timeout)
					{
						testResult.UsedMemmory = tester.GetUsedResources().PeakMemoryUsageKB;
						testResult.WorkTime = tester.GetUsedResources().RealTimeUsageMS;

						testResult.Result = TestingResult.TimeLimitExceeded;
						Application.Get().RequestMessages.Enqueue(apiClient.GetSendTestingResultRequestMessage(testResult));

						logger.Info("Slot {0}: Wait timeouted", slotNum);

						//Not start checker
						tester.Destroy();
						continue;
					}
					else
					{
						logger.Error("Slot {0}: Wait failed", slotNum);
						return WorkerResult.TestingError;
					}

					uint exitCode = tester.GetExitCode();
					if (exitCode == 0)
					{
						logger.Info("Slot {0}: Solution exited successfully", slotNum);
					}
					else
					{
						testResult.UsedMemmory = tester.GetUsedResources().PeakMemoryUsageKB;
						testResult.WorkTime = tester.GetUsedResources().CPUWorkTimeMS;

						testResult.Result = TestingResult.RuntimeError;
						Application.Get().RequestMessages.Enqueue(apiClient.GetSendTestingResultRequestMessage(testResult));

						logger.Info("Slot {0}: Solution exit with code {1}", slotNum, exitCode);

						//Not start checker
						tester.Destroy();
						continue;
					}

					testResult.UsedMemmory = tester.GetUsedResources().PeakMemoryUsageKB;
					testResult.WorkTime = tester.GetUsedResources().CPUWorkTimeMS;

					tester.Destroy();
				}

				if (testResult.WorkTime > submission.TimeLimit)
				{
					testResult.Result = TestingResult.TimeLimitExceeded;

					Application.Get().RequestMessages.Enqueue(apiClient.GetSendTestingResultRequestMessage(testResult));

					logger.Info("Slot {0}: Solution work {1}ms and time limit {2}ms",
						slotNum, testResult.WorkTime, submission.TimeLimit);

					//Not start checker
					continue;
				}

				if (testResult.UsedMemmory > submission.MemoryLimit)
				{
					testResult.Result = TestingResult.MemoryLimitExceeded;
					Application.Get().RequestMessages.Enqueue(apiClient.GetSendTestingResultRequestMessage(testResult));

					logger.Info("Slot {0}: Solution used {1}kb memory and memory limit {2}kb",
						slotNum, testResult.UsedMemmory, submission.MemoryLimit);

					//Not start checker
					continue;
				}

				logger.Info("Slot {0}: Preparion checker start enviroment", slotNum);
				logger.Info("Slot {0}: Copy answer file.", slotNum);
				Application.Get().FileProvider.Copy(problem.Tests[i].Answer, answerFileFullPath);

				{
					Tester tester = new Tester();

					var replacement = GenerateReplacementDictionary(
						binaryFullPath: Path.Combine(workdir, checkerBinaryFilename),
						binaryFilename: checkerBinaryFilename,
						workDirecrory: workdir,
						inputFilePath: inputFileFullPath,
						outputFilePath: outputFileFullPath,
						answerFilePath: answerFileFullPath,
						reportFilePath: reportFileFullPath);

					string program = checkerCompiler.RunCommand.Program.ReplaceByDictionary(re, replacement);
					string args = $"{checkerCompiler.RunCommand.Arguments} {checkerCompiler.RunCommand.CheckerArguments}".
						ReplaceByDictionary(re, replacement);

					tester.SetProgram(program, $"\"{program}\" {args}");

					tester.SetWorkDirectory(workdir);
					tester.SetRealTimeLimit(60 * 1000);
					//tester.RedirectIOHandleToFile(IOHandleType.Output, reportFileFullPath);
					//tester.RedirectIOHandleToHandle(IOHandleType.Error, tester.GetIORedirectedHandle(IOHandleType.Output));

					logger.Info("Slot {0}: Run checker", slotNum);
					if (tester.Run())
					{
						logger.Info("Slot {0}: Checker run successfully", slotNum);
					}
					else
					{
						logger.Error("Slot {0}: Can't run checker", slotNum);
						return WorkerResult.TestingError;
					}

					if (tester.Wait() == WaitingResult.Ok)
					{
						logger.Debug("Slot {0}: Waited successfully", slotNum);
					}
					else
					{
						logger.Error("Slot {0}: Wait failed", slotNum);
						return WorkerResult.TestingError;
					}

					uint exitCode = tester.GetExitCode();
					logger.Info("Slot {0}: Checker exit with code {1}", slotNum, exitCode);

					testResult.Result = (TestingResult)exitCode;
					testResult.Log = File.ReadAllText(reportFileFullPath);

					Application.Get().RequestMessages.Enqueue(apiClient.GetSendTestingResultRequestMessage(testResult));

					tester.Destroy();
				}

			}

			try
			{
				Directory.Delete(workdir, true);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Slot {0}: Can not delete work directory.", slotNum);
			}

			return WorkerResult.Ok;
		}
	}
}
