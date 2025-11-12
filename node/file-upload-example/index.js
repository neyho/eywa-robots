#!/usr/bin/env node
/**
 * EYWA File Upload Example
 * 
 * This robot demonstrates uploading files to EYWA:
 * 1. Creates a test file
 * 2. Uploads it using quickUpload
 * 3. Verifies the upload
 * 4. Lists uploaded files
 * 
 * Usage:
 *     eywa run -c "node index.js"
 */

import eywa from 'eywa-client'
import fs from 'fs/promises'
import { tmpdir } from 'os'
import { join } from 'path'

async function uploadFileExample() {
    console.log('🚀 EYWA File Upload Example Starting')
    
    try {
        // Step 1: Create a test file to upload
        eywa.info('📝 Creating test file...')
        
        const testContent = `Test File Upload to EYWA
Generated at: ${new Date().toISOString()}
This is a demonstration of file upload capabilities.

Sample data:
- Item 1
- Item 2
- Item 3
`
        
        const tempFile = join(tmpdir(), 'eywa_upload_test.txt')
        await fs.writeFile(tempFile, testContent)
        eywa.info(`✅ Test file created: ${tempFile}`)
        
        try {
            // Step 2: Upload the file using quickUpload
            eywa.info('📤 Uploading file to EYWA...')
            
            const fileUuid = await eywa.quickUpload(tempFile)
            eywa.info(`✅ File uploaded successfully!`)
            eywa.info(`   File UUID: ${fileUuid}`)
            
            // Step 3: Get file info to verify
            eywa.info('🔍 Retrieving file information...')
            
            const fileInfo = await eywa.getFileInfo(fileUuid)
            if (fileInfo) {
                eywa.info('📄 File Details:')
                eywa.info(`   • Name: ${fileInfo.name}`)
                eywa.info(`   • UUID: ${fileInfo.euuid}`)
                eywa.info(`   • Status: ${fileInfo.status}`)
                eywa.info(`   • Size: ${fileInfo.size} bytes`)
                eywa.info(`   • Content Type: ${fileInfo.content_type}`)
            }
            
            // Step 4: List recent files
            eywa.info('📋 Listing recent files...')
            
            const recentFiles = await eywa.listFiles({ limit: 5 })
            eywa.info(`📁 Found ${recentFiles.length} recent files:`)
            recentFiles.forEach((file, index) => {
                const statusEmoji = file.status === 'UPLOADED' ? '✅' : '⏳'
                eywa.info(`   ${index + 1}. ${statusEmoji} ${file.name} (${file.size} bytes)`)
            })
            
            // Step 5: Report success
            eywa.report('File Upload Complete', {
                status: 'success',
                file_uuid: fileUuid,
                file_name: fileInfo?.name,
                file_size: fileInfo?.size,
                total_files: recentFiles.length
            })
            
        } finally {
            // Cleanup: Remove temporary file
            try {
                await fs.unlink(tempFile)
                eywa.info('🧹 Cleaned up temporary file')
            } catch (e) {
                // Ignore cleanup errors
            }
        }
        
        eywa.info('🎉 File Upload Example Completed Successfully!')
        
    } catch (error) {
        eywa.error(`❌ Upload failed: ${error.message}`)
        console.error(error.stack)
        throw error
    }
}

async function main() {
    try {
        // Open communication pipe with EYWA
        eywa.open_pipe()
        
        // Wait for pipe to initialize
        await new Promise(resolve => setTimeout(resolve, 100))
        
        // Get task context
        const task = await eywa.get_task()
        eywa.info(`Task received: ${task.name || 'File Upload Example'}`)
        
        // Update task status to processing
        eywa.update_task(eywa.PROCESSING)
        
        // Run the upload example
        await uploadFileExample()
        
        // Complete successfully
        eywa.close_task(eywa.SUCCESS)
        
    } catch (error) {
        eywa.error(`Task failed: ${error.message}`)
        eywa.close_task(eywa.ERROR)
    }
}

main().catch(console.error)
