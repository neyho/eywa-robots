@echo off
REM Windows setup script for EYWA robots

echo Setting up EYWA robot environments (Windows)...

REM Python environment
if exist python (
    echo Setting up Python environment...
    cd python
    if not exist ".venv" (
        echo Creating virtual environment...
        python -m venv .venv
    )
    .venv\Scripts\pip install --quiet --upgrade pip
    .venv\Scripts\pip install --quiet -r requirements.txt
    cd ..
)

REM Node.js environment
if exist node (
    echo Setting up Node.js environments...
    for /d %%d in (node\*) do (
        if exist "%%d\package.json" (
            echo   Installing dependencies for %%~nxd...
            cd "%%d"
            call npm install --quiet
            cd ..\..
        )
    )
)

REM Ruby environment
if exist ruby (
    if exist ruby\Gemfile (
        echo Setting up Ruby environment...
        cd ruby
        bundle install --quiet
        cd ..
    )
)

REM Go environment
if exist go (
    if exist go\go.mod (
        echo Setting up Go environment...
        cd go
        go mod download
        cd ..
    )
)

REM C# environment
if exist csharp (
    echo Setting up C# environments...
    for /d %%d in (csharp\*) do (
        if exist "%%d\*.csproj" (
            echo   Restoring packages for %%~nxd...
            cd "%%d"
            dotnet restore --verbosity quiet
            cd ..\..
        )
    )
)

REM Babashka environment
if exist babashka (
    if exist babashka\deps.edn (
        echo Babashka environment ready - dependencies loaded at runtime
    )
)

echo Environment setup complete!