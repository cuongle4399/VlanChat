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
echo Dang bien dich Server sang Native AOT...
dotnet publish LANChatServer\LANChatServer.csproj -c Release -r win-x64 --self-contained
if %errorlevel% neq 0 (
    color 0c
    echo [LOI] Bien dich Server that bai!
    pause
    exit /b %errorlevel%
)

echo Dang bien dich Client (VlanChat) sang Native AOT...
dotnet publish VLANChatClient\LANChatPro.csproj -c Release -r win-x64 --self-contained
if %errorlevel% neq 0 (
    color 0c
    echo [LOI] Bien dich Client that bai!
    pause
    exit /b %errorlevel%
)

echo ==========================================================
echo [THANH CONG] Da bien dich Native AOT thanh cong ca 2 ung dung!
echo.
echo File Server nam tai:
echo LANChatServer\bin\Release\net8.0-windows\win-x64\publish\LANChatServer.exe
echo.
echo File Client nam tai:
echo VLANChatClient\bin\Release\net8.0-windows\win-x64\publish\LANChatPro.exe
echo ==========================================================
pause
