@echo off
echo Budowanie Release...
cd /d "%~dp0"
dotnet publish -c Release -r win-x64 --self-contained false -o "Release"
if %errorlevel% equ 0 (
    echo.
    echo Gotowe. Pliki w folderze Release
    echo Uruchom: Release\RedRat3ControllerWebServer.exe
    echo.
) else (
    echo Blad budowania.
)
pause
