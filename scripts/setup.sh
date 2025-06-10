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

echo "✅ Environment setup complete!"
