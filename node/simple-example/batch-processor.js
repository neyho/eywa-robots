import eywa from 'eywa-client'

/**
 * Batch Data Processor Robot
 * 
 * This robot demonstrates:
 * - Processing data in batches
 * - Progress reporting
 * - Error handling with retry
 */

async function processWithRetry(operation, maxRetries = 3) {
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
        try {
            return await operation()
        } catch (error) {
            eywa.warn(`Attempt ${attempt} failed: ${error.message}`)
            if (attempt === maxRetries) throw error
            // Wait before retry (exponential backoff)
            await new Promise(resolve => setTimeout(resolve, 1000 * attempt))
        }
    }
}

async function processBatch(items, batchNumber) {
    eywa.info(`Processing batch ${batchNumber}`, { 
        itemCount: items.length 
    })
    
    // Simulate some processing
    const processed = items.map(item => ({
        ...item,
        processed: true,
        processedAt: new Date().toISOString(),
        batchNumber
    }))
    
    // Example: Save to database
    const result = await eywa.graphql(`
        mutation saveBatch($data: [ProcessedItemInput]!) {
            stackProcessedItemList(data: $data) {
                inserted
                updated
            }
        }
    `, { data: processed })
    
    return result.data.stackProcessedItemList
}

async function main() {
    eywa.open_pipe()
    
    try {
        const task = await eywa.get_task()
        const { items, batchSize = 10 } = task.input
        
        eywa.info('Starting batch processor', { 
            totalItems: items.length,
            batchSize 
        })
        
        eywa.update_task(eywa.PROCESSING)
        
        // Process items in batches
        const results = []
        const totalBatches = Math.ceil(items.length / batchSize)
        
        for (let i = 0; i < items.length; i += batchSize) {
            const batchNumber = Math.floor(i / batchSize) + 1
            const batch = items.slice(i, i + batchSize)
            
            // Update progress
            const progress = Math.round((batchNumber / totalBatches) * 100)
            eywa.update_task(eywa.PROCESSING, { progress })
            
            // Process batch with retry logic
            const batchResult = await processWithRetry(
                () => processBatch(batch, batchNumber)
            )
            
            results.push({
                batch: batchNumber,
                processed: batch.length,
                ...batchResult
            })
            
            // Report intermediate progress
            eywa.info(`Batch ${batchNumber}/${totalBatches} complete`, {
                progress: `${progress}%`,
                totalProcessed: i + batch.length
            })
        }
        
        // Final report
        const summary = {
            totalItems: items.length,
            totalBatches,
            batchSize,
            results,
            completedAt: new Date().toISOString()
        }
        
        eywa.report('Batch processing complete', summary)
        eywa.close_task(eywa.SUCCESS)
        
    } catch (error) {
        eywa.error('Batch processing failed', { 
            error: error.message,
            stack: error.stack 
        })
        eywa.close_task(eywa.ERROR)
    }
}

main()
