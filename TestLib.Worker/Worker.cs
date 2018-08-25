using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TestLib.Worker.ClientApi.Models;

namespace TestLib.Worker
{
	class Worker
	{
		static readonly Logger logger;
		static readonly Regex re;

		static readonly string compilerLogFilename;
		static readonly string checkerBinaryFilename;
		static readonly string solutionBinaryFilename;

		uint slotNum = 0;

		static Worker()
		{
			logger = LogManager.GetCurrentClassLogger();
			re = new Regex(@"\$\((\w+)\)", RegexOptions.Compiled);

			compilerLogFilename = "compiler.log";
			checkerBinaryFilename = "checker.exe";
			solutionBinaryFilename = "solution.exe";
		}

		public Worker(uint slotNum)
		{
			this.slotNum = slotNum;
		}

		Dictionary<string, string> GenerateReplacementDictionary(string workDirecrory,
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
				replacement.Add("$(SourceFullPath)", sourceFullPath);

			if (compilerLogFilename != null)
				replacement.Add("$(CompilerLogFilename)", compilerLogFilename);
			if (compilerLogFullPath != null)
				replacement.Add("$(CompilerLogFullPath)", compilerLogFullPath);

			if (inputFileName != null)
				replacement.Add("$(InputFileName)", inputFileName);
			if (inputFilePath != null)
				replacement.Add("$(InputFilePath)", inputFilePath);

			if (outputFileName != null)
				replacement.Add("$(OutputFileName)", outputFileName);
			if (outputFilePath != null)
				replacement.Add("$(OutputFilePath)", outputFilePath);

			if (answerFileName != null)
				replacement.Add("$(AnswerFileName)", answerFileName);
			if (answerFilePath != null)
				replacement.Add("$(AnswerFilePath)", answerFilePath);

			if (reportFileName != null)
				replacement.Add("$(ReportFileName)", reportFileName);
			if (reportFilePath != null)
				replacement.Add("$(ReportFilePath)", reportFilePath);

			return replacement;
		}

		bool compile(string workdir, Compiler compiler,
			Dictionary<string, string> replacement, string compilerLogFullPath,
			bool isChecker = false)
		{
			for (int i = 0; i < compiler.Commands.Count; i++)
			{
				Tester tester = new Tester();

				string program = re.Replace(compiler.Commands[i].Program, match => replacement[match.Value]);
				string args = re.Replace(isChecker ?
					compiler.Commands[i].Arguments + " " + compiler.Commands[i].CheckerArguments :
					compiler.Commands[i].Arguments, match => replacement[match.Value]);

				tester.SetProgram(program, $"\"{program}\" {args}");

				tester.SetWorkDirectory(workdir);
				tester.SetRealTimeLimit(compiler.CompilersRealTimeLimit);
				tester.RedirectIOHandleToFile(IOHandleType.Output, compilerLogFullPath);
				tester.RedirectIOHandleToHandle(IOHandleType.Error, tester.GetIORedirectedHandle(IOHandleType.Output));

				if (tester.Run(!isChecker))
					logger.Info("Slot {0}: Compiler run successfully", slotNum);
				else
				{
					logger.Error("Slot {0}: Can't run compiller", slotNum);
					//Log Win32Error

					tester.Destroy();
					return false;
				}

				if (tester.Wait())
					logger.Info("Slot {0}: Waiting started", slotNum);
				else
				{
					logger.Error("Slot {0}: Wait failed", slotNum);
					//Log Win32Error

					tester.Destroy();
					return false;
				}

				uint exitCode = tester.GetExitCode();
				if (exitCode == 0)
					logger.Info("Slot {0}: Compiler exited successfully", slotNum);
				else
				{
					logger.Error("Slot {0}: Compiler exit with code {0}", slotNum, exitCode);
					//Log Win32Error

					tester.Destroy();
					return false;
				}

				tester.Destroy();
			}

			logger.Info("File {0} compiled successfully", replacement["$(SourceFilename)"]);
			return true;
		}

		bool compileChecker(string workdir, ProblemFile checker, Compiler compiler)
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

		bool compileSolution(string workdir, ProblemFile solution, Compiler compiler)
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

			return compile(workdir, compiler, replacement, compilerLogFullPath, true);
		}

		public bool Testing(Submission submission, Problem problem, ProblemFile solution)
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
				TestResult testResult = new TestResult();
				testResult.SubmissionId = submission.Id;
				testResult.TestId = problem.Tests[0].Id;
				testResult.Result = TestingResult.TestingError;

				Application.Get().TestingResults.Enqueue(testResult);
				return false;
			}
			if (!compileSolution(workdir, solution, solutionCompiler))
			{
				TestResult testResult = new TestResult();
				testResult.SubmissionId = submission.Id;
				testResult.TestId = problem.Tests[0].Id;
				testResult.Result = TestingResult.CompilerError;
				testResult.CompilerLog = File.ReadAllText(compilerLogFileFullPath);

				Application.Get().TestingResults.Enqueue(testResult);
				return true;
			}

			for (uint i = 0; i < problem.Tests.Length; i++)
			{
				logger.Info("Slot {0}: Statring testing test with num {1}", slotNum, problem.Tests[i].Num);

				logger.Info("Slot {0}: Preparion solution start enviroment", slotNum);
				logger.Info("Slot {0}: Copy input file.", slotNum);
				Application.Get().FileProvider.Copy(problem.Tests[i].Input, inputFileFullPath);

				UsedResources solutionUsedResources;
				TestResult testResult = new TestResult();
				testResult.SubmissionId = submission.Id;
				testResult.TestId = problem.Tests[i].Id;

				{
					Tester tester = new Tester();

					var replacement = GenerateReplacementDictionary(
						binaryFullPath: Path.Combine(workdir, solutionBinaryFilename),
						binaryFilename: solutionBinaryFilename,
						workDirecrory: workdir);

					string program = re.Replace(solutionCompiler.RunCommand.Program, match => replacement[match.Value]);
					string args = re.Replace(solutionCompiler.RunCommand.Arguments, match => replacement[match.Value]);

					tester.SetProgram(program, $"\"{program}\" {args}");

					tester.SetWorkDirectory(workdir);
					tester.SetRealTimeLimit(60 * 1000);
					tester.RedirectIOHandleToFile(IOHandleType.Input, inputFileFullPath);
					tester.RedirectIOHandleToFile(IOHandleType.Output, outputFileFullPath);
					tester.RedirectIOHandleToHandle(IOHandleType.Error, tester.GetIORedirectedHandle(IOHandleType.Output));

					logger.Info("Slot {0}: Run solution", slotNum);
					if (tester.Run(true))
						logger.Info("Slot {0}: Solution run successfully", slotNum);
					else
					{
						testResult.Result = TestingResult.RunTimeError;
						Application.Get().TestingResults.Enqueue(testResult);

						logger.Error("Slot {0}: Can't run solution", slotNum);
						//Log Win32Error
						return false;
					}

					if (tester.Wait())
						logger.Info("Slot {0}: Waiting started", slotNum);
					else
					{
						testResult.Result = TestingResult.RunTimeError;
						Application.Get().TestingResults.Enqueue(testResult);

						logger.Error("Slot {0}: Wait failed", slotNum);
						//Log Win32Error
						return false;
					}

					uint exitCode = tester.GetExitCode();
					if (exitCode == 0)
						logger.Info("Slot {0}: Solution exited successfully", slotNum);
					else
					{
						testResult.Result = TestingResult.RunTimeError;
						Application.Get().TestingResults.Enqueue(testResult);

						logger.Error("Slot {0}: Solution exit with code {1}", slotNum, exitCode);
						//Log Win32Error
						return false;
					}

					solutionUsedResources = tester.GetUsedResources();

					tester.Destroy();
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

					string program = re.Replace(checkerCompiler.RunCommand.Program, match => replacement[match.Value]);
					string args = re.Replace(checkerCompiler.RunCommand.Arguments
						 + " " + checkerCompiler.RunCommand.CheckerArguments, match => replacement[match.Value]);

					tester.SetProgram(program, $"\"{program}\" {args}");

					tester.SetWorkDirectory(workdir);
					tester.SetRealTimeLimit(60 * 1000);
					tester.RedirectIOHandleToFile(IOHandleType.Output, reportFileFullPath);
					tester.RedirectIOHandleToHandle(IOHandleType.Error, tester.GetIORedirectedHandle(IOHandleType.Output));

					logger.Info("Slot {0}: Run checker", slotNum);
					if (tester.Run())
						logger.Info("Slot {0}: Checker run successfully", slotNum);
					else
					{
						testResult.Result = TestingResult.RunTimeError;
						Application.Get().TestingResults.Enqueue(testResult);

						logger.Error("Slot {0}: Can't run checker", slotNum);
						//Log Win32Error
						return false;
					}

					if (tester.Wait())
						logger.Info("Slot {0}: Waiting started", slotNum);
					else
					{
						testResult.Result = TestingResult.RunTimeError;
						Application.Get().TestingResults.Enqueue(testResult);

						logger.Error("Slot {0}: Wait failed", slotNum);
						//Log Win32Error
						return false;
					}

					uint exitCode = tester.GetExitCode();
					logger.Info("Slot {0}: Checker exit with code {1}", slotNum, exitCode);

					testResult.Result = (TestingResult)exitCode;

					testResult.WorkTime = solutionUsedResources.cpuWorkTimeMs;
					testResult.UsedMemmory = solutionUsedResources.peakMemoryUsageKb;

					Application.Get().TestingResults.Enqueue(testResult);

					tester.Destroy();
				}

			}
			return true;
			//Directory.Delete(workdir, true);
		}
	}
}
