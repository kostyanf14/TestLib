#include "Tester.h"
#include <cmath>

namespace Internal
{
	ULONG Tester::current_processor = 0;
	ULONG Tester::processor_count = 1;

	bool Tester::Run(bool useRestrictions)
	{
		Internal::logger->Debug(L"Called " __FUNCTIONW__);

		if (!workDirSet || !programSet)
		{
			Internal::logger->Error(L"Can't start program w/o program name or workdirectory. workDirectory = '%s', program = '%s'", workDirectory, program);

			return false;
		}
		if (!realTimeLimitSet)
		{
			Internal::logger->Warning(L"Real time limit was not set. workDirectory = '%s', program = '%s'", workDirectory, program);
		}
		if (!memoryLimitSet)
		{
			Internal::logger->Warning(L"Memory limit was not set. workDirectory = '%s', program = '%s'", workDirectory, program);
		}

		HANDLE hProcessCreationToken;

		if (userInfo.useLogon)
			hProcessCreationToken = LoginUser();
		else
			hProcessCreationToken = DuplicateCurrentProcessToken();

		if (hProcessCreationToken == INVALID_HANDLE_VALUE)
		{
			Internal::logger->Error(L"Can't start program while hProcessCreationToken is invalid. workDirectory = '%s', program = '%s'", workDirectory, program);

			return false;
		}

		startupHandles.job = CreateJobObjectW(nullptr, nullptr);

		STARTUPINFOEXW startupInfoEx = { 0 };
		startupInfoEx.StartupInfo.cb = sizeof(startupInfoEx);

		if (IoHandles.input != INVALID_HANDLE_VALUE)
		{
			startupInfoEx.StartupInfo.dwFlags |= STARTF_USESTDHANDLES;
			startupInfoEx.StartupInfo.hStdInput = IoHandles.input;
		}
		if (IoHandles.output != INVALID_HANDLE_VALUE)
		{
			startupInfoEx.StartupInfo.dwFlags |= STARTF_USESTDHANDLES;
			startupInfoEx.StartupInfo.hStdOutput = IoHandles.output;
		}
		if (IoHandles.error != INVALID_HANDLE_VALUE)
		{
			startupInfoEx.StartupInfo.dwFlags |= STARTF_USESTDHANDLES;
			startupInfoEx.StartupInfo.hStdError = IoHandles.error;
		}

		if (useRestrictions)
		{
			applyMandatoryLevel(hProcessCreationToken);
			applyMemoryLimit();
			applyUIRestrictions();
			//applyStartupAttribute(&startupInfoEx);
		}

		PROCESS_INFORMATION processInfo = { 0 };
		BOOL result = CreateProcessAsUserW(hProcessCreationToken, program, args,
			nullptr, nullptr, TRUE, CREATE_SUSPENDED | EXTENDED_STARTUPINFO_PRESENT | CREATE_NO_WINDOW | CREATE_BREAKAWAY_FROM_JOB,
			nullptr, workDirectory, (STARTUPINFOW*)&startupInfoEx, &processInfo);

		if (!result)
		{
			Internal::logger->Error(L"Can't create process in " __FUNCTIONW__ " at line %d. CreateProcessAsUserW failed, error %s", __LINE__, GetErrorMessage().get());

			return false;
		}

		if (useRestrictions)
		{
			applyAffinity();
		}

		if (!AssignProcessToJobObject(startupHandles.job, processInfo.hProcess))
		{
			Internal::logger->Error(L"Can't assign process to job in " __FUNCTIONW__ " at line %d. AssignProcessToJobObject failed, error %s", __LINE__, GetErrorMessage().get());

			if (!TerminateProcessS(startupHandles.process, -1))
				Internal::logger->Error(L"Failed to TerminateProcess. workDirectory = '%s', program = '%s'", workDirectory, program);

			SafeCloseHandle(&processInfo.hThread);
			SafeCloseHandle(&processInfo.hProcess);

			DeleteProcThreadAttributeList(startupInfoEx.lpAttributeList);
			free(startupInfoEx.lpAttributeList);
			TerminateJobObject(startupHandles.job, -1);

			SafeCloseHandle(&startupHandles.job);
			SafeCloseHandle(&hProcessCreationToken);

			return false;
		}

		BOOL res = FALSE;
		if (!IsProcessInJob(processInfo.hProcess, startupHandles.job, &res) || !res)
		{
			Internal::logger->Error(L"Process didnot assign to job in " __FUNCTIONW__ " at line %d. IsProcessInJob failed, error %s", __LINE__, GetErrorMessage().get());

			if (!TerminateProcessS(startupHandles.process, -1))
				Internal::logger->Error(L"Failed to TerminateProcess. workDirectory = '%s', program = '%s'", workDirectory, program);

			SafeCloseHandle(&processInfo.hThread);
			SafeCloseHandle(&processInfo.hProcess);

			DeleteProcThreadAttributeList(startupInfoEx.lpAttributeList);
			free(startupInfoEx.lpAttributeList);
			TerminateJobObject(startupHandles.job, -1);

			SafeCloseHandle(&startupHandles.job);
			SafeCloseHandle(&hProcessCreationToken);
			return false;
		}

		startTime = GetTickCount();
		if (ResumeThread(processInfo.hThread) == (DWORD)-1)
		{
			Internal::logger->Error(L"Can't resume main thread in " __FUNCTIONW__ " at line %d. ResumeThread failed, error %s", __LINE__, GetErrorMessage().get());

			if (!TerminateProcessS(startupHandles.process, -1))
				Internal::logger->Error(L"Failed to TerminateProcess. workDirectory = '%s', program = '%s'", workDirectory, program);

			SafeCloseHandle(&processInfo.hThread);
			SafeCloseHandle(&processInfo.hProcess);

			DeleteProcThreadAttributeList(startupInfoEx.lpAttributeList);
			free(startupInfoEx.lpAttributeList);
			TerminateJobObject(startupHandles.job, -1);

			SafeCloseHandle(&startupHandles.job);
			SafeCloseHandle(&hProcessCreationToken);
			return false;
		}

		DeleteProcThreadAttributeList(startupInfoEx.lpAttributeList);
		free(startupInfoEx.lpAttributeList);
		SafeCloseHandle(&hProcessCreationToken);

		startupHandles.process = processInfo.hProcess;
		startupHandles.thread = processInfo.hThread;

		return true;
	}

	TestLib::WaitingResult Tester::Wait()
	{
		TestLib::WaitingResult result;

		DWORD timeOut = limits.realTimeLimitMs - (GetTickCount() - startTime);

		DWORD waitCode = WaitForSingleObject(startupHandles.process, timeOut);
		switch (waitCode)
		{
		case WAIT_TIMEOUT:
			result = TestLib::WaitingResult::Timeout;

			if (!TerminateProcessS(startupHandles.process, -1))
			{
				Internal::logger->Error(L"Failed to TerminateProcess. workDirectory = '%s', program = '%s'", workDirectory, program);
				result = TestLib::WaitingResult::Fail;
			}

			SafeCloseHandle(&startupHandles.thread);
			SafeCloseHandle(&startupHandles.process);

			TerminateJobObject(startupHandles.job, -1);
			SafeCloseHandle(&startupHandles.job);

			usedResources.RealTimeUsageMS = limits.realTimeLimitMs + 1;
			usedResources.ProcessExitCode = WAIT_TIMEOUT;

			Internal::logger->Error(L"Waiting program timeout expired. workDirectory = '%s', program = '%s'", workDirectory, program);
			break;
		case WAIT_FAILED:
			result = TestLib::WaitingResult::Fail;
			
			if (!TerminateProcessS(startupHandles.process, -1))
				Internal::logger->Error(L"Failed to TerminateProcess. workDirectory = '%s', program = '%s'", workDirectory, program);

			SafeCloseHandle(&startupHandles.thread);
			SafeCloseHandle(&startupHandles.process);

			TerminateJobObject(startupHandles.job, -1);
			SafeCloseHandle(&startupHandles.job);

			usedResources.ProcessExitCode = -1;

			Internal::logger->Error(L"Waiting program failed. workDirectory = '%s', program = '%s'", workDirectory, program);
			break;

		case WAIT_OBJECT_0:
			GetExitCodeProcess(startupHandles.process, &usedResources.ProcessExitCode);

			result = TestLib::WaitingResult::Ok;

			Internal::logger->Info(L"Program waited successfully. workDirectory = '%s', program = '%s'", workDirectory, program);
			break;

		default:
			Internal::logger->Error(L"Error waiting process. Unknown status. status = %u, workDirectory = '%s', program = '%s'", waitCode, workDirectory, program);
			break;
		}

		usedResources.RealTimeUsageMS = static_cast<uint32>(GetTickCount() - startTime);

		if (startupHandles.job != INVALID_HANDLE_VALUE)
		{
			/*JOBOBJECT_BASIC_ACCOUNTING_INFORMATION basicAccountingInfo;
			if (!QueryInformationJobObject(startupHandles.job, JobObjectBasicAccountingInformation,
				&basicAccountingInfo, sizeof(JOBOBJECT_BASIC_ACCOUNTING_INFORMATION), nullptr))
			{
				//log->LogErrorLastSystemError(L"Error getting BASIC_ACCOUNTING_INFORMATION");
			}
			else
			{
				usedResources.cpuWorkTimeMs = static_cast<uint32>
					((basicAccountingInfo.TotalKernelTime.QuadPart + basicAccountingInfo.TotalUserTime.QuadPart) / 10000);
			}*/

			//TODO: check need if
			LARGE_INTEGER frequency;
			QueryPerformanceFrequency(&frequency);

			ULONG64 processTime;
			if (!QueryProcessCycleTime(startupHandles.process, &processTime))
			{
				Internal::logger->Error(L"Error get process cycle time in " __FUNCTIONW__ " at line %d. QueryProcessCycleTime failed, error %s. workDirectory = '%s', program = '%s'",
					__LINE__, GetErrorMessage().get(), workDirectory, program);
			}
			else
			{
				usedResources.CPUWorkTimeMS = trunc(10. * processTime / frequency.QuadPart) / 10.;
			}

			JOBOBJECT_EXTENDED_LIMIT_INFORMATION exLimitInfo;
			if (!QueryInformationJobObject(startupHandles.job, JobObjectExtendedLimitInformation,
				&exLimitInfo, sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION), nullptr))
			{
				//log->LogErrorLastSystemError(L"Error getting EXTENDED_LIMIT_INFORMATION");
			}
			else
			{
				usedResources.PeakMemoryUsageKB = trunc(10. * exLimitInfo.PeakJobMemoryUsed / 1024.) / 10.;
			}
		}

		return result;
	}
	void Tester::CloseIORedirectionHandles()
	{
		if (IoHandles.input != INVALID_HANDLE_VALUE)
			FlushFileBuffers(IoHandles.input);
		if (IoHandles.output != INVALID_HANDLE_VALUE)
			FlushFileBuffers(IoHandles.output);
		if (IoHandles.error != INVALID_HANDLE_VALUE)
			FlushFileBuffers(IoHandles.error);

		SafeCloseHandle(&IoHandles.input);
		SafeCloseHandle(&IoHandles.output);
		SafeCloseHandle(&IoHandles.error);
	}
}
