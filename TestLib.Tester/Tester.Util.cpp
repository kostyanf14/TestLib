#include "Tester.h"

namespace Internal
{
	HANDLE DuplicateCurrentProcessToken()
	{
		HANDLE hCurrentProcessToken = nullptr;
		HANDLE hDuplicatedToken = nullptr;

		OpenProcessToken(GetCurrentProcess(), TOKEN_DUPLICATE | TOKEN_ADJUST_DEFAULT |
			TOKEN_QUERY | TOKEN_ASSIGN_PRIMARY, &hCurrentProcessToken);
		DuplicateTokenEx(hCurrentProcessToken, 0, nullptr,
			SecurityImpersonation, TokenPrimary, &hDuplicatedToken);
		SafeCloseHandle(&hCurrentProcessToken);

		return hDuplicatedToken;
	}
	bool SetTokenIntegrityLevel(HANDLE hToken, DWORD mandatoryLabelAuthority)
	{
		PSID integritySID = nullptr;
		SID_IDENTIFIER_AUTHORITY authority = SECURITY_MANDATORY_LABEL_AUTHORITY;
		AllocateAndInitializeSid(&authority, 1, mandatoryLabelAuthority,
			0, 0, 0, 0, 0, 0, 0, &integritySID);

		TOKEN_MANDATORY_LABEL mandatoryLabel = {};
		mandatoryLabel.Label.Attributes = SE_GROUP_INTEGRITY;
		mandatoryLabel.Label.Sid = integritySID;
		BOOL result = SetTokenInformation(hToken, TokenIntegrityLevel,
			&mandatoryLabel, sizeof(mandatoryLabel));
		FreeSid(integritySID);

		if (!result)
		{
			//log->LogErrorLastSystemError(L"in " __FUNCTION__);

			return false;
		}
		return true;
	}
	bool SetProcessIntegrityLevel(HANDLE hProcess, DWORD mandatoryLabelAuthority)
	{
		HANDLE hToken = nullptr;
		if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE | TOKEN_ADJUST_DEFAULT |
			TOKEN_QUERY | TOKEN_ASSIGN_PRIMARY, &hToken))
		{
			//log->LogErrorLastSystemError(L"in " __FUNCTION__);

			return false;
		}

		bool result = SetTokenIntegrityLevel(hToken, mandatoryLabelAuthority);
		SafeCloseHandle(&hToken);
		return result;
	}
}