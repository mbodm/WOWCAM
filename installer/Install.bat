@echo off

set CURRENT_FOLDER=%cd%
set INSTALL_FOLDER=%LocalAppData%\Programs\WOWCAM

cls
echo. && echo WOWCAM Install 1.0.0 (by MBODM 01/2025) && echo.
echo - This batch script just copies the executable file to user's local programs directory
echo - This batch script does nothing else (therefore 'Install' is somewhat misleading here)
echo.

echo Copy...
echo.
if not exist "%INSTALL_FOLDER%" mkdir "%INSTALL_FOLDER%"
copy /B /V /Y "%CURRENT_FOLDER%\WOWCAM.exe" "%INSTALL_FOLDER%" >NUL

echo The 'WOWCAM.exe' was copied to '%INSTALL_FOLDER%'
echo.
echo Have a nice day.

REM Show timeout when started via double click
REM From https://stackoverflow.com/questions/5859854/detect-if-bat-file-is-running-via-double-click-or-from-cmd-window
if /I %0 EQU "%~dpnx0" echo. && pause && %SystemRoot%\explorer.exe "%INSTALL_FOLDER%"
