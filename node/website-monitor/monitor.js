import eywa from 'eywa-client'
import { chromium } from 'playwright'
import fetch from 'node-fetch'

/**
 * Website Monitor Robot using Playwright
 * More reliable alternative to Puppeteer
 */

async function monitorTarget(target) {
    const startTime = Date.now()
    const result = {
        timestamp: new Date().toISOString(),
        target_url: target.url,
        status: 'SUCCESS',
        extracted_value: null,
        numeric_value: null,
        available: null,
        error_message: null
    }
    
    let browser
    
    try {
        eywa.info(`Starting monitor check for: ${target.name}`, { url: target.url })
        
        // Launch Playwright
        browser = await chromium.launch({
            headless: true
        })
        
        const page = await browser.newPage()
        await page.goto(target.url, { timeout: 30000 })
        
        // Extract value
        if (target.css_selector) {
            const element = await page.$(target.css_selector)
            if (element) {
                result.extracted_value = await element.textContent()
                eywa.info('Extracted value', { value: result.extracted_value })
                
                // Parse price if needed
                if (target.expected_type === 'PRICE' && result.extracted_value) {
                    const cleaned = result.extracted_value.replace(/[^0-9.,]/g, '')
                    result.numeric_value = parseFloat(cleaned.replace(/,/g, ''))
                }
            }
        }
        
        result.response_time = Date.now() - startTime
        
    } catch (error) {
        result.status = 'ERROR'
        result.error_message = error.message
        eywa.error('Monitor check failed', { error: error.message })
    } finally {
        if (browser) await browser.close()
    }
    
    return result
}

async function main() {
    eywa.open_pipe()
    
    try {
        const task = await eywa.get_task()
        const input = task.input || {}
        
        eywa.update_task(eywa.PROCESSING)
        eywa.info('Website monitor robot started (Playwright)', { input })
        
        const result = await monitorTarget(input)
        
        eywa.report('Monitor check completed', result)
        eywa.close_task(eywa.SUCCESS)
        
    } catch (error) {
        eywa.error('Robot execution failed', { error: error.message })
        eywa.close_task(eywa.ERROR)
    }
}

main()
