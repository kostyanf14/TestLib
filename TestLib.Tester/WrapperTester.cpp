#include "Tester.h"
#include "Logger.h"
#include <vcclr.h>  
#include <msclr/gcroot.h>
#include <assert.h>

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Runtime::CompilerServices;

namespace TestLib
{
	class NativeLogCallbackHandler
	{
	public:
		NativeLogCallbackHandler(ref class LoggerManaged^ _owner);
	private:
		static void OnLog(Internal::Logger & log, const uint8 level, const wchar_t * message, void * userData);
		msclr::gcroot<LoggerManaged^> owner;
	};

	public ref class LoggerManaged
	{
	public:
		enum class LogLevel : uint8
		{
			Trace,
			Debug,
			Info,
			Warn,
			Error,
			Fatal,
			Off,
		};

		delegate void LogEventHandler(ref class LoggerManaged^ log, LogLevel level, String^ message);

		LoggerManaged() :
			nativeLogger(new Internal::Logger()),
			nativeHandler(new NativeLogCallbackHandler(this))
		{ }

		~LoggerManaged()
		{
			this->!LoggerManaged();
		}

		!LoggerManaged()
		{
			delete nativeLogger;
			nativeLogger = nullptr;

			delete nativeHandler;
			nativeHandler = nullptr;
		}

		void Destroy()
		{
			delete nativeLogger;
			nativeLogger = nullptr;

			delete nativeHandler;
			nativeHandler = nullptr;
		}

		void InitNativeLogger(LogEventHandler ^ _logger)
		{
			this->Log += _logger;

			Internal::logger = this->GetNative();
			Internal::logger->Debug(L"Check native log from " __FUNCTIONW__);
		}

		event LogEventHandler^ Log
		{
			[MethodImplAttribute(MethodImplOptions::Synchronized)]
			void add(LogEventHandler^ value) {
				managedHandler = safe_cast<LogEventHandler^>(Delegate::Combine(value, managedHandler));
			}

			[MethodImplAttribute(MethodImplOptions::Synchronized)]
			void remove(LogEventHandler^ value) {
				managedHandler = safe_cast<LogEventHandler^>(Delegate::Remove(value, managedHandler));
			}

		private:
			void raise(LoggerManaged^ log, LogLevel level, String^ message)
			{
				if (managedHandler != nullptr)
					managedHandler(this, level, message);
			}
		}

	internal:
		void RaiseLogEvent(Byte level, String^ message)
		{
			Log(this, (LogLevel)level, message);
		}
		void RaiseLogEvent(LogLevel level, String^ message)
		{
			Log(this, level, message);
		}

		Internal::Logger * GetNative() { return nativeLogger; }

	private:
		Internal::Logger * nativeLogger;
		NativeLogCallbackHandler * nativeHandler;
		LogEventHandler^ managedHandler;
	};

	public ref class Tester
	{
	public:
		Tester()
		{
			tester = new Internal::Tester();
		}

		void SetProgram(String ^ _program, String ^ _args)
		{
			using namespace Runtime::InteropServices;

			const wchar_t* program = (const wchar_t*)(Marshal::StringToHGlobalUni(_program)).ToPointer();
			const wchar_t* args = (const wchar_t*)(Marshal::StringToHGlobalUni(_args)).ToPointer();

			tester->SetProgram(program, args);

			Marshal::FreeHGlobal(IntPtr((void*)program));
			Marshal::FreeHGlobal(IntPtr((void*)args));
		}

		void SetWorkDirectory(String ^ _dir)
		{
			using namespace Runtime::InteropServices;

			const wchar_t* dir = (const wchar_t*)(Marshal::StringToHGlobalUni(_dir)).ToPointer();
			tester->SetWorkDirectory(dir);

			Marshal::FreeHGlobal(IntPtr((void*)dir));
		}

		void SetRealTimeLimit(UInt32 _timeMs)
		{
			tester->SetRealTimeLimit(_timeMs);
		}

		void SetMemoryLimit(UInt32 _memoryKb)
		{
			tester->SetMemoryLimit(_memoryKb);
		}

		void RedirectIOHandleToFile(IOHandleType _handleType, String ^ _fileName)
		{
			if (_fileName == nullptr)
				throw gcnew ArgumentNullException(nameof(_fileName));

			using namespace Runtime::InteropServices;

			const wchar_t* fileName = (const wchar_t*)(Marshal::StringToHGlobalUni(_fileName)).ToPointer();

			tester->RedirectIOHandleToFile(_handleType, fileName);

			Marshal::FreeHGlobal(IntPtr((void*)fileName));
		}

		Boolean RedirectIOHandleToHandle(IOHandleType _handleType, void * _handle, bool _duplicate)
		{
			if (_handle == nullptr)
				throw gcnew ArgumentNullException(nameof(_handle));

			return tester->RedirectIOHandleToHandle(_handleType, _handle, _duplicate);
		}
		Boolean RedirectIOHandleToHandle(IOHandleType _handleType, void * _handle)
		{
			return RedirectIOHandleToHandle(_handleType, _handle, false);
		}
		Boolean RedirectIOHandleToHandle(IOHandleType _handleType, IntPtr ^ _handle, bool _duplicate)
		{
			return RedirectIOHandleToHandle(_handleType, _handle->ToPointer(), _duplicate);
		}
		Boolean RedirectIOHandleToHandle(IOHandleType _handleType, IntPtr ^ _handle)
		{
			return RedirectIOHandleToHandle(_handleType, _handle, false);
		}

		IntPtr ^ GetIORedirectedHandle(IOHandleType _handleType)
		{
			return gcnew IntPtr(tester->GetIORedirectedHandle(_handleType));
		}

		Boolean Run(Boolean _useRestrictions)
		{
			return tester->Run(_useRestrictions);
		}
		Boolean Run()
		{
			return tester->Run();
		}

		Boolean Wait()
		{
			return tester->Wait();
		}

		void CloseIoRedirectionHandles()
		{
			tester->CloseIoRedirectionHandles();
		}

		UInt32 GetRunResult()
		{
			return tester->GetRunResult();
		}
		UInt32 GetExitCode()
		{
			return tester->GetExitCode();
		}

		UsedResources GetUsedResources()
		{
			return tester->GetUsedResources();
		}

		void Destroy()
		{
			delete tester;
		}

	private:
		Internal::Tester * tester;

		static LoggerManaged ^ loggerManaged;
	};
	inline NativeLogCallbackHandler::NativeLogCallbackHandler(LoggerManaged ^ _owner) : owner(_owner)
	{
		owner->GetNative()->SetCallback(&OnLog, this);
	}
	inline void NativeLogCallbackHandler::OnLog(Internal::Logger & log, const uint8 level, const wchar_t * message, void * userData)
	{
		static_cast<NativeLogCallbackHandler*>(userData)->owner->RaiseLogEvent(level, gcnew String(message));
	}
}
