@echo off
REM Windows setup script for EYWA robots

echo Setting up EYWA robot environments (Windows)...

REM Python environment
cd python
if not exist ".venv" (
    echo Creating virtual environment...
    python -m venv .venv
)
.venv\Scripts\pip install --quiet --upgrade pip
.venv\Scripts\pip install --quiet -r requirements.txt
cd ..

REM Go environment
cd go\system-monitor
set GO111MODULE=on
set GOCACHE=%TEMP%\go-cache
set GOMODCACHE=%TEMP%\go-mod
if not exist "system-monitor.exe" (
    echo Building Go binary...
    go mod download
    go build -o system-monitor.exe .
)
cd ..\..

echo Environment setup complete!
