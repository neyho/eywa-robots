# Website Monitor Data Model

This document defines the EYWA data model required for the Website Monitor & Alert System robot.

## Entities

### Monitor Target
Represents a website/product to monitor.

```
Entity: Monitor Target
Attributes:
# URL[string]              # Unique URL to monitor
* Name[string]             # Display name for the target
o Description[string]      # Optional description
* Active[boolean]          # Whether monitoring is active
* Check Interval[int]      # Minutes between checks
o CSS Selector[string]     # Element selector for price/content
o XPath[string]           # Alternative to CSS selector
o Expected Type[enum]{PRICE,AVAILABILITY,CONTENT,STATUS}
o Currency[currency]       # For price monitoring
o Notification Email[string]
o Notification Webhook[string]
* Created On[timestamp]
o Updated On[timestamp]
```

### Monitor Check
Records each monitoring check performed.

```
Entity: Monitor Check
Attributes:
* Timestamp[timestamp]     # When the check was performed
* Status[enum]{SUCCESS,FAILED,TIMEOUT,ERROR}
o Response Time[int]       # Milliseconds
o HTTP Status[int]         # HTTP response code
o Extracted Value[string]  # The value found (price, text, etc)
o Numeric Value[float]     # Parsed numeric value if applicable
o Available[boolean]       # For availability checks
o Error Message[string]    # If check failed
o Screenshot[string]       # Base64 or URL to screenshot
o Page Title[string]      # For context
o Has Changed[boolean]     # Compared to previous check
```

### Monitor Alert
Tracks alerts generated when conditions are met.

```
Entity: Monitor Alert
Attributes:
* Created On[timestamp]
* Type[enum]{PRICE_DROP,PRICE_INCREASE,BACK_IN_STOCK,OUT_OF_STOCK,CONTENT_CHANGED,SITE_DOWN,SITE_UP}
* Severity[enum]{INFO,WARNING,CRITICAL}
o Previous Value[string]
o Current Value[string]
o Change Percentage[float]  # For price changes
* Message[string]
o Sent[boolean]            # Whether notification was sent
o Sent On[timestamp]
o Send Error[string]       # If notification failed
```

### Monitor Rule
Defines alert conditions for a target.

```
Entity: Monitor Rule
Attributes:
# Name[string]
* Active[boolean]
* Type[enum]{THRESHOLD,CHANGE,REGEX,AVAILABILITY}
o Operator[enum]{LESS_THAN,GREATER_THAN,EQUALS,NOT_EQUALS,CONTAINS,MATCHES}
o Threshold Value[float]   # For numeric comparisons
o String Value[string]     # For text comparisons
o Regex Pattern[string]    # For pattern matching
o Change Percentage[float] # Trigger on X% change
* Alert Severity[enum]{INFO,WARNING,CRITICAL}
o Cool Down Minutes[int]   # Prevent alert spam
```

## Relationships

```
Monitor Target---checks[o2m]--->Monitor Check
Monitor Target---alerts[o2m]--->Monitor Alert
Monitor Target---rules[o2m]--->Monitor Rule
Monitor Check---alerts[o2m]--->Monitor Alert
Monitor Rule---alerts[o2m]--->Monitor Alert
```

## Example GraphQL Operations

### Create a new monitor target
```graphql
mutation {
  syncMonitorTarget(data: {
    url: "https://example.com/product/123",
    name: "Example Product",
    active: true,
    check_interval: 30,
    css_selector: ".price-now",
    expected_type: PRICE,
    currency: "USD"
  }) {
    euuid
    name
    url
  }
}
```

### Record a check result
```graphql
mutation {
  stackMonitorCheck(data: {
    timestamp: "2024-01-20T10:30:00Z",
    status: SUCCESS,
    response_time: 1250,
    http_status: 200,
    extracted_value: "$49.99",
    numeric_value: 49.99,
    available: true,
    has_changed: true
  }) {
    euuid
    timestamp
    extracted_value
  }
}
```

### Query recent checks for a target
```graphql
query {
  searchMonitorCheck(
    _where: {
      target: {url: {_eq: "https://example.com/product/123"}}
    },
    _order_by: {timestamp: desc},
    _limit: 10
  ) {
    timestamp
    status
    extracted_value
    numeric_value
    has_changed
  }
}
```

### Find active alerts
```graphql
query {
  searchMonitorAlert(
    _where: {
      sent: {_eq: false},
      severity: {_in: [WARNING, CRITICAL]}
    }
  ) {
    type
    message
    current_value
    created_on
    target {
      name
      url
    }
  }
}
```
