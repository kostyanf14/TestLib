#pragma once

#include <Windows.h>

struct LocalAllocatorDeleter
{
	void operator()(void* memory)
	{
		LocalFree(memory);
	}
};
