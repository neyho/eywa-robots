import eywa from 'eywa-client'
import { chromium } from 'playwright'

// ECB exchange rates page
const ECB_RATES_URL = 'https://www.ecb.europa.eu/stats/policy_and_exchange_rates/euro_reference_exchange_rates/html/index.en.html'

async function main() {
    eywa.open_pipe()

    let browser = null

    try {
        // Get task context
        const task = await eywa.get_task()
        const input = task.input || {}

        eywa.update_task(eywa.PROCESSING)
        eywa.info('Starting ECB Exchange Rate Collection via Web Scraping')

        // Launch browser in non-headless mode
        browser = await chromium.launch({
            headless: false,
            args: ['--window-size=1280,800']
        })

        const context = await browser.newContext({
            viewport: { width: 1280, height: 800 }
        })
        const page = await context.newPage()

        eywa.info('Navigating to ECB exchange rates page...')

        // Navigate to the ECB exchange rates page
        await page.goto(ECB_RATES_URL, {
            waitUntil: 'domcontentloaded',
            timeout: 30000
        })

        // Wait a bit for page to load
        await page.waitForTimeout(5000)

        // Accept cookies if banner appears
        try {
            const cookieButton = await page.locator('button:has-text("I understand")').first()
            if (await cookieButton.isVisible({ timeout: 3000 })) {
                await cookieButton.click()
                eywa.info('Accepted cookie banner')
                await page.waitForTimeout(2000)
            }
        } catch (e) {
            // Cookie banner might not appear
        }

        eywa.info('Looking for exchange rates on the page...')

        // Extract rates from the forex table
        const rates = await page.evaluate(() => {
            const extractedRates = []

            // Find the forex table
            const forexTable = document.querySelector('.forextable')
            if (forexTable) {
                const rows = forexTable.querySelectorAll('tbody tr')

                rows.forEach(row => {
                    const currencyCell = row.querySelector('.currency')
                    const spotCell = row.querySelector('.spot')

                    if (currencyCell && spotCell) {
                        const currency = currencyCell.textContent.trim()
                        const rateText = spotCell.textContent.trim()
                        const rate = parseFloat(rateText.replace(',', '.'))

                        if (currency && !isNaN(rate)) {
                            extractedRates.push({
                                currency: currency,
                                rate: rate
                            })
                        }
                    }
                })
            }

            return extractedRates
        })

        eywa.info(`Found ${rates.length} exchange rates`)

        // Take a screenshot
        await page.screenshot({
            path: 'ecb-rates-screenshot.png',
            fullPage: true
        })
        eywa.info('Screenshot saved: ecb-rates-screenshot.png')

        if (rates.length === 0) {
            throw new Error('No exchange rates found on the page')
        }

        // Get today's date
        const today = new Date()
        const isoDate = today.toISOString()

        // Prepare data for EYWA
        const exchangeRates = rates.map(rate => ({
            base_currency: 'EUR',
            target_currency: rate.currency,
            rate: rate.rate,
            date: isoDate,
            source: 'ECB'
        }))

        // Add EUR to EUR rate if not present
        if (!exchangeRates.find(r => r.target_currency === 'EUR')) {
            exchangeRates.push({
                base_currency: 'EUR',
                target_currency: 'EUR',
                rate: 1.0,
                date: isoDate,
                source: 'ECB'
            })
        }

        eywa.info(`Storing ${exchangeRates.length} exchange rates in EYWA...`)

        // Store in EYWA
        const mutation = `
            mutation($data: [ExampleExchangeRateInput!]!) {
                syncExampleExchangeRateList(data: $data) {
                    euuid
                    base_currency
                    target_currency
                    rate
                    date
                    source
                }
            }
        `

        const mutationResult = await eywa.graphql(mutation, { data: exchangeRates })

        // Check if we got results
        let storedRates = []
        if (mutationResult && mutationResult.syncExampleExchangeRateList) {
            storedRates = mutationResult.syncExampleExchangeRateList
            eywa.info(`Successfully stored ${storedRates.length} exchange rates`)
        } else {
            eywa.warn('No results returned from mutation', mutationResult)
            storedRates = exchangeRates // Use original data for summary
        }

        // Create summary
        const summary = {
            date: today.toDateString(),
            totalRates: storedRates.length,
            sampleRates: {
                USD: storedRates.find(r => r.target_currency === 'USD')?.rate,
                GBP: storedRates.find(r => r.target_currency === 'GBP')?.rate,
                JPY: storedRates.find(r => r.target_currency === 'JPY')?.rate,
                CHF: storedRates.find(r => r.target_currency === 'CHF')?.rate,
                CNY: storedRates.find(r => r.target_currency === 'CNY')?.rate
            },
            allCurrencies: storedRates.map(r => r.target_currency).sort()
        }

        eywa.report('Exchange Rate Collection Complete', summary)

        // Keep browser open for a few seconds
        await page.waitForTimeout(5000)

        eywa.close_task(eywa.SUCCESS)

    } catch (error) {
        eywa.error(`Error: ${error.message}`, { stack: error.stack })
        eywa.close_task(eywa.ERROR)
    } finally {
        if (browser) {
            await browser.close()
        }
    }
}

// Run the robot
main().catch(error => {
    console.error('Fatal error:', error)
    process.exit(1)
})
