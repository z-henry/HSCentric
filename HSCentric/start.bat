@echo off
cd /d "%~dp0"
"%~dp0HSCentric.exe" --host=* --port=17321 --no-browser
if errorlevel 1 pause
