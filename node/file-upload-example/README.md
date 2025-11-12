# EYWA File Upload Example

A simple EYWA robot that demonstrates file upload capabilities.

## What it does

1. Creates a test file with sample content
2. Uploads the file to EYWA using `quickUpload()`
3. Retrieves and displays file information
4. Lists recent files in EYWA
5. Reports results back to EYWA

## Installation

```bash
npm install
```

## Usage

Run the robot using the EYWA CLI:

```bash
eywa run -c "node index.js"
```

Or with a task file:

```bash
eywa run -c "node index.js" -t task.json
```

## Features Demonstrated

- ✅ File upload using `eywa.quickUpload()`
- ✅ File information retrieval using `eywa.getFileInfo()`
- ✅ File listing using `eywa.listFiles()`
- ✅ Task lifecycle management
- ✅ Progress reporting with `eywa.report()`
- ✅ Error handling and cleanup

## API Methods Used

### Upload
- `eywa.quickUpload(filePath)` - Simple file upload returning UUID

### Query
- `eywa.getFileInfo(fileUuid)` - Get detailed file metadata
- `eywa.listFiles(options)` - List files with optional filters

### Task Management
- `eywa.open_pipe()` - Initialize communication
- `eywa.get_task()` - Get task context
- `eywa.update_task(status)` - Update task status
- `eywa.close_task(status)` - Complete task
- `eywa.report(title, data)` - Report results

### Logging
- `eywa.info(message)` - Info level logging
- `eywa.error(message)` - Error level logging

## Example Output

```
🚀 EYWA File Upload Example Starting
📝 Creating test file...
✅ Test file created: /tmp/eywa_upload_test.txt
📤 Uploading file to EYWA...
✅ File uploaded successfully!
   File UUID: abc123...
🔍 Retrieving file information...
📄 File Details:
   • Name: eywa_upload_test.txt
   • UUID: abc123...
   • Status: UPLOADED
   • Size: 234 bytes
   • Content Type: text/plain
📋 Listing recent files...
📁 Found 5 recent files:
   1. ✅ eywa_upload_test.txt (234 bytes)
   2. ✅ another_file.json (512 bytes)
   ...
🧹 Cleaned up temporary file
🎉 File Upload Example Completed Successfully!
```
