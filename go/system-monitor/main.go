package main

import (
	"encoding/json"
	"fmt"
	"log"
	"system-monitor/monitor"
	"time"

	eywa "github.com/neyho/eywa-go"
)

type TaskInput struct {
	Interval        int     `json:"interval"`
	CPUThreshold    float64 `json:"cpu_threshold"`
	MemoryThreshold float64 `json:"memory_threshold"`
	DiskThreshold   float64 `json:"disk_threshold"`
	RunOnce         bool    `json:"run_once"`
}

func main() {
	// Initialize EYWA pipe
	go eywa.OpenPipe()
	time.Sleep(100 * time.Millisecond)

	// Get task
	task, err := eywa.GetTask()
	if err != nil {
		eywa.Error("Failed to get task", map[string]interface{}{
			"error": err.Error(),
		})
		eywa.CloseTask(eywa.ERROR)
		return
	}

	// Update task status
	eywa.UpdateTask(eywa.PROCESSING)
	eywa.Info("Starting system monitoring", nil)

	// Parse task input
	taskData := task.(map[string]interface{})
	inputData := taskData["input"]
	
	var input TaskInput
	// Default values
	input.Interval = 30
	input.RunOnce = true
	
	// Parse input if provided
	if inputData != nil {
		inputBytes, _ := json.Marshal(inputData)
		json.Unmarshal(inputBytes, &input)
	}

	// Configure monitoring
	config := monitor.DefaultConfig()
	if input.CPUThreshold > 0 {
		config.CPUThreshold = input.CPUThreshold
	}
	if input.MemoryThreshold > 0 {
		config.MemoryThreshold = input.MemoryThreshold
	}
	if input.DiskThreshold > 0 {
		config.DiskThreshold = input.DiskThreshold
	}

	eywa.Info("Monitoring configuration", map[string]interface{}{
		"config": config,
		"interval": input.Interval,
		"run_once": input.RunOnce,
	})

	// Get system info
	sysInfo, err := monitor.GetSystemInfo()
	if err != nil {
		eywa.Warn("Failed to get system info", map[string]interface{}{
			"error": err.Error(),
		})
	} else {
		eywa.Info("System information", sysInfo)
	}

	// Initialize collector and analyzer
	collector := monitor.NewCollector(config)
	analyzer := monitor.NewAnalyzer(config)

	// Main monitoring loop
	iterations := 0
	startTime := time.Now()
	
	for {
		iterations++
		
		// Collect metrics
		metrics, err := collector.CollectMetrics()
		if err != nil {
			eywa.Error("Failed to collect metrics", map[string]interface{}{
				"error": err.Error(),
			})
			if input.RunOnce {
				eywa.CloseTask(eywa.ERROR)
				return
			}
			time.Sleep(time.Duration(input.Interval) * time.Second)
			continue
		}


		// Log metrics to EYWA
		err = logMetricsToEYWA(metrics)
		if err != nil {
			eywa.Warn("Failed to log metrics to EYWA", map[string]interface{}{
				"error": err.Error(),
			})
		}

		// Analyze metrics
		alerts := analyzer.AnalyzeMetrics(metrics)
		
		// Generate recommendations
		recommendations := analyzer.GenerateRecommendations(metrics, alerts)

		// Report current status
		topCPUProcesses := monitor.GetTopProcesses(metrics, false, 5)
		topMemProcesses := monitor.GetTopProcesses(metrics, true, 5)
		
		eywa.Report("System metrics collected", map[string]interface{}{
			"iteration": iterations,
			"timestamp": metrics.Timestamp,
			"cpu": map[string]interface{}{
				"usage_percent": fmt.Sprintf("%.1f", metrics.CPU.UsagePercent),
				"cores": metrics.CPU.Cores,
			},
			"memory": map[string]interface{}{
				"total_gb": fmt.Sprintf("%.1f", metrics.Memory.TotalGB),
				"used_gb": fmt.Sprintf("%.1f", metrics.Memory.UsedGB),
				"available_gb": fmt.Sprintf("%.1f", metrics.Memory.AvailableGB),
				"percent": fmt.Sprintf("%.1f", metrics.Memory.UsedPercent),
			},
			"disk_summary": getDiskSummary(metrics.Disk),
			"load": map[string]interface{}{
				"1min": fmt.Sprintf("%.2f", metrics.Load.Load1),
				"5min": fmt.Sprintf("%.2f", metrics.Load.Load5),
				"15min": fmt.Sprintf("%.2f", metrics.Load.Load15),
			},
			"top_cpu_processes": formatProcesses(topCPUProcesses),
			"top_memory_processes": formatProcesses(topMemProcesses),
			"alerts": len(alerts),
			"recommendations": recommendations,
		}, nil)

		// Process alerts
		if len(alerts) > 0 {
			for _, alert := range alerts {
				eywa.Warn(fmt.Sprintf("[%s] %s", alert.Category, alert.Message), map[string]interface{}{
					"level": alert.Level,
					"category": alert.Category,
					"value": alert.Value,
					"threshold": alert.Threshold,
				})

				// Create EYWA task for critical alerts
				if alert.Level == "critical" {
					err = createAlertTask(alert)
					if err != nil {
						eywa.Error("Failed to create alert task", map[string]interface{}{
							"error": err.Error(),
						})
					}
				}
			}
		}

		// Check if we should continue
		if input.RunOnce {
			break
		}

		// Wait for next iteration
		time.Sleep(time.Duration(input.Interval) * time.Second)
	}

	// Final summary
	eywa.Info("Monitoring completed", map[string]interface{}{
		"iterations": iterations,
		"duration": time.Since(startTime).String(),
	})

	eywa.CloseTask(eywa.SUCCESS)
}

func logMetricsToEYWA(metrics *monitor.SystemMetrics) error {
	// Store metrics as TaskLog
	mutation := `
		mutation($data: TaskLogInput) {
			syncTaskLog(data: $data) {
				euuid
				created
			}
		}
	`

	variables := map[string]interface{}{
		"data": map[string]interface{}{
			"event": "SYSTEM_METRICS",
			"message": "System metrics snapshot",
			"data": map[string]interface{}{
				"timestamp": metrics.Timestamp,
				"cpu": metrics.CPU,
				"memory": metrics.Memory,
				"disk": metrics.Disk,
				"load": metrics.Load,
				"top_processes": metrics.Processes,
			},
		},
	}

	result, err := eywa.GraphQL(mutation, variables)
	if err != nil {
		return err
	}

	log.Printf("Stored metrics: %v", result)
	return nil
}

func createAlertTask(alert monitor.Alert) error {
	// Create a task for critical alerts
	mutation := `
		mutation($data: TaskInput) {
			syncTask(data: $data) {
				euuid
				name
				created
			}
		}
	`

	variables := map[string]interface{}{
		"data": map[string]interface{}{
			"name": fmt.Sprintf("System Alert: %s", alert.Category),
			"description": alert.Message,
			"priority": "HIGH",
			"status": "OPEN",
			"data": map[string]interface{}{
				"alert_type": alert.Category,
				"level": alert.Level,
				"value": alert.Value,
				"threshold": alert.Threshold,
				"timestamp": alert.Timestamp,
			},
		},
	}

	_, err := eywa.GraphQL(mutation, variables)
	return err
}

func getDiskSummary(disks []monitor.DiskMetrics) []map[string]interface{} {
	summary := make([]map[string]interface{}, 0, len(disks))
	
	for _, disk := range disks {
		summary = append(summary, map[string]interface{}{
			"mount": disk.MountPoint,
			"total_gb": fmt.Sprintf("%.1f", disk.TotalGB),
			"used_gb": fmt.Sprintf("%.1f", disk.UsedGB),
			"percent": fmt.Sprintf("%.1f", disk.UsedPercent),
		})
	}
	
	return summary
}

func formatProcesses(processes []monitor.ProcessMetrics) []map[string]interface{} {
	formatted := make([]map[string]interface{}, 0, len(processes))
	
	for _, p := range processes {
		formatted = append(formatted, map[string]interface{}{
			"name": p.Name,
			"pid": p.PID,
			"cpu_percent": fmt.Sprintf("%.1f", p.CPUPercent),
			"memory_mb": fmt.Sprintf("%.1f", p.MemoryMB),
		})
	}
	
	return formatted
}
