@echo off

set CURRENT_FOLDER=%cd%
set RELEASE_FOLDER=%CURRENT_FOLDER%\release
set PROJECT_FOLDER=%CURRENT_FOLDER%\src\WOWCAM\WOWCAM
set PUBLISH_FOLDER=%PROJECT_FOLDER%\bin\Release\net8.0-windows\win-x64\publish

cls
echo. && echo WOWCAM build script 1.0.5 (by MBODM 02/2025) && echo.
echo Will perform the following 5 steps now: && echo.
echo 1) dotnet clean && echo 2) dotnet build && echo 3) dotnet publish
echo 4) copy executable && echo 5) copy installer && echo.

cd "%PROJECT_FOLDER%"
dotnet clean -v quiet && dotnet build --no-incremental -c Release -v quiet && dotnet publish -c Release -v quiet

cd "%CURRENT_FOLDER%"
if not exist "%RELEASE_FOLDER%" mkdir "%RELEASE_FOLDER%"
copy /B /V /Y "%PUBLISH_FOLDER%\WOWCAM.exe" "%RELEASE_FOLDER%" >NUL
copy /B /V /Y "%CURRENT_FOLDER%\installer\Install.bat" "%RELEASE_FOLDER%" >NUL

echo Finished (you can now deploy everything inside 'release' folder)
echo.
echo Have a nice day.

REM Show timeout when started via double click
REM From https://stackoverflow.com/questions/5859854/detect-if-bat-file-is-running-via-double-click-or-from-cmd-window
if /I %0 EQU "%~dpnx0" timeout /T 9
