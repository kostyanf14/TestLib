@echo off

SETLOCAL EnableExtensions EnableDelayedExpansion

set _ARTIFACTS_DIR_="%~dp0\artifacts"

if not exist "!_ARTIFACTS_DIR_!" mkdir "!_ARTIFACTS_DIR_!"

call :nuget "%%~dp0\TestLib.sln"
if !ERRORLEVEL! NEQ 0 exit /B 1

call :build "%%~dp0\TestLib.Worker.Updater" "TestLib.Worker.Updater.csproj" "Release" "AnyCPU"
if !ERRORLEVEL! NEQ 0 exit /B 1
call :build "%%~dp0\TestLib.UpdateServer" "TestLib.UpdateServer.csproj" "Release" "AnyCPU"
if !ERRORLEVEL! NEQ 0 exit /B 1
call :build "%%~dp0\TestLib.WorkerService" "TestLib.WorkerService.csproj" "Release" "x64"
if !ERRORLEVEL! NEQ 0 exit /B 1
call :build "%%~dp0\TestLib.WorkerService" "TestLib.WorkerService.csproj" "Release" "x86"
if !ERRORLEVEL! NEQ 0 exit /B 1

exit 0

:build
echo "Building: %~1\%~2 (%~3, %~4)"
call "MSBuild.exe" "%~1\%~2" "/p:Configuration=%~3" "/p:Platform=%~4"
exit /B %ERRORLEVEL%

:nuget
echo "Restoring NuGet packages: %~1"
call "NuGet.exe" "restore" %~1
exit /B %ERRORLEVEL%
