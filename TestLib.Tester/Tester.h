#pragma once
#include <Windows.h>
#include <cstdio>

#include "Utils.h"
#include "Logger.h"

#pragma comment(lib, "kernel32.lib")
#pragma comment(lib, "user32.lib")
#pragma comment(lib, "advapi32.lib")

using uint8 = unsigned char;
using uint16 = unsigned short int;
using uint32 = unsigned long int;

#define nameof(name) #name

namespace TestLib
{
	public enum class IOHandleType
	{
		Input,
		Output,
		Error,
	};

	public enum class TestingResult
	{
		Ok = 0,
		WrongAnswer = 1,
		PresentationError = 2,
		Dirt = 4,
		Points = 5,
		BadTest = 6,
		UnexpectedEof = 8,
		CompilerError = 9,
		RunTimeError = 10,
		TestingError = 13,
		MemoryLimitExceded = 14,
		TimeLimitExceded = 15,
		PartillyCorrect = 16
	};

	public value class UsedResources
	{
	public:
		uint32 realTimeUsageMs;
		uint32 cpuWorkTimeMs;
		uint32 peakMemoryUsageKb;
		uint32 processExitCode;
	};
}

namespace Internal
{
	HANDLE DuplicateCurrentProcessToken();
	bool SetTokenIntegrityLevel(HANDLE hToken, DWORD mandatoryLabelAuthority);
	bool SetProcessIntegrityLevel(HANDLE hProcess, DWORD mandatoryLabelAuthority);

	class Tester
	{
	public:
		Tester()
		{
			programSet = false;
			realTimeLimitSet = false;
			memoryLimitSet = false;
			workDirSet = false;

			IoHandles.input = INVALID_HANDLE_VALUE;
			IoHandles.output = INVALID_HANDLE_VALUE;
			IoHandles.error = INVALID_HANDLE_VALUE;

			startupHandles.process = INVALID_HANDLE_VALUE;
			startupHandles.thread = INVALID_HANDLE_VALUE;
			startupHandles.job = INVALID_HANDLE_VALUE;

			limits.realTimeLimitMs = 0;
			limits.memoryLimitKb = 0;

			usedResources.cpuWorkTimeMs = 0;
			usedResources.peakMemoryUsageKb = 0;
			usedResources.processExitCode = 0;
			usedResources.realTimeUsageMs = 0;

			program = nullptr;
			args = nullptr;
			workDirectory = nullptr;
		}

		~Tester()
		{
			programSet = false;
			realTimeLimitSet = false;
			memoryLimitSet = false;
			workDirSet = false;

			SafeCloseHandle(&IoHandles.input);
			SafeCloseHandle(&IoHandles.output);
			//SafeCloseHandle(&IoHandles.error);

			if (startupHandles.process != INVALID_HANDLE_VALUE)
			{
				TerminateProcess(startupHandles.process, 0);
				//SafeCloseHandle(&startupHandles.process);
				//SafeCloseHandle(&startupHandles.thread);
			}

			TerminateJobObject(startupHandles.job, 0);
			//SafeCloseHandle(&startupHandles.job);

			limits.realTimeLimitMs = 0;
			limits.memoryLimitKb = 0;

			usedResources.cpuWorkTimeMs = 0;
			usedResources.peakMemoryUsageKb = 0;
			usedResources.processExitCode = 0;
			usedResources.realTimeUsageMs = 0;

			//free(program);
			//free(args);
			//free(workDirectory);
		}

		void SetProgram(const wchar_t * _program, const wchar_t * _args)
		{
			programSet = true;

			program = _wcsdup(_program);
			args = _wcsdup(_args);
		}

		void SetWorkDirectory(const wchar_t * _dir)
		{
			workDirSet = true;

			workDirectory = _wcsdup(_dir);
		}

		void SetRealTimeLimit(uint32 _timeMs)
		{
			realTimeLimitSet = true;

			limits.realTimeLimitMs = _timeMs;
		}

		void SetMemoryLimit(uint32 _memoryKb)
		{
			memoryLimitSet = true;

			limits.memoryLimitKb = _memoryKb;
		}

		bool RedirectIOHandleToFile(TestLib::IOHandleType _handleType, const wchar_t * _fileName)
		{
			if (_fileName == nullptr)
			{
				Internal::logger->Error(__FUNCTION__ L"ArgumentNullException " nameof(_fileName) "\n");

				throw "ArgumentNullException " nameof(_fileName);
			}

			SECURITY_ATTRIBUTES attr;
			attr.nLength = sizeof(SECURITY_ATTRIBUTES);
			attr.lpSecurityDescriptor = nullptr;
			attr.bInheritHandle = TRUE;

			DWORD rwMode;
			DWORD openMode;

			switch (_handleType)
			{
			case TestLib::IOHandleType::Input:
				rwMode = GENERIC_READ;
				openMode = OPEN_EXISTING;
				break;
			case TestLib::IOHandleType::Output:
			case TestLib::IOHandleType::Error:
				rwMode = GENERIC_WRITE;
				openMode = CREATE_ALWAYS;
				break;
				/*case RedirectFileHandleMode::Rewrite:
				rwMode = GENERIC_WRITE;
				openMode = CREATE_ALWAYS;
				break;
				*/
			default:
				Internal::logger->Error(__FUNCTION__ L"IOHandleType incorrect _handleType = %hhu\n",
					(uint8)_handleType);

				return false;
			}

			HANDLE h = CreateFileW(_fileName, rwMode, 0, &attr, openMode, FILE_ATTRIBUTE_NORMAL, nullptr);
			if (h == INVALID_HANDLE_VALUE)
			{
				Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. CreateFileW failed error code %lu. File name = %S\n",
					__LINE__, GetLastError(), _fileName);

				return false;
			}

			switch (_handleType)
			{
			case TestLib::IOHandleType::Input:
				SafeCloseHandle(&IoHandles.input);
				IoHandles.input = h;
				break;
			case TestLib::IOHandleType::Output:
				SafeCloseHandle(&IoHandles.output);
				IoHandles.output = h;
				break;
			case TestLib::IOHandleType::Error:
				SafeCloseHandle(&IoHandles.error);
				IoHandles.error = h;
				break;
			default:
				Internal::logger->Error(__FUNCTION__ L"IOHandleType incorrect _handleType = %hhu\n",
					(uint8)_handleType);
				return false;
			}

			return true;
		}

		bool RedirectIOHandleToHandle(TestLib::IOHandleType _handleType, void * _handle, bool _duplicate)
		{
			if (_handle == nullptr)
			{
				Internal::logger->Error(__FUNCTION__ L"ArgumentNullException " nameof(_handle) "\n");

				throw "ArgumentNullException " nameof(_handle);
			}

			PHANDLE h;

			switch (_handleType)
			{
			case TestLib::IOHandleType::Input:
				SafeCloseHandle(&IoHandles.input);
				h = &IoHandles.input;
				break;
			case TestLib::IOHandleType::Output:
				SafeCloseHandle(&IoHandles.output);
				h = &IoHandles.output;
				break;
			case TestLib::IOHandleType::Error:
				SafeCloseHandle(&IoHandles.error);
				h = &IoHandles.error;
				break;
			default:
				Internal::logger->Error(__FUNCTION__ L"IOHandleType incorrect _handleType = %hhu\n",
					(uint8)_handleType);
				return false;

			}

			if (_duplicate)
			{
				if (!DuplicateHandle(GetCurrentProcess(), _handle, GetCurrentProcess(), h,
					0, TRUE, DUPLICATE_SAME_ACCESS))
				{
					Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. DuplicateHandle failed error code %lu\n",
						__LINE__, GetLastError());

					return false;
				}
			}
			else
			{
				(*h) = _handle;
			}

			return true;
		}

		void * GetIORedirectedHandle(TestLib::IOHandleType _handleType)
		{
			switch (_handleType)
			{
			case TestLib::IOHandleType::Input:
				return IoHandles.input;
			case TestLib::IOHandleType::Output:
				return IoHandles.output;
			case TestLib::IOHandleType::Error:
				return IoHandles.error;
			default:
				Internal::logger->Error(L"%hhd is incorrect _handleType", (uint8)_handleType);
				return nullptr;
			}
		}

		bool Run(bool useRestrictions = false);
		bool Wait();
		void CloseIoRedirectionHandles();

		uint32 GetRunResult()
		{
			return 0;
		}
		uint32 GetExitCode()
		{
			return usedResources.processExitCode;
		}

		TestLib::UsedResources GetUsedResources()
		{
			return usedResources;
		}

	private:
		bool applyMemoryLimit()
		{
			if (limits.memoryLimitKb <= 0)
			{
				Internal::logger->Warning(L"Can't set not positive memory limit. MemoryLimitKb = %lu\n",
					limits.memoryLimitKb);

				return false;
			}

			JOBOBJECT_EXTENDED_LIMIT_INFORMATION jobExtendedLimits = { 0 };

			if (!QueryInformationJobObject(startupHandles.job, JobObjectExtendedLimitInformation,
				&jobExtendedLimits, sizeof(jobExtendedLimits), nullptr))
			{
				Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. QueryInformationJobObject failed error code %lu\n",
					__LINE__, GetLastError());

				return false;
			}

			jobExtendedLimits.BasicLimitInformation.LimitFlags |= JOB_OBJECT_LIMIT_PROCESS_MEMORY;
			jobExtendedLimits.ProcessMemoryLimit = static_cast<SIZE_T>(1.5 * limits.memoryLimitKb * 1024); // 1.5X reserve;

			if (!SetInformationJobObject(startupHandles.job, JobObjectExtendedLimitInformation,
				&jobExtendedLimits, sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION)))
			{
				Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. SetInformationJobObject failed error code %lu. Memory limit = %lu\n",
					__LINE__, GetLastError(), limits.memoryLimitKb);

				return false;
			}

			return true;
		}
		bool applyMandatoryLevel(HANDLE hProcessCreationToken)
		{
			if (!SetTokenIntegrityLevel(hProcessCreationToken, SECURITY_MANDATORY_LOW_RID))// SECURITY_MANDATORY_UNTRUSTED_RID))
			{
				Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. Can't set container process creation token security info. SetTokenIntegrityLevel failed error code %lu\n",
					__LINE__, GetLastError());

				SafeCloseHandle(&hProcessCreationToken);

				return false;
			}

			return true;
		}
		bool applyStartupAttribute(LPSTARTUPINFOEXW _startupInfoEx)
		{
			SIZE_T attributeListSize = 0;
			InitializeProcThreadAttributeList(nullptr, 2, 0, &attributeListSize);

			_startupInfoEx->lpAttributeList = (LPPROC_THREAD_ATTRIBUTE_LIST)malloc(attributeListSize);

			if (!InitializeProcThreadAttributeList(_startupInfoEx->lpAttributeList, 2, 0, &attributeListSize))
			{
				Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. InitializeProcThreadAttributeList failed error code %lu\n",
					__LINE__, GetLastError());

				return false;
			}

			DWORD64 mitigationPolicy =
				PROCESS_CREATION_MITIGATION_POLICY_DEP_ENABLE |
				PROCESS_CREATION_MITIGATION_POLICY_DEP_ATL_THUNK_ENABLE |
				PROCESS_CREATION_MITIGATION_POLICY_SEHOP_ENABLE |
				PROCESS_CREATION_MITIGATION_POLICY_HEAP_TERMINATE_ALWAYS_ON |
				PROCESS_CREATION_MITIGATION_POLICY_BOTTOM_UP_ASLR_ALWAYS_ON |
				PROCESS_CREATION_MITIGATION_POLICY_HIGH_ENTROPY_ASLR_ALWAYS_ON |
				PROCESS_CREATION_MITIGATION_POLICY_STRICT_HANDLE_CHECKS_ALWAYS_ON |
				PROCESS_CREATION_MITIGATION_POLICY_WIN32K_SYSTEM_CALL_DISABLE_ALWAYS_ON |
				PROCESS_CREATION_MITIGATION_POLICY_EXTENSION_POINT_DISABLE_ALWAYS_ON;
#ifdef _WIN32_WINNT_WIN10
			mitigationPolicy |=
				PROCESS_CREATION_MITIGATION_POLICY_FONT_DISABLE_ALWAYS_ON |
				PROCESS_CREATION_MITIGATION_POLICY_PROHIBIT_DYNAMIC_CODE_ALWAYS_ON;
#endif

			if (!UpdateProcThreadAttribute(_startupInfoEx->lpAttributeList, 0, PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY,
				&mitigationPolicy, sizeof(mitigationPolicy), nullptr, nullptr))
			{
				Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. UpdateProcThreadAttribute for PROC_THREAD_ATTRIBUTE_MITIGATION_POLICY failed error code %lu\n",
					__LINE__, GetLastError());

				DeleteProcThreadAttributeList(_startupInfoEx->lpAttributeList);
				free(_startupInfoEx->lpAttributeList);

				return false;
			}

#ifdef _WIN32_WINNT_WIN10
			DWORD childProcessPolicy = PROCESS_CREATION_CHILD_PROCESS_RESTRICTED;
			if (!UpdateProcThreadAttribute(_startupInfoEx->lpAttributeList, 0, PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY,
				&childProcessPolicy, sizeof(childProcessPolicy), nullptr, nullptr))
			{
				Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. UpdateProcThreadAttribute for PROC_THREAD_ATTRIBUTE_CHILD_PROCESS_POLICY failed error code %lu\n",
					__LINE__, GetLastError());

				DeleteProcThreadAttributeList(_startupInfoEx->lpAttributeList);
				free(_startupInfoEx->lpAttributeList);

				return false;
			}
#endif

			return true;
		}
		bool applyUIRestrictions()
		{
			JOBOBJECT_BASIC_UI_RESTRICTIONS jobUILimits = { 0 };

			DWORD restrictionsClass =
				JOB_OBJECT_UILIMIT_EXITWINDOWS |
				JOB_OBJECT_UILIMIT_DESKTOP |
				JOB_OBJECT_UILIMIT_GLOBALATOMS |
				JOB_OBJECT_UILIMIT_DISPLAYSETTINGS |
				JOB_OBJECT_UILIMIT_SYSTEMPARAMETERS |
				JOB_OBJECT_UILIMIT_WRITECLIPBOARD |
				JOB_OBJECT_UILIMIT_READCLIPBOARD |
				JOB_OBJECT_UILIMIT_HANDLES |
				JOB_OBJECT_UILIMIT_ALL;

			jobUILimits.UIRestrictionsClass = restrictionsClass;
			if (!SetInformationJobObject(startupHandles.job, JobObjectBasicUIRestrictions,
				&jobUILimits, sizeof(JOBOBJECT_BASIC_UI_RESTRICTIONS)))
			{
				Internal::logger->Error(L"WinAPI error in " __FUNCTION__ " at line %d. Can't set UI restrinctions. SetInformationJobObject failed error code %lu\n",
					__LINE__, GetLastError());

				return false;
			}

			return true;
		}

		bool programSet;
		bool realTimeLimitSet;
		bool memoryLimitSet;
		bool workDirSet;
		wchar_t * program;
		wchar_t * args;
		wchar_t * workDirectory;

		DWORD startTime;
		TestLib::UsedResources usedResources;

		struct
		{
			HANDLE input;
			HANDLE output;
			HANDLE error;
		} IoHandles;

		struct
		{
			HANDLE process;
			HANDLE thread;
			HANDLE job;
		} startupHandles;

		struct
		{
			uint32 realTimeLimitMs;
			uint32 memoryLimitKb;
		} limits;
	};
}