@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"
set "REPO_ROOT=%~dp0.."
set "FLEXIS_DB=%LOCALAPPDATA%\flexis-db"

set "DOTNET=dotnet"
where dotnet >nul 2>&1
if errorlevel 1 (
  if exist "%LOCALAPPDATA%\dotnet-flexis\dotnet.exe" (
    set "DOTNET=%LOCALAPPDATA%\dotnet-flexis\dotnet.exe"
  ) else if exist "C:\Program Files\dotnet\dotnet.exe" (
    set "DOTNET=C:\Program Files\dotnet\dotnet.exe"
  ) else (
    echo dotnet was not found.
    echo Install .NET 10 SDK or add dotnet to PATH.
    pause
    exit /b 1
  )
)

call :port_open 5432
if errorlevel 1 call :start_flexis_postgres
call :port_open 27017
if errorlevel 1 call :start_flexis_mongo
call :port_open 5432
if errorlevel 1 call :start_docker_databases
call :port_open 5432
if errorlevel 1 (
  echo.
  echo PostgreSQL is not accepting connections on port 5432.
  if exist "%FLEXIS_DB%\pgsql\bin\pg_ctl.exe" (
    echo flexis-db is installed but Postgres did not start.
    if exist "%FLEXIS_DB%\postgres.log" (
      echo.
      echo Last lines from %FLEXIS_DB%\postgres.log:
      powershell -NoProfile -Command "Get-Content -Path '%FLEXIS_DB%\postgres.log' -Tail 12"
    )
    echo.
    echo Try: close other run.bat windows, then run backend\run.bat again.
    echo Or manually: "%FLEXIS_DB%\pgsql\bin\pg_ctl.exe" -D "%FLEXIS_DB%\pgdata" -m fast stop
    echo           then: "%FLEXIS_DB%\pgsql\bin\pg_ctl.exe" -D "%FLEXIS_DB%\pgdata" -l "%FLEXIS_DB%\postgres.log" start -w
  ) else (
    echo Install Docker Desktop or run the flexis-db setup once in Cursor.
  )
  pause
  exit /b 1
)

call :stop_api
"%DOTNET%" watch run --project src\Flexis.Api --non-interactive
exit /b 0

:start_flexis_postgres
set "PG_CTL=%FLEXIS_DB%\pgsql\bin\pg_ctl.exe"
set "PGDATA=%FLEXIS_DB%\pgdata"
set "PGLOG=%FLEXIS_DB%\postgres.log"
set "PG_ISREADY=%FLEXIS_DB%\pgsql\bin\pg_isready.exe"
if not exist "%PG_CTL%" exit /b 1
call :port_open 5432
if not errorlevel 1 exit /b 0
echo Starting flexis-db PostgreSQL...
"%PG_CTL%" -D "%PGDATA%" -l "%PGLOG%" status >nul 2>&1
if not errorlevel 1 goto wait_flexis_postgres
"%PG_CTL%" -D "%PGDATA%" -l "%PGLOG%" start -w -t 60
if errorlevel 1 (
  echo Postgres did not start. Stopping stale server and retrying once...
  "%PG_CTL%" -D "%PGDATA%" -m fast stop >nul 2>&1
  timeout /t 2 /nobreak >nul
  "%PG_CTL%" -D "%PGDATA%" -l "%PGLOG%" start -w -t 60
)
if errorlevel 1 exit /b 1
set "ATTEMPTS=0"
:wait_flexis_postgres
call :port_open 5432
if not errorlevel 1 exit /b 0
set /a ATTEMPTS+=1
if !ATTEMPTS! GEQ 60 exit /b 1
timeout /t 1 /nobreak >nul
goto wait_flexis_postgres

:start_flexis_mongo
set "MONGOD=%FLEXIS_DB%\mongo\bin\mongod.exe"
set "MONGO_DATA=%FLEXIS_DB%\mongo\data"
set "MONGO_LOG=%FLEXIS_DB%\mongo\mongod.log"
if not exist "%MONGOD%" exit /b 1
echo Starting flexis-db MongoDB...
powershell -NoProfile -Command ^
  "$p = Get-NetTCPConnection -LocalPort 27017 -State Listen -ErrorAction SilentlyContinue; if ($p) { exit 0 };" ^
  "$mongod = '%MONGOD%'; $data = '%MONGO_DATA%'; $log = '%MONGO_LOG%';" ^
  "New-Item -ItemType Directory -Force -Path $data | Out-Null;" ^
  "Start-Process -FilePath $mongod -ArgumentList @('--dbpath',$data,'--port','27017','--bind_ip','127.0.0.1','--logpath',$log,'--auth') -WindowStyle Hidden;" ^
  "for ($i = 0; $i -lt 30; $i++) { if (Get-NetTCPConnection -LocalPort 27017 -State Listen -ErrorAction SilentlyContinue) { exit 0 }; Start-Sleep -Seconds 1 }; exit 1"
exit /b %errorlevel%

:start_docker_databases
if exist "%FLEXIS_DB%\pgsql\bin\pg_ctl.exe" exit /b 1
call :find_docker
if errorlevel 1 exit /b 1
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" (
  "%DOCKER%" info >nul 2>&1
  if errorlevel 1 (
    echo Starting Docker Desktop...
    start "" "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
    set "ATTEMPTS=0"
    :wait_docker
    "%DOCKER%" info >nul 2>&1
    if not errorlevel 1 goto docker_ready
    set /a ATTEMPTS+=1
    if !ATTEMPTS! GEQ 120 exit /b 1
    timeout /t 2 /nobreak >nul
    goto wait_docker
  )
)
:docker_ready
echo Starting PostgreSQL and MongoDB with Docker...
pushd "%REPO_ROOT%"
"%DOCKER%" compose up -d
set "COMPOSE_ERROR=!errorlevel!"
popd
if not "!COMPOSE_ERROR!"=="0" exit /b 1
set "ATTEMPTS=0"
:wait_docker_postgres
call :port_open 5432
if not errorlevel 1 exit /b 0
set /a ATTEMPTS+=1
if !ATTEMPTS! GEQ 60 exit /b 1
timeout /t 1 /nobreak >nul
goto wait_docker_postgres

:find_docker
set "DOCKER="
where docker >nul 2>&1
if not errorlevel 1 (
  set "DOCKER=docker"
  exit /b 0
)
if exist "%ProgramFiles%\Docker\Docker\resources\bin\docker.exe" (
  set "DOCKER=%ProgramFiles%\Docker\Docker\resources\bin\docker.exe"
  exit /b 0
)
if exist "%LOCALAPPDATA%\Programs\Docker\Docker\resources\bin\docker.exe" (
  set "DOCKER=%LOCALAPPDATA%\Programs\Docker\Docker\resources\bin\docker.exe"
  exit /b 0
)
exit /b 1

:stop_api
echo Stopping any API already on port 5080...
powershell -NoProfile -Command "Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }" >nul 2>&1
taskkill /F /IM Flexis.Api.exe >nul 2>&1
timeout /t 2 /nobreak >nul
exit /b 0

:port_open
if exist "%FLEXIS_DB%\pgsql\bin\pg_isready.exe" (
  "%FLEXIS_DB%\pgsql\bin\pg_isready.exe" -h 127.0.0.1 -p %~1 -q
  exit /b %errorlevel%
)
powershell -NoProfile -Command "if ((Test-NetConnection -ComputerName 127.0.0.1 -Port %~1 -WarningAction SilentlyContinue).TcpTestSucceeded) { exit 0 } else { exit 1 }"
exit /b %errorlevel%
