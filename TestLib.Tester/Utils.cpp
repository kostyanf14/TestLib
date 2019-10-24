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

BOOL TerminateProcessS(HANDLE hProcess, UINT uExitCode)
{
	UINT _2Minutes = 2 * 60 * 1000;
	TerminateProcess(hProcess, uExitCode);

	DWORD waitCode = WaitForSingleObject(hProcess, _2Minutes);
	switch (waitCode)
	{
	case WAIT_TIMEOUT:
	case WAIT_FAILED:
		return FALSE;

	case WAIT_OBJECT_0:
		return TRUE;
	default:
		return FALSE;
	}

}

UniqueLocalStringType GetErrorMessage(DWORD error)
{
	wchar_t * buffer;

	FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
		nullptr, error, 0, (wchar_t *)&buffer, 0, nullptr);

	return UniqueLocalStringType(buffer);
}