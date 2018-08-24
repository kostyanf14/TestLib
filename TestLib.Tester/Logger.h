#pragma once
#include "Types.h"
#include <stdarg.h>
#include <stdio.h>

namespace Internal
{
	using LogCallback = void(*)(class Logger & log, const uint8 level, const wchar_t * message, void * userData);

	class Logger
	{
	public:
		Logger() : log(nullptr), userData(nullptr) { }

		void SetCallback(LogCallback _log, void * _userData);

		void Debug(wchar_t * format, ...);
		void Info(wchar_t * format, ...);
		void Warning(wchar_t * format, ...);
		void Error(wchar_t * format, ...);

	private:
		static const int buffer_size = 4096;
		LogCallback log;
		void * userData;
	};

	extern Logger * logger;
}