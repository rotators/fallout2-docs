@echo off
setlocal

pushd "%~dp0"
if errorlevel 1 exit /b %errorlevel%

dotnet build tools\FoDocs.Generator\FoDocs.Generator.csproj
if errorlevel 1 goto :fail

dotnet run --project tools\FoDocs.Generator -- --clean
if errorlevel 1 goto :fail

powershell -NoProfile -ExecutionPolicy Bypass -File tools\Validate-Site.ps1
if errorlevel 1 goto :fail

popd
exit /b 0

:fail
set EXITCODE=%errorlevel%
popd
exit /b %EXITCODE%
