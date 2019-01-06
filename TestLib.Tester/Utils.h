#pragma once
#include <memory>
#include <Windows.h>
#include "TypesHepler.h"

using UniqueLocalStringType = std::unique_ptr<wchar_t, LocalAllocatorDeleter>;

void SafeCloseHandle(PHANDLE _h);
UniqueLocalStringType GetErrorMessage(DWORD error = GetLastError());