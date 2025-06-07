# Website Monitor Robot

An EYWA robot that monitors websites for price changes, availability updates, and content modifications.

## Features

- **Multi-Type Monitoring**
  - Price tracking with currency support
  - Product availability detection
  - Content change monitoring
  - Site uptime checking

- **Flexible Data Extraction**
  - CSS selector support
  - XPath selector support
  - Automatic price parsing
  - Availability detection patterns

- **Smart Alerting**
  - Rule-based alert generation
  - Multiple alert types (price drop, back in stock, etc.)
  - Severity levels (INFO, WARNING, CRITICAL)
  - Cooldown periods to prevent spam

- **Notifications**
  - Email notifications with HTML templates
  - Webhook support for integrations
  - Detailed alert information

## Installation

```bash
cd node/website-monitor
npm install
```

## Usage

### Test locally with EYWA

```bash
# Basic test
npm test

# Test with specific scenarios
npm run test:price-drop
npm run test:availability
```

### Task Input Schema

```json
{
  "input": {
    "url": "https://example.com/product",
    "name": "Product Name",
    "css_selector": ".price-class",
    "xpath": "//div[@class='price']",
    "expected_type": "PRICE|AVAILABILITY|CONTENT|STATUS",
    "capture_screenshot": true,
    "currency": "USD",
    "check_rules": true,
    "rules": [...],
    "notification_email": "alerts@example.com",
    "notification_webhook": "https://webhook.url"
  }
}
```

## Architecture

The robot is modular with three main components:

1. **monitor.js** - Main robot that performs web scraping
2. **alert-generator.js** - Evaluates rules and generates alerts
3. **notification-sender.js** - Sends notifications via various channels

## Rule Types

### Threshold Rule
Triggers when value crosses a threshold:
```json
{
  "type": "THRESHOLD",
  "operator": "LESS_THAN",
  "threshold_value": 50,
  "alert_severity": "INFO"
}
```

### Change Rule
Triggers on percentage changes:
```json
{
  "type": "CHANGE",
  "change_percentage": 20,
  "alert_severity": "WARNING"
}
```

### Availability Rule
Triggers on stock status changes:
```json
{
  "type": "AVAILABILITY",
  "alert_severity": "CRITICAL"
}
```

### Regex Rule
Triggers when content matches pattern:
```json
{
  "type": "REGEX",
  "regex_pattern": "limited|sale|discount",
  "alert_severity": "INFO"
}
```

## Environment Variables

- `EMAIL_API_URL` - Email service API endpoint
- `EMAIL_API_KEY` - Email service API key

## Future Enhancements

- [ ] Add proxy support for geo-restricted sites
- [ ] Implement browser fingerprint rotation
- [ ] Add more notification channels (SMS, Discord, etc.)
- [ ] Support for monitoring multiple elements per page
- [ ] Historical price charts
- [ ] Bulk monitoring from CSV/Excel
- [ ] API monitoring support
- [ ] Custom JavaScript execution for complex extractions

## Example Use Cases

1. **E-commerce Price Tracking**
   - Monitor competitor prices
   - Track deals and discounts
   - Alert on price drops

2. **Inventory Management**
   - Track product availability
   - Monitor restocks
   - Alert when items are back

3. **Content Monitoring**
   - Track website changes
   - Monitor news updates
   - Detect policy changes

4. **Uptime Monitoring**
   - Check site availability
   - Monitor response times
   - Alert on downtime
