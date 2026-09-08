@echo off
setlocal
pushd "%~dp0"
if errorlevel 1 exit /b %errorlevel%
call gen.bat
if errorlevel 1 goto :fail
python tools\publish\publish.py --apply %*
if errorlevel 1 goto :fail
popd
exit /b 0
:fail
set EXITCODE=%errorlevel%
popd
exit /b %EXITCODE%
