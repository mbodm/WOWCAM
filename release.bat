@echo off

set CURRENT_FOLDER=%cd%
set RELEASE_FOLDER=%CURRENT_FOLDER%\release
set PROJECT_FOLDER_1=%CURRENT_FOLDER%\src\WOWCAM\WOWCAM
set PUBLISH_FOLDER_1=%PROJECT_FOLDER_1%\bin\Release\net8.0-windows\win-x64\publish
set PROJECT_FOLDER_2=%CURRENT_FOLDER%\src\wcupdate\wcupdate
set PUBLISH_FOLDER_2=%PROJECT_FOLDER_2%\bin\Release\net8.0\win-x64\publish

cls
echo.
echo WOWCAM release script 1.0.0 (by MBODM 05/2024)
echo.
if not exist %RELEASE_FOLDER% mkdir %RELEASE_FOLDER%

cd %PROJECT_FOLDER_1%
dotnet publish -c Release
copy /B /V /Y %PUBLISH_FOLDER_1%\WOWCAM.exe %RELEASE_FOLDER%
echo.

cd %PROJECT_FOLDER_2%
dotnet publish -c Release
copy /B /V /Y %PUBLISH_FOLDER_2%\wcupdate.exe %RELEASE_FOLDER%
echo.

cd %CURRENT_FOLDER%
echo|set /p="Have a nice day."
echo.
timeout /T 5
