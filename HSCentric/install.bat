@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\scripts\build.ps1"
exit /b %errorlevel%
