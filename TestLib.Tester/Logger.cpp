#include "Logger.h"

namespace Internal
{
	Logger * logger = nullptr;;

	void Logger::Debug(wchar_t * format, ...)
	{
		if (!log)
			return;

		wchar_t buffer[buffer_size] = { 0 };

		va_list args;
		va_start(args, format);
		swprintf_s(buffer, format, args);
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
		swprintf_s(buffer, format, args);
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
		swprintf_s(buffer, format, args);
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
		swprintf_s(buffer, format, args);
		va_end(args);

		log(*this, 4u, buffer, userData);
	}
}