@echo off
cd /d "%~dp0"
dotnet watch run --project src\Flexis.Api --non-interactive
