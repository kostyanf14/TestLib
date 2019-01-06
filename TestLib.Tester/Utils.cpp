#include "Utils.h"

void SafeCloseHandle(PHANDLE _h)
{
	if (*_h == INVALID_HANDLE_VALUE)
		return;

	if (*_h == nullptr)
	{
		*_h = INVALID_HANDLE_VALUE;
		return;
	}

	CloseHandle(*_h);

	*_h = INVALID_HANDLE_VALUE;
}

UniqueLocalStringType GetErrorMessage(DWORD error)
{
	wchar_t * buffer;

	FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
		nullptr, error, 0, (wchar_t *)&buffer, 0, nullptr);

	return UniqueLocalStringType(buffer);
}