@echo off
setlocal EnableDelayedExpansion

set "ROOT=%~dp0"
set "BUILD_DIR=%ROOT%Build"
set "CONFIG=Release"
set "SLN=%ROOT%NetworkSignalCore.sln"

echo.
echo ============================================================
echo  NetworkSignalCore - Build Script
echo ============================================================
echo.

REM ----------------------------------------------------------------
REM 1. Clean
REM ----------------------------------------------------------------
echo [1/6] Cleaning previous build...
if exist "%BUILD_DIR%" (
    rmdir /s /q "%BUILD_DIR%"
    if !ERRORLEVEL! neq 0 (
        echo [ERROR] Could not remove "%BUILD_DIR%". Is it open somewhere?
        exit /b 1
    )
)

mkdir "%BUILD_DIR%\Core"           || goto :error
mkdir "%BUILD_DIR%\Server"         || goto :error
mkdir "%BUILD_DIR%\Client"         || goto :error
mkdir "%BUILD_DIR%\Unity\Plugins"  || goto :error
mkdir "%BUILD_DIR%\Unity\Scripts\Core"       || goto :error
mkdir "%BUILD_DIR%\Unity\Scripts\Components" || goto :error
echo       OK

REM ----------------------------------------------------------------
REM 2. Restore NuGet packages
REM ----------------------------------------------------------------
echo [2/6] Restoring NuGet packages...
dotnet restore "%SLN%" --verbosity quiet
if %ERRORLEVEL% neq 0 goto :error
echo       OK

REM ----------------------------------------------------------------
REM 3. Core  (netstandard2.1)
REM    CopyLocalLockFileAssemblies=true forces all NuGet deps to output
REM ----------------------------------------------------------------
echo [3/6] Building NetworkSignalCore.Core  (netstandard2.1)...
dotnet build "%ROOT%src\NetworkSignalCore.Core\NetworkSignalCore.Core.csproj" ^
    --configuration %CONFIG% ^
    --output "%BUILD_DIR%\Core" ^
    --no-restore ^
    --verbosity quiet ^
    /p:CopyLocalLockFileAssemblies=true
if %ERRORLEVEL% neq 0 goto :error
echo       OK

REM ----------------------------------------------------------------
REM 4. Server  (net8.0)
REM    Includes all ASP.NET Core + SignalR + JWT dependencies
REM ----------------------------------------------------------------
echo [4/6] Building NetworkSignalCore.Server  (net8.0)...
dotnet build "%ROOT%src\NetworkSignalCore.Server\NetworkSignalCore.Server.csproj" ^
    --configuration %CONFIG% ^
    --output "%BUILD_DIR%\Server" ^
    --no-restore ^
    --verbosity quiet ^
    /p:CopyLocalLockFileAssemblies=true
if %ERRORLEVEL% neq 0 goto :error
echo       OK

REM ----------------------------------------------------------------
REM 5. Client  (netstandard2.1)
REM    Includes SignalR.Client + Logging deps
REM ----------------------------------------------------------------
echo [5/6] Building NetworkSignalCore.Client  (netstandard2.1)...
dotnet build "%ROOT%src\NetworkSignalCore.Client\NetworkSignalCore.Client.csproj" ^
    --configuration %CONFIG% ^
    --output "%BUILD_DIR%\Client" ^
    --no-restore ^
    --verbosity quiet ^
    /p:CopyLocalLockFileAssemblies=true
if %ERRORLEVEL% neq 0 goto :error
echo       OK

REM ----------------------------------------------------------------
REM 6. Unity package
REM    Plugins\  - all Client DLLs (drop into Assets/Plugins/)
REM    Scripts\  - MonoBehaviour source files (drop into Assets/)
REM ----------------------------------------------------------------
echo [6/6] Preparing Unity package...

REM Copy all Client DLLs (and their deps) -> Unity/Plugins/
xcopy "%BUILD_DIR%\Client\*.dll" "%BUILD_DIR%\Unity\Plugins\" /Y /Q
if %ERRORLEVEL% neq 0 goto :error

REM Copy .pdb for debugging (optional, users can delete them)
xcopy "%BUILD_DIR%\Client\*.pdb" "%BUILD_DIR%\Unity\Plugins\" /Y /Q 2>nul

REM Copy Unity MonoBehaviour source files -> Unity/Scripts/
xcopy "%ROOT%unity\NetworkSignalCore.Unity\Core\*.cs" ^
      "%BUILD_DIR%\Unity\Scripts\Core\" /Y /Q
xcopy "%ROOT%unity\NetworkSignalCore.Unity\Components\*.cs" ^
      "%BUILD_DIR%\Unity\Scripts\Components\" /Y /Q
echo       OK

REM ----------------------------------------------------------------
REM Done
REM ----------------------------------------------------------------
echo.
echo ============================================================
echo  Build SUCCESSFUL  ^|  Configuration: %CONFIG%
echo ============================================================
echo.
echo   Build\Core\
echo     NetworkSignalCore.Core.dll  +  dependencies (netstandard2.1)
echo.
echo   Build\Server\
echo     NetworkSignalCore.Server.dll  +  all dependencies (net8.0)
echo     Reference this in your ASP.NET Core server project.
echo.
echo   Build\Client\
echo     NetworkSignalCore.Client.dll  +  all dependencies (netstandard2.1)
echo     Reference this in a plain .NET client (console, WPF, etc).
echo.
echo   Build\Unity\
echo     Plugins\   -  drop the whole folder into Assets/Plugins/
echo     Scripts\   -  drop the whole folder into Assets/
echo.
goto :eof

:error
echo.
echo ============================================================
echo  Build FAILED  (exit code %ERRORLEVEL%)
echo ============================================================
exit /b 1
