@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\ngrok\start-public.ps1" %*
if errorlevel 1 pause
