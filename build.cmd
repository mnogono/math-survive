@echo off
rem Сборка Math Survive: C#-решение Godot и тесты логики.
rem Использование: build.cmd [notest]
setlocal
chcp 65001 >nul
cd /d "%~dp0"

set "DOTNET=dotnet"
where dotnet >nul 2>nul || set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"

echo === Сборка MathSurvive.sln ===
"%DOTNET%" build MathSurvive.sln -nologo -v:minimal || goto :fail

if /i "%~1"=="notest" goto :ok

echo === Тесты логики ===
"%DOTNET%" test tests\MathSurvive.Core.Tests -nologo -v:minimal || goto :fail

:ok
echo === Готово ===
exit /b 0

:fail
echo === ОШИБКА СБОРКИ ===
exit /b 1
