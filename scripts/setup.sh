#!/bin/bash
# Unix/Linux/macOS setup script for EYWA robots

set -e

echo "🚀 Setting up EYWA robot environments (Unix)..."

# Python environment
cd python
[ ! -d ".venv" ] && python3 -m venv .venv
.venv/bin/pip install --quiet --upgrade pip
.venv/bin/pip install --quiet -r requirements.txt
cd ..

# Go environment
cd go/system-monitor
export GO111MODULE=on GOCACHE=/tmp/go-cache GOMODCACHE=/tmp/go-mod
[ ! -f "system-monitor" ] && go build -o system-monitor .
cd ../..

echo "✅ Environment setup complete!"
