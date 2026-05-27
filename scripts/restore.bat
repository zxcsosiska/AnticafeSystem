@echo off
echo Список доступных бэкапов:
dir C:\AnticafeSystem\backups\*.db
echo.
set /p FILE="Введите имя файла для восстановления: "
copy "C:\AnticafeSystem\backups\%FILE%" "C:\AnticafeSystem\database\anticafe.db"
echo Восстановление выполнено!
pause