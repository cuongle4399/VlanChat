@echo off
:: Enable UTF-8 encoding for nice console print
chcp 65001 > nul
title LAN Chat Pro - Native AOT Builder & DPI Optimizer

echo ======================================================================
echo             LAN CHAT PRO - NATIVE AOT BUILDER & DPI OPTIMIZER
echo ======================================================================
echo.
echo [*] Trạng thái tối ưu hóa High-DPI: ĐÃ KÍCH HOẠT (PerMonitorV2 trong .csproj)
echo [*] Trạng thái Native AOT: ĐÃ KÍCH HOẠT (PublishAot=true trong .csproj)
echo.
echo [1] Bắt đầu dọn dẹp thư mục build cũ (dotnet clean)...
dotnet clean -c Release
if %errorlevel% neq 0 (
    echo [ERROR] Dọn dẹp thất bại! Vui lòng đóng các ứng dụng đang chạy và thử lại.
    pause
    exit /b %errorlevel%
)

echo.
echo [2] Đang tiến hành biên dịch Native AOT (Release, win-x64, Self-Contained)...
echo [*] Quá trình này có thể mất từ 1-3 phút do trình biên dịch C++ đang tối ưu mã máy...
dotnet publish -c Release -r win-x64 --self-contained
if %errorlevel% neq 0 (
    echo [ERROR] Biên dịch Native AOT thất bại! Vui lòng cài đặt Visual Studio C++ Build Tools.
    pause
    exit /b %errorlevel%
)

echo.
echo [3] Đang thu thập file cài đặt và tối ưu hóa thư mục đầu ra...
set SOURCE_DIR=bin\Release\net8.0-windows\win-x64\publish
set DEST_DIR=Build_AOT

if not exist "%DEST_DIR%" mkdir "%DEST_DIR%"

:: Sao chép file exe và các DLL bổ trợ cho đồ họa WinForms
copy /y "%SOURCE_DIR%\LANChatPro.exe" "%DEST_DIR%\" > nul
if exist "%SOURCE_DIR%\PenImc_cor3.dll" copy /y "%SOURCE_DIR%\PenImc_cor3.dll" "%DEST_DIR%\" > nul
if exist "%SOURCE_DIR%\wpfgfx_cor3.dll" copy /y "%SOURCE_DIR%\wpfgfx_cor3.dll" "%DEST_DIR%\" > nul
if exist "%SOURCE_DIR%\PresentationNative_cor3.dll" copy /y "%SOURCE_DIR%\PresentationNative_cor3.dll" "%DEST_DIR%\" > nul
if exist "%SOURCE_DIR%\vcruntime140_cor3.dll" copy /y "%SOURCE_DIR%\vcruntime140_cor3.dll" "%DEST_DIR%\" > nul

echo.
echo ======================================================================
echo                     BIÊN DỊCH NATIVE AOT THÀNH CÔNG!
echo ======================================================================
echo.
echo [+] File thực thi độc lập đã được xuất tại thư mục: "%DEST_DIR%\"
echo [+] File chính: "%DEST_DIR%\LANChatPro.exe"
echo [+] Ứng dụng đã được tối ưu hóa High DPI (màn hình 2K, 4K sắc nét).
echo.
set /p CHOICE="Bạn có muốn chạy thử ứng dụng Native AOT ngay bây giờ không? (Y/N): "
if /i "%CHOICE%"=="Y" (
    echo Đang khởi chạy LAN Chat Pro...
    start "" "%DEST_DIR%\LANChatPro.exe"
)
exit /b 0
