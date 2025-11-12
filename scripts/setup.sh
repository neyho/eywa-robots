#!/bin/bash
# Unix/Linux/macOS setup script for EYWA robots

set -e

echo "🚀 Setting up EYWA robot environments (Unix)..."

# Python environment
if [ -d "python" ]; then
    echo "📦 Setting up Python environment..."
    cd python
    [ ! -d ".venv" ] && python3 -m venv .venv
    .venv/bin/pip install --quiet --upgrade pip
    .venv/bin/pip install --quiet -r requirements.txt
    cd ..
fi

# Node.js environment
if [ -d "node" ]; then
    echo "📦 Setting up Node.js environments..."
    for dir in node/*/; do
        if [ -f "$dir/package.json" ]; then
            echo "  Installing dependencies for $(basename $dir)..."
            cd "$dir"
            npm install --quiet
            cd - > /dev/null
        fi
    done
fi

# Ruby environment
if [ -d "ruby" ] && [ -f "ruby/Gemfile" ]; then
    echo "📦 Setting up Ruby environment..."
    cd ruby
    bundle install --quiet
    cd ..
fi

# Go environment
if [ -d "go" ] && [ -f "go/go.mod" ]; then
    echo "📦 Setting up Go environment..."
    cd go
    go mod download
    cd ..
fi

# C# environment
if [ -d "csharp" ]; then
    echo "🔷 Setting up C# environments..."
    for dir in csharp/*/; do
        if [ -f "$dir"*.csproj ]; then
            echo "  Restoring packages for $(basename "$dir")..."
            cd "$dir"
            dotnet restore --quiet
            cd - > /dev/null
        fi
    done
fi

# Babashka environment
if [ -d "babashka" ] && [ -f "babashka/deps.edn" ]; then
    echo "📦 Babashka environment ready (dependencies loaded at runtime)"
fi

echo "✅ Environment setup complete!"