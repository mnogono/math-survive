@echo off
rem Сборка Math Survive: C#-решение, тесты логики и экспорт игры в каталог build.
rem Использование: build.cmd [notest]
rem Путь к консольному Godot можно переопределить переменной GODOT_CON.
setlocal
chcp 65001 >nul
cd /d "%~dp0"

set "PATH=%ProgramFiles%\dotnet;%PATH%"
set "DOTNET=dotnet"
if not defined GODOT_CON set "GODOT_CON=C:\w\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
if not exist "%GODOT_CON%" (
  echo Не найден Godot: %GODOT_CON%
  goto :fail
)

echo === Сборка MathSurvive.sln ===
"%DOTNET%" build MathSurvive.sln -nologo -v:minimal || goto :fail

if /i "%~1"=="notest" goto :export

echo === Тесты логики ===
"%DOTNET%" test tests\MathSurvive.Core.Tests -nologo -v:minimal || goto :fail

:export
echo === Экспорт в build\MathSurvive.exe ===
if exist build rmdir /s /q build
mkdir build
"%GODOT_CON%" --headless --path . --export-release "Windows Desktop" build/MathSurvive.exe || goto :fail
if not exist build\MathSurvive.exe goto :fail

echo === Пакеты вопросов в build\packs ===
robocopy packs build\packs /e /xf .gdignore /njh /njs /nfl /ndl >nul
if errorlevel 8 goto :fail

echo === Готово: build\MathSurvive.exe ===
exit /b 0

:fail
echo === ОШИБКА СБОРКИ ===
exit /b 1
