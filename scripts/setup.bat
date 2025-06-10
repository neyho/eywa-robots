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

echo Environment setup complete!
