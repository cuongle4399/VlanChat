@echo off
title Vlan Chat Pro - Native AOT Builder
color 0b
echo ==========================================================
echo       BIEN DICH NATIVE AOT - VLAN CHAT PRO
echo ==========================================================
echo.
echo Dang kiem tra va bien dich du an sang Native AOT...
echo Yeu cau: May tinh can cai dat Visual Studio voi "Desktop development with C++"
echo.
dotnet publish -c Release -r win-x64 --self-contained
if %errorlevel% neq 0 (
    color 0c
    echo.
    echo [LOI] Bien dich Native AOT that bai!
    echo Vui long kiem tra xem ban da cai dat C++ Build Tools ^(Desktop development with C++ in VS^) chua.
    echo.
    pause
    exit /b %errorlevel%
)
color 0a
echo.
echo ==========================================================
echo [THANH CONG] Da bien dich Native AOT thanh cong!
echo File thuc thi nam tai thu muc:
echo bin\Release\net8.0-windows\win-x64\publish\LANChatPro.exe
echo ==========================================================
echo.
pause
