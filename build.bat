@echo off
chcp 65001 > nul
title Сборка Анти-кафе

echo ========================================
echo    🍵 СБОРКА АНТИ-КАФЕ
echo ========================================
echo.

echo [0/4] Завершение предыдущих процессов...
taskkill /f /im Anticafe.exe 2>nul
timeout /t 1 /nobreak > nul

echo [1/4] Очистка старых файлов...
if exist publish rmdir /s /q publish
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo [2/4] Восстановление зависимостей...
dotnet restore
if errorlevel 1 goto error

echo [3/4] Сборка проекта...
dotnet build -c Release --no-restore
if errorlevel 1 goto error

echo [4/4] Создание EXE файла...
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
echo    ✅ СБОРКА ЗАВЕРШЕНА!
echo ========================================
echo 📁 Готовый файл: publish\Anticafe.exe
echo.
echo 🚀 Для запуска выполните start.bat
echo ========================================
pause
exit /b 0

:error
echo.
echo ========================================
echo    ❌ ОШИБКА СБОРКИ!
echo ========================================
pause
exit /b 1