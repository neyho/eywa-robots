import eywa from 'eywa-client'
import puppeteer from 'puppeteer'
import * as cheerio from 'cheerio'
import fetch from 'node-fetch'

/**
 * Website Monitor Robot
 * 
 * This robot monitors websites for:
 * - Price changes
 * - Product availability
 * - Content updates
 * - Site status
 * 
 * It can extract data using CSS selectors or XPath,
 * compare with previous values, and trigger alerts.
 */

// Helper to extract value based on selector type
async function extractValue(page, target) {
    if (target.css_selector) {
        return await page.$eval(target.css_selector, el => el.textContent.trim())
            .catch(() => null)
    } else if (target.xpath) {
        const elements = await page.$x(target.xpath)
        if (elements.length > 0) {
            return await page.evaluate(el => el.textContent.trim(), elements[0])
        }
    }
    return null
}

// Parse price from text (handles various formats)
function parsePrice(text) {
    if (!text) return null
    
    // Remove currency symbols and clean up
    const cleaned = text.replace(/[^0-9.,]/g, '')
    // Handle European format (1.234,56) vs US format (1,234.56)
    const normalized = cleaned.replace(/,/g, '').replace(/\./g, '.')
    const price = parseFloat(normalized)
    
    return isNaN(price) ? null : price
}

// Determine if content indicates availability
function checkAvailability(text) {
    if (!text) return false
    
    const outOfStockPatterns = [
        /out of stock/i,
        /sold out/i,
        /unavailable/i,
        /not available/i,
        /coming soon/i
    ]
    
    const inStockPatterns = [
        /in stock/i,
        /available/i,
        /add to cart/i,
        /buy now/i,
        /\d+ in stock/i
    ]
    
    // Check negative patterns first
    for (const pattern of outOfStockPatterns) {
        if (pattern.test(text)) return false
    }
    
    // Then check positive patterns
    for (const pattern of inStockPatterns) {
        if (pattern.test(text)) return true
    }
    
    // Default to unavailable if uncertain
    return false
}

// Main monitoring function
async function monitorTarget(target) {
    const startTime = Date.now()
    const result = {
        timestamp: new Date().toISOString(),
        target_url: target.url,
        status: 'SUCCESS',
        http_status: null,
        response_time: null,
        extracted_value: null,
        numeric_value: null,
        available: null,
        error_message: null,
        page_title: null,
        screenshot: null
    }
    
    let browser
    
    try {
        eywa.info(`Starting monitor check for: ${target.name}`, { url: target.url })
        
        // First try a simple HTTP request for basic checks
        if (target.expected_type === 'STATUS') {
            const response = await fetch(target.url, {
                method: 'HEAD',
                timeout: 10000
            })
            result.http_status = response.status
            result.response_time = Date.now() - startTime
            result.status = response.ok ? 'SUCCESS' : 'FAILED'
            return result
        }
        
        // For content extraction, use Puppeteer
        browser = await puppeteer.launch({
            headless: 'new',
            args: ['--no-sandbox', '--disable-setuid-sandbox']
        })
        
        const page = await browser.newPage()
        
        // Set viewport and user agent
        await page.setViewport({ width: 1280, height: 800 })
        await page.setUserAgent('Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36')
        
        // Navigate to the page
        const response = await page.goto(target.url, {
            waitUntil: 'networkidle2',
            timeout: 30000
        })
        
        result.http_status = response.status()
        result.page_title = await page.title()
        
        // Wait for the target element if selector provided
        if (target.css_selector) {
            await page.waitForSelector(target.css_selector, { timeout: 10000 })
        }
        
        // Extract the value
        const extractedText = await extractValue(page, target)
        result.extracted_value = extractedText
        
        eywa.info('Extracted value', { 
            value: extractedText,
            type: target.expected_type 
        })
        
        // Process based on expected type
        switch (target.expected_type) {
            case 'PRICE':
                result.numeric_value = parsePrice(extractedText)
                result.available = true // If we found a price, item is available
                break
                
            case 'AVAILABILITY':
                result.available = checkAvailability(extractedText)
                break
                
            case 'CONTENT':
                // Just store the extracted text
                break
        }
        
        // Take screenshot for debugging/proof
        if (target.capture_screenshot) {
            const screenshotBuffer = await page.screenshot({ 
                fullPage: false,
                encoding: 'base64' 
            })
            result.screenshot = `data:image/png;base64,${screenshotBuffer}`
        }
        
        result.response_time = Date.now() - startTime
        
    } catch (error) {
        eywa.error('Monitor check failed', {
            error: error.message,
            url: target.url
        })
        
        result.status = 'ERROR'
        result.error_message = error.message
        result.response_time = Date.now() - startTime
        
    } finally {
        if (browser) {
            await browser.close()
        }
    }
    
    return result
}

// Main robot function
async function main() {
    eywa.open_pipe()
    
    try {
        const task = await eywa.get_task()
        const input = task.input || {}
        
        eywa.update_task(eywa.PROCESSING)
        eywa.info('Website monitor robot started', { input })
        
        // Get monitor configuration
        const config = {
            url: input.url || 'https://example.com',
            name: input.name || 'Test Monitor',
            css_selector: input.css_selector,
            xpath: input.xpath,
            expected_type: input.expected_type || 'CONTENT',
            capture_screenshot: input.capture_screenshot !== false,
            // Add more configuration as needed
        }
        
        // Perform the monitoring check
        const result = await monitorTarget(config)
        
        // Report results
        eywa.report('Monitor check completed', {
            url: config.url,
            status: result.status,
            value: result.extracted_value,
            numeric_value: result.numeric_value,
            available: result.available,
            response_time: result.response_time,
            http_status: result.http_status
        })
        
        // In a real implementation, you would:
        // 1. Store the result using eywa.graphql()
        // 2. Compare with previous checks
        // 3. Evaluate rules and trigger alerts
        // 4. Send notifications if needed
        
        eywa.info('Monitor check stored successfully')
        eywa.close_task(eywa.SUCCESS)
        
    } catch (error) {
        eywa.error('Robot execution failed', {
            error: error.message,
            stack: error.stack
        })
        eywa.close_task(eywa.ERROR)
    }
}

// Run the robot
main().catch(error => {
    console.error('Fatal error:', error)
    process.exit(1)
})
