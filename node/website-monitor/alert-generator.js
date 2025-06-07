import eywa from 'eywa-client'

/**
 * Alert Generator Module
 * 
 * Evaluates monitoring rules and generates alerts
 */

export class AlertGenerator {
    constructor() {
        this.alertTypes = {
            PRICE_DROP: 'Price has dropped',
            PRICE_INCREASE: 'Price has increased', 
            BACK_IN_STOCK: 'Item is back in stock',
            OUT_OF_STOCK: 'Item is out of stock',
            CONTENT_CHANGED: 'Content has changed',
            SITE_DOWN: 'Site is not responding',
            SITE_UP: 'Site is back online'
        }
    }
    
    /**
     * Evaluate rules against current and previous checks
     */
    evaluateRules(currentCheck, previousCheck, rules) {
        const alerts = []
        
        for (const rule of rules) {
            if (!rule.active) continue
            
            const alert = this.evaluateRule(rule, currentCheck, previousCheck)
            if (alert) {
                alerts.push(alert)
            }
        }
        
        return alerts
    }
    
    /**
     * Evaluate a single rule
     */
    evaluateRule(rule, current, previous) {
        switch (rule.type) {
            case 'THRESHOLD':
                return this.evaluateThreshold(rule, current)
                
            case 'CHANGE':
                return this.evaluateChange(rule, current, previous)
                
            case 'AVAILABILITY':
                return this.evaluateAvailability(rule, current, previous)
                
            case 'REGEX':
                return this.evaluateRegex(rule, current)
                
            default:
                return null
        }
    }
    
    /**
     * Threshold rule: value compared to fixed threshold
     */
    evaluateThreshold(rule, current) {
        if (!current.numeric_value) return null
        
        const value = current.numeric_value
        const threshold = rule.threshold_value
        let triggered = false
        
        switch (rule.operator) {
            case 'LESS_THAN':
                triggered = value < threshold
                break
            case 'GREATER_THAN':
                triggered = value > threshold
                break
            case 'EQUALS':
                triggered = Math.abs(value - threshold) < 0.01
                break
            case 'NOT_EQUALS':
                triggered = Math.abs(value - threshold) >= 0.01
                break
        }
        
        if (triggered) {
            return {
                type: value < threshold ? 'PRICE_DROP' : 'PRICE_INCREASE',
                severity: rule.alert_severity,
                message: `${rule.name}: Value ${value} ${rule.operator.toLowerCase().replace('_', ' ')} ${threshold}`,
                current_value: current.extracted_value,
                rule_name: rule.name
            }
        }
        
        return null
    }
    
    /**
     * Change rule: detect percentage or absolute changes
     */
    evaluateChange(rule, current, previous) {
        if (!current.numeric_value || !previous?.numeric_value) return null
        
        const currentVal = current.numeric_value
        const previousVal = previous.numeric_value
        const change = currentVal - previousVal
        const changePercent = (change / previousVal) * 100
        
        // Check if change exceeds threshold
        const threshold = rule.change_percentage || 0
        if (Math.abs(changePercent) >= threshold) {
            const type = change < 0 ? 'PRICE_DROP' : 'PRICE_INCREASE'
            
            return {
                type,
                severity: rule.alert_severity,
                message: `${rule.name}: ${type === 'PRICE_DROP' ? 'Decreased' : 'Increased'} by ${Math.abs(changePercent).toFixed(2)}% (${previousVal} → ${currentVal})`,
                previous_value: previous.extracted_value,
                current_value: current.extracted_value,
                change_percentage: changePercent,
                rule_name: rule.name
            }
        }
        
        return null
    }
    
    /**
     * Availability rule: detect stock status changes
     */
    evaluateAvailability(rule, current, previous) {
        if (current.available === null) return null
        
        // Check for status change
        if (previous && current.available !== previous.available) {
            const type = current.available ? 'BACK_IN_STOCK' : 'OUT_OF_STOCK'
            
            return {
                type,
                severity: rule.alert_severity || 'INFO',
                message: `${rule.name}: ${this.alertTypes[type]}`,
                current_value: current.extracted_value,
                previous_value: previous?.extracted_value,
                rule_name: rule.name
            }
        }
        
        return null
    }
    
    /**
     * Regex rule: match content against pattern
     */
    evaluateRegex(rule, current) {
        if (!current.extracted_value || !rule.regex_pattern) return null
        
        try {
            const regex = new RegExp(rule.regex_pattern, 'i')
            const matches = regex.test(current.extracted_value)
            
            if (matches) {
                return {
                    type: 'CONTENT_CHANGED',
                    severity: rule.alert_severity,
                    message: `${rule.name}: Content matches pattern "${rule.regex_pattern}"`,
                    current_value: current.extracted_value,
                    rule_name: rule.name
                }
            }
        } catch (error) {
            eywa.warn('Invalid regex pattern', {
                pattern: rule.regex_pattern,
                error: error.message
            })
        }
        
        return null
    }
    
    /**
     * Check for site status alerts
     */
    checkSiteStatus(current, previous) {
        const alerts = []
        
        // Site went down
        if (current.status === 'ERROR' && previous?.status === 'SUCCESS') {
            alerts.push({
                type: 'SITE_DOWN',
                severity: 'CRITICAL',
                message: `Site is not responding: ${current.error_message}`,
                current_value: current.error_message,
                previous_value: 'Online'
            })
        }
        
        // Site came back up
        if (current.status === 'SUCCESS' && previous?.status === 'ERROR') {
            alerts.push({
                type: 'SITE_UP',
                severity: 'INFO',
                message: 'Site is back online',
                current_value: 'Online',
                previous_value: previous.error_message
            })
        }
        
        return alerts
    }
    
    /**
     * Filter alerts based on cooldown period
     */
    async filterCooldowns(alerts, recentAlerts, rules) {
        const filtered = []
        const now = new Date()
        
        for (const alert of alerts) {
            const rule = rules.find(r => r.name === alert.rule_name)
            const cooldownMinutes = rule?.cool_down_minutes || 60
            
            // Check if similar alert was sent recently
            const recentAlert = recentAlerts.find(recent => 
                recent.type === alert.type &&
                recent.rule_name === alert.rule_name
            )
            
            if (recentAlert) {
                const minutesSince = (now - new Date(recentAlert.created_on)) / (1000 * 60)
                if (minutesSince < cooldownMinutes) {
                    eywa.info('Alert suppressed due to cooldown', {
                        type: alert.type,
                        cooldown_remaining: cooldownMinutes - minutesSince
                    })
                    continue
                }
            }
            
            filtered.push(alert)
        }
        
        return filtered
    }
}

// Export for use in main monitor
export default AlertGenerator
