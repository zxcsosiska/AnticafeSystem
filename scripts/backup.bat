@echo off
set BACKUP_DIR=C:\AnticafeSystem\backups
set DB_PATH=C:\AnticafeSystem\database\anticafe.db
set DATE=%date:~0,4%%date:~5,2%%date:~8,2%

if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

copy "%DB_PATH%" "%BACKUP_DIR%\anticafe_backup_%DATE%.db"
echo Резервная копия создана: %BACKUP_DIR%\anticafe_backup_%DATE%.db
pause