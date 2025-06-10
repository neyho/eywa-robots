# EYWA Robot Setup Scripts

This directory contains environment setup scripts that are automatically run by EYWA's `onGitUpdate` hook when the repository is deployed or updated.

## Scripts

### `setup.sh` (Unix/Linux/macOS)
- Sets up Python virtual environments
- Builds Go binaries
- Installs all dependencies

### `setup.bat` (Windows)
- Windows equivalent of setup.sh
- Uses Windows-specific paths and commands

## How It Works

1. When you push code to the repository, EYWA automatically runs `onGitUpdate`
2. The `onGitUpdate` mutation detects the OS and runs the appropriate script:
   - Unix/Linux/macOS: `bash scripts/setup.sh`
   - Windows: `scripts\setup.bat`
3. The scripts only create environments if they don't exist (idempotent)

## Benefits

- **OS-agnostic**: Works on any platform
- **No manual setup**: Environments are ready automatically
- **Efficient**: Only rebuilds when necessary
- **Centralized**: All setup logic in one place

## Adding New Languages

To add support for a new language (e.g., Node.js):

1. Update `setup.sh`:
   ```bash
   # Node.js environment
   cd node
   [ ! -d "node_modules" ] && npm ci
   cd ..
   ```

2. Update `setup.bat`:
   ```batch
   REM Node.js environment
   cd node
   if not exist "node_modules" npm ci
   cd ..
   ```

## Testing Locally

```bash
# Unix/Linux/macOS
bash scripts/setup.sh

# Windows
scripts\setup.bat
```
