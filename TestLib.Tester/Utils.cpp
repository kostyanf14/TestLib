#include <Windows.h>

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