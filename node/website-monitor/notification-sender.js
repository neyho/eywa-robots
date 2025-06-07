import eywa from 'eywa-client'
import fetch from 'node-fetch'

/**
 * Notification Sender Module
 * 
 * Sends alerts via email, webhooks, and other channels
 */

export class NotificationSender {
    constructor(config = {}) {
        this.config = {
            email_api_url: config.email_api_url || process.env.EMAIL_API_URL,
            email_api_key: config.email_api_key || process.env.EMAIL_API_KEY,
            default_from: config.default_from || 'alerts@monitor.eywa',
            ...config
        }
    }
    
    /**
     * Send notifications for alerts
     */
    async sendAlerts(alerts, target) {
        const results = []
        
        for (const alert of alerts) {
            const result = {
                alert,
                sent: false,
                sent_on: null,
                send_error: null
            }
            
            try {
                // Send based on configured channels
                const notifications = []
                
                if (target.notification_email) {
                    notifications.push(
                        this.sendEmail(alert, target)
                    )
                }
                
                if (target.notification_webhook) {
                    notifications.push(
                        this.sendWebhook(alert, target)
                    )
                }
                
                // Wait for all notifications to complete
                await Promise.all(notifications)
                
                result.sent = true
                result.sent_on = new Date().toISOString()
                
                eywa.info('Alert sent successfully', {
                    type: alert.type,
                    channels: notifications.length
                })
                
            } catch (error) {
                result.send_error = error.message
                eywa.error('Failed to send alert', {
                    type: alert.type,
                    error: error.message
                })
            }
            
            results.push(result)
        }
        
        return results
    }
    
    /**
     * Send email notification
     */
    async sendEmail(alert, target) {
        if (!this.config.email_api_url) {
            eywa.warn('Email API not configured, skipping email notification')
            return
        }
        
        const subject = this.getEmailSubject(alert, target)
        const body = this.getEmailBody(alert, target)
        
        const response = await fetch(this.config.email_api_url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.config.email_api_key}`
            },
            body: JSON.stringify({
                to: target.notification_email,
                from: this.config.default_from,
                subject,
                html: body,
                text: this.stripHtml(body)
            })
        })
        
        if (!response.ok) {
            throw new Error(`Email API error: ${response.status} ${response.statusText}`)
        }
        
        return { channel: 'email', success: true }
    }
    
    /**
     * Send webhook notification
     */
    async sendWebhook(alert, target) {
        const payload = {
            timestamp: new Date().toISOString(),
            monitor: {
                name: target.name,
                url: target.url
            },
            alert: {
                type: alert.type,
                severity: alert.severity,
                message: alert.message,
                previous_value: alert.previous_value,
                current_value: alert.current_value,
                change_percentage: alert.change_percentage
            }
        }
        
        const response = await fetch(target.notification_webhook, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'User-Agent': 'EYWA-Monitor-Robot/1.0'
            },
            body: JSON.stringify(payload),
            timeout: 10000
        })
        
        if (!response.ok) {
            throw new Error(`Webhook error: ${response.status} ${response.statusText}`)
        }
        
        return { channel: 'webhook', success: true }
    }
    
    /**
     * Generate email subject
     */
    getEmailSubject(alert, target) {
        const emoji = {
            PRICE_DROP: '💰',
            PRICE_INCREASE: '📈',
            BACK_IN_STOCK: '✅',
            OUT_OF_STOCK: '❌',
            CONTENT_CHANGED: '🔄',
            SITE_DOWN: '🚨',
            SITE_UP: '✅'
        }
        
        return `${emoji[alert.type] || '📢'} ${target.name}: ${alert.type.replace(/_/g, ' ')}`
    }
    
    /**
     * Generate email body
     */
    getEmailBody(alert, target) {
        const severityColor = {
            INFO: '#17a2b8',
            WARNING: '#ffc107',
            CRITICAL: '#dc3545'
        }
        
        return `
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: ${severityColor[alert.severity] || '#007bff'}; color: white; padding: 20px; border-radius: 5px 5px 0 0; }
        .content { background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; }
        .footer { margin-top: 20px; padding: 10px; text-align: center; color: #6c757d; font-size: 12px; }
        .value-change { display: flex; align-items: center; gap: 10px; margin: 10px 0; }
        .value { background: white; padding: 8px 12px; border-radius: 3px; border: 1px solid #dee2e6; }
        .arrow { font-size: 20px; color: ${alert.type.includes('DROP') ? '#28a745' : '#dc3545'}; }
        .button { display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 3px; margin-top: 15px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h2 style="margin: 0;">${alert.type.replace(/_/g, ' ')}</h2>
            <p style="margin: 5px 0 0 0;">${target.name}</p>
        </div>
        <div class="content">
            <p><strong>Alert:</strong> ${alert.message}</p>
            
            ${alert.previous_value && alert.current_value ? `
            <div class="value-change">
                <span class="value">${alert.previous_value}</span>
                <span class="arrow">→</span>
                <span class="value">${alert.current_value}</span>
                ${alert.change_percentage ? `<span>(${alert.change_percentage > 0 ? '+' : ''}${alert.change_percentage.toFixed(2)}%)</span>` : ''}
            </div>
            ` : alert.current_value ? `
            <p><strong>Current Value:</strong> <span class="value">${alert.current_value}</span></p>
            ` : ''}
            
            <p><strong>Monitored URL:</strong> <a href="${target.url}">${target.url}</a></p>
            <p><strong>Alert Time:</strong> ${new Date().toLocaleString()}</p>
            
            <a href="${target.url}" class="button">View Product</a>
        </div>
        <div class="footer">
            <p>This alert was generated by EYWA Monitor Robot</p>
            <p>To modify alert settings, please update your monitoring rules.</p>
        </div>
    </div>
</body>
</html>
        `.trim()
    }
    
    /**
     * Strip HTML tags for plain text version
     */
    stripHtml(html) {
        return html
            .replace(/<[^>]*>/g, '')
            .replace(/\s+/g, ' ')
            .trim()
    }
}

export default NotificationSender
