package monitor

import (
	"fmt"
)

// Analyzer handles anomaly detection and alert generation
type Analyzer struct {
	config        Config
	history       []SystemMetrics
	historyWindow int
}

// NewAnalyzer creates a new metrics analyzer
func NewAnalyzer(config Config) *Analyzer {
	return &Analyzer{
		config:        config,
		historyWindow: 10, // Keep last 10 measurements
		history:       make([]SystemMetrics, 0, 10),
	}
}

// AnalyzeMetrics analyzes metrics for anomalies and generates alerts
func (a *Analyzer) AnalyzeMetrics(metrics *SystemMetrics) []Alert {
	// Add to history
	a.addToHistory(metrics)

	var alerts []Alert

	// Check CPU usage
	if cpuAlert := a.checkCPUUsage(metrics); cpuAlert != nil {
		alerts = append(alerts, *cpuAlert)
	}

	// Check memory usage
	if memAlert := a.checkMemoryUsage(metrics); memAlert != nil {
		alerts = append(alerts, *memAlert)
	}

	// Check disk usage
	diskAlerts := a.checkDiskUsage(metrics)
	alerts = append(alerts, diskAlerts...)

	// Check for anomalies based on historical data
	if len(a.history) >= 5 {
		anomalyAlerts := a.detectAnomalies(metrics)
		alerts = append(alerts, anomalyAlerts...)
	}

	return alerts
}

func (a *Analyzer) addToHistory(metrics *SystemMetrics) {
	a.history = append(a.history, *metrics)
	if len(a.history) > a.historyWindow {
		a.history = a.history[1:]
	}
}

func (a *Analyzer) checkCPUUsage(metrics *SystemMetrics) *Alert {
	if metrics.CPU.UsagePercent > a.config.CPUThreshold {
		level := "warning"
		if metrics.CPU.UsagePercent > 95 {
			level = "critical"
		}

		// Check if sustained high CPU usage
		sustained := a.isSustainedHighCPU()
		message := fmt.Sprintf("CPU usage is %.1f%% (threshold: %.1f%%)", 
			metrics.CPU.UsagePercent, a.config.CPUThreshold)
		
		if sustained {
			message = fmt.Sprintf("Sustained high CPU usage: %.1f%% for %d measurements", 
				metrics.CPU.UsagePercent, len(a.history))
			level = "critical"
		}

		return &Alert{
			Level:     level,
			Category:  "cpu",
			Message:   message,
			Value:     metrics.CPU.UsagePercent,
			Threshold: a.config.CPUThreshold,
			Timestamp: metrics.Timestamp,
		}
	}
	return nil
}

func (a *Analyzer) checkMemoryUsage(metrics *SystemMetrics) *Alert {
	if metrics.Memory.UsedPercent > a.config.MemoryThreshold {
		level := "warning"
		if metrics.Memory.UsedPercent > 95 {
			level = "critical"
		}

		return &Alert{
			Level:     level,
			Category:  "memory",
			Message:   fmt.Sprintf("Memory usage is %.1f%% (%.1f GB / %.1f GB)", 
				metrics.Memory.UsedPercent, metrics.Memory.UsedGB, metrics.Memory.TotalGB),
			Value:     metrics.Memory.UsedPercent,
			Threshold: a.config.MemoryThreshold,
			Timestamp: metrics.Timestamp,
		}
	}
	return nil
}

func (a *Analyzer) checkDiskUsage(metrics *SystemMetrics) []Alert {
	var alerts []Alert

	for _, disk := range metrics.Disk {
		if disk.UsedPercent > a.config.DiskThreshold {
			level := "warning"
			if disk.UsedPercent > 95 {
				level = "critical"
			}

			alerts = append(alerts, Alert{
				Level:     level,
				Category:  "disk",
				Message:   fmt.Sprintf("Disk %s usage is %.1f%% (%.1f GB free)", 
					disk.MountPoint, disk.UsedPercent, disk.FreeGB),
				Value:     disk.UsedPercent,
				Threshold: a.config.DiskThreshold,
				Timestamp: metrics.Timestamp,
			})
		}
	}

	return alerts
}

func (a *Analyzer) isSustainedHighCPU() bool {
	if len(a.history) < 3 {
		return false
	}

	// Check if last 3 measurements all exceeded threshold
	count := 0
	for i := len(a.history) - 3; i < len(a.history); i++ {
		if a.history[i].CPU.UsagePercent > a.config.CPUThreshold {
			count++
		}
	}

	return count >= 3
}

func (a *Analyzer) detectAnomalies(current *SystemMetrics) []Alert {
	var alerts []Alert

	// Calculate average CPU usage from history
	avgCPU := a.calculateAverageCPU()
	cpuDelta := current.CPU.UsagePercent - avgCPU

	// Detect CPU spike (> 30% increase from average)
	if cpuDelta > 30 && current.CPU.UsagePercent > 50 {
		alerts = append(alerts, Alert{
			Level:     "warning",
			Category:  "cpu",
			Message:   fmt.Sprintf("CPU spike detected: %.1f%% (%.1f%% above average)", 
				current.CPU.UsagePercent, cpuDelta),
			Value:     current.CPU.UsagePercent,
			Threshold: avgCPU,
			Timestamp: current.Timestamp,
		})
	}

	// Detect memory leak pattern (consistently increasing memory usage)
	if a.isMemoryIncreasing() {
		alerts = append(alerts, Alert{
			Level:     "warning",
			Category:  "memory",
			Message:   "Potential memory leak detected: memory usage consistently increasing",
			Value:     current.Memory.UsedPercent,
			Threshold: a.config.MemoryThreshold,
			Timestamp: current.Timestamp,
		})
	}

	return alerts
}

func (a *Analyzer) calculateAverageCPU() float64 {
	if len(a.history) == 0 {
		return 0
	}

	sum := 0.0
	for _, metrics := range a.history {
		sum += metrics.CPU.UsagePercent
	}

	return sum / float64(len(a.history))
}

func (a *Analyzer) isMemoryIncreasing() bool {
	if len(a.history) < 4 {
		return false
	}

	// Check if memory usage has been increasing for last 4 measurements
	increasing := true
	for i := len(a.history) - 3; i < len(a.history); i++ {
		if a.history[i].Memory.UsedPercent <= a.history[i-1].Memory.UsedPercent {
			increasing = false
			break
		}
	}

	// Also check if the increase is significant (> 10% total)
	if increasing {
		firstMem := a.history[len(a.history)-4].Memory.UsedPercent
		lastMem := a.history[len(a.history)-1].Memory.UsedPercent
		return (lastMem - firstMem) > 10
	}

	return false
}

// GetTopProcesses returns the top N processes by CPU or memory usage
func GetTopProcesses(metrics *SystemMetrics, byMemory bool, count int) []ProcessMetrics {
	if count > len(metrics.Processes) {
		count = len(metrics.Processes)
	}

	processes := make([]ProcessMetrics, len(metrics.Processes))
	copy(processes, metrics.Processes)

	// Sort by the requested metric
	if byMemory {
		// Sort by memory usage
		for i := 0; i < len(processes); i++ {
			for j := i + 1; j < len(processes); j++ {
				if processes[j].MemoryMB > processes[i].MemoryMB {
					processes[i], processes[j] = processes[j], processes[i]
				}
			}
		}
	}
	// Already sorted by CPU if not byMemory

	return processes[:count]
}

// GenerateRecommendations generates recommendations based on system state
func (a *Analyzer) GenerateRecommendations(metrics *SystemMetrics, alerts []Alert) []string {
	var recommendations []string

	// High CPU recommendations
	for _, alert := range alerts {
		if alert.Category == "cpu" && alert.Level == "critical" {
			// Find top CPU consumers
			topProcesses := GetTopProcesses(metrics, false, 3)
			if len(topProcesses) > 0 {
				recommendations = append(recommendations, 
					fmt.Sprintf("Consider terminating or optimizing high CPU process: %s (%.1f%% CPU)", 
						topProcesses[0].Name, topProcesses[0].CPUPercent))
			}
		}

		// Low disk space recommendations
		if alert.Category == "disk" && alert.Level == "critical" {
			recommendations = append(recommendations, 
				"Critical: Clean up disk space immediately to prevent system issues")
			recommendations = append(recommendations, 
				"Run disk cleanup tools or remove unnecessary files")
		}

		// Memory recommendations
		if alert.Category == "memory" && alert.Level == "warning" {
			topMemProcesses := GetTopProcesses(metrics, true, 3)
			if len(topMemProcesses) > 0 {
				recommendations = append(recommendations, 
					fmt.Sprintf("High memory consumer: %s (%.1f MB)", 
						topMemProcesses[0].Name, topMemProcesses[0].MemoryMB))
			}
		}
	}

	return recommendations
}
