@echo off
title Build Anticafe
echo ========================================
echo    BUILDING ANTICAFE EXE
echo ========================================
echo.

cd frontend
echo [1/3] Installing frontend...
call npm install
if errorlevel 1 goto error

echo [2/3] Building frontend...
call npm run build
if errorlevel 1 goto error

cd ..
echo [3/3] Copying files...
if not exist "backend\wwwroot" mkdir "backend\wwwroot"
xcopy /E /I /Y "frontend\dist\*" "backend\wwwroot\" > nul

cd backend
echo [4/4] Building EXE (wait 2-3 min)...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=none -o ../publish

if errorlevel 1 goto error

cd ..
echo.
echo ========================================
echo    SUCCESS!
echo ========================================
echo EXE created: publish\AnticafeBackend.exe
echo.
pause
exit /b 0

:error
echo.
echo ========================================
echo    ERROR!
echo ========================================
pause
exit /b 1