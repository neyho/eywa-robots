import eywa from 'eywa-client'

/**
 * Simple EYWA Robot Example
 * 
 * This robot demonstrates the basic structure and lifecycle:
 * 1. Open communication pipe
 * 2. Get task input
 * 3. Process data
 * 4. Report results
 * 5. Close task
 */

async function main() {
    // Step 1: Open communication pipe with EYWA
    eywa.open_pipe()
    
    try {
        // Step 2: Get the task data
        const task = await eywa.get_task()
        const input = task.input
        
        // Log that we received the task
        eywa.info('Task received', { 
            taskId: task.euuid,
            inputKeys: Object.keys(input) 
        })
        
        // Step 3: Update task status to show we're working
        eywa.update_task(eywa.PROCESSING)
        
        // Step 4: Do some simple processing
        const greeting = input.name 
            ? `Hello, ${input.name}!` 
            : 'Hello, World!'
            
        const message = input.message 
            ? `Your message: "${input.message}"` 
            : 'No message provided'
        
        eywa.info('Processing complete', { greeting, message })
        
        // Step 5: Create a result object
        const result = {
            greeting,
            message,
            processedAt: new Date().toISOString(),
            inputReceived: input
        }
        
        // Step 6: Report the results
        eywa.report('Task completed successfully', result)
        
        // Step 7: Close the task as successful
        eywa.close_task(eywa.SUCCESS)
        
    } catch (error) {
        // Handle any errors
        eywa.error('Task failed', { 
            error: error.message,
            stack: error.stack 
        })
        
        // Close task with error status
        eywa.close_task(eywa.ERROR)
    }
}

// Run the robot
main()
