#include "Logger.h"

namespace Internal
{
	Logger * logger = nullptr;;

	void Logger::SetCallback(LogCallback _log, void * _userData)
	{
		log = _log;
		userData = _userData;

		Debug(L"Check native log from " __FUNCTIONW__);
	}


/*
varargs not supported under /clr
internal logger calls only at native code
*/
#pragma warning(disable: 4793)
	void Logger::Debug(wchar_t * format, ...)
	{
		if (!log)
			return;

		wchar_t buffer[buffer_size] = { 0 };

		va_list args;
		va_start(args, format);
		vswprintf_s(buffer, format, args);
		va_end(args);


		log(*this, 1u, buffer, userData);
	}

	void Logger::Info(wchar_t * format, ...)
	{
		if (!log)
			return;

		wchar_t buffer[buffer_size] = { 0 };

		va_list args;
		va_start(args, format);
		vswprintf_s(buffer, format, args);
		va_end(args);


		log(*this, 2u, buffer, userData);
	}

	void Logger::Warning(wchar_t * format, ...)
	{
		if (!log)
			return;

		wchar_t buffer[buffer_size] = { 0 };

		va_list args;
		va_start(args, format);
		vswprintf_s(buffer, format, args);
		va_end(args);

		log(*this, 3u, buffer, userData);
	}

	void Logger::Error(wchar_t * format, ...)
	{
		if (!log)
			return;

		wchar_t buffer[buffer_size] = { 0 };

		va_list args;
		va_start(args, format);
		vswprintf_s(buffer, format, args);
		va_end(args);

		log(*this, 4u, buffer, userData);
	}
#pragma warning(default: 4793) 
}