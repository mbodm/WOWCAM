@echo off

set CURRENT_FOLDER=%cd%
set PROJECT_FOLDER_1=%CURRENT_FOLDER%\src\WOWCAM\WOWCAM
set PUBLISH_FOLDER_1=%PROJECT_FOLDER_1%\bin\Release\net8.0-windows\win-x64\publish
set PROJECT_FOLDER_2=%CURRENT_FOLDER%\src\wcupdate\wcupdate
set PUBLISH_FOLDER_2=%PROJECT_FOLDER_2%\bin\Release\net8.0\win-x64\publish

cls
echo.
echo WOWCAM release script 1.0.0 (by MBODM 05/2024)

echo.
cd %PROJECT_FOLDER_1%
dotnet publish -c Release
cd %CURRENT_FOLDER%
copy /B /V /Y %PUBLISH_FOLDER_1%\WOWCAM.exe %CURRENT_FOLDER%

echo.
cd %PROJECT_FOLDER_2%
dotnet publish -c Release
cd %CURRENT_FOLDER%
copy /B /V /Y %PUBLISH_FOLDER_2%\wcupdate.exe %CURRENT_FOLDER%

echo.
echo|set /p="Have a nice day."
echo.
timeout /T 5
