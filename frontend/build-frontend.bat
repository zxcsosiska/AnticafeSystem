@echo off
cd frontend
echo Сборка фронтенда...
call npm install
call npm run build
echo Копируем в backend/wwwroot...
xcopy /E /I /Y dist\* ..\backend\wwwroot\
echo Готово!
pause