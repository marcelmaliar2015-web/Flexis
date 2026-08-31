@echo off
cd /d "%~dp0"

set "NPM="
if exist "C:\Program Files\nodejs\npm.cmd" (
  set "NPM=C:\Program Files\nodejs\npm.cmd"
) else (
  for /f "delims=" %%I in ('where npm.cmd 2^>nul') do (
    set "NPM=%%I"
    goto npm_found
  )
)

:npm_found
if not defined NPM (
  echo npm.cmd was not found.
  echo Install Node.js or add it to PATH.
  pause
  exit /b 1
)

if not exist "node_modules\.bin\vite.cmd" (
  echo Installing frontend dependencies...
  call "%NPM%" install
  if errorlevel 1 (
    pause
    exit /b 1
  )
)

echo Starting frontend on http://127.0.0.1:5173
call "%NPM%" run dev
