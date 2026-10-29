@echo off
rem Запуск Math Survive (без редактора). Путь к Godot можно переопределить переменной GODOT.
rem Отладочные аргументы передаются после --, например: start.cmd -- --horde-now
setlocal
chcp 65001 >nul
cd /d "%~dp0"

if not defined GODOT set "GODOT=C:\w\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
if not exist "%GODOT%" (
  echo Не найден Godot: %GODOT%
  exit /b 1
)

set "PATH=%ProgramFiles%\dotnet;%PATH%"
dotnet build MathSurvive.sln -nologo -v:quiet
if errorlevel 1 (
  echo ОШИБКА СБОРКИ: игра не запущена
  exit /b 1
)

start "" "%GODOT%" --path "%~dp0." %*
