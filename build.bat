@echo off
chcp 65001 > nul
title Anticafe Build

echo ========================================
echo    ANTICAFE BUILD
echo ========================================
echo.

echo [0/4] Killing previous processes...
taskkill /f /im Anticafe.exe 2>nul
timeout /t 1 /nobreak > nul

echo [1/4] Cleaning old files...
if exist publish rmdir /s /q publish
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo [2/4] Restoring dependencies...
dotnet restore
if errorlevel 1 goto error

echo [3/4] Building project...
dotnet build -c Release --no-restore
if errorlevel 1 goto error

echo [4/4] Creating EXE file...
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:DebugType=none ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:CopyOutputSymbolsToPublishDirectory=false ^
    -o publish
if errorlevel 1 goto error

echo.
echo ========================================
echo    BUILD COMPLETE!
echo ========================================
echo File: publish\Anticafe.exe
echo.
echo Run start.bat to launch
echo ========================================
pause
exit /b 0

:error
echo.
echo ========================================
echo    BUILD ERROR!
echo ========================================
pause
exit /b 1
