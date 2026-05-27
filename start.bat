@echo off
cd /d "%~dp0publish"
start AnticafeBackend.exe
timeout /t 3 /nobreak > nul
start http://localhost:5154
exit