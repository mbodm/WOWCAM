@echo off

set CURRENT_FOLDER=%cd%
set RELEASE_FOLDER=%CURRENT_FOLDER%\release
set PUBLISH_FOLDER=bin\Release\net8.0-windows\win-x64\publish

cls
echo.
echo WOWCAM release script 1.0.1 (by MBODM 09/2024)
if not exist %RELEASE_FOLDER% mkdir %RELEASE_FOLDER%

cd %CURRENT_FOLDER%\src\WOWCAM\WOWCAM
dotnet build --no-incremental -c Release -v quiet && dotnet publish -c Release -v quiet
echo.
copy /B /V /Y %PUBLISH_FOLDER%\WOWCAM.exe %RELEASE_FOLDER%

cd %CURRENT_FOLDER%\src\WOWCAMUPD\WOWCAMUPD
dotnet build --no-incremental -c Release -v quiet && dotnet publish -c Release -v quiet
echo.
copy /B /V /Y %PUBLISH_FOLDER%\WOWCAMUPD.exe %RELEASE_FOLDER%

echo.
cd %CURRENT_FOLDER%
echo Have a nice day.

REM Show timeout when started via double click
REM From https://stackoverflow.com/questions/5859854/detect-if-bat-file-is-running-via-double-click-or-from-cmd-window
if /I %0 EQU "%~dpnx0" timeout /T 5
