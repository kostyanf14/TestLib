#pragma once

using uint8 = unsigned char;
using uint16 = unsigned short int;
using uint32 = unsigned long int;
using uint64 = unsigned long long int;
using sint8 = signed char;
using sint16 = signed short int;
using sint32 = signed long int;
using sint64 = signed long long int;
using float32 = float;
using float64 = double;

using uint = unsigned;
using sint = signed;

using byte = uint8;
using wchar = wchar_t;

#ifdef _WIN64
using uintptr = uint64;
using sintptr = sint64;
#else
using uintptr = __w64 uint32;
using sintptr = __w64 sint32;
#endif

static_assert(sizeof(uintptr) == sizeof(void*) && sizeof(sintptr) == sizeof(void*), "invalid uintptr/sintptr size");
