@echo off
chcp 65001 > nul
echo Запуск Анти-кафе...
cd /d "%~dp0publish"
start Anticafe.exe
exit