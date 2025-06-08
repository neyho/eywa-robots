package monitor

import "time"

// SystemMetrics holds all collected system metrics
type SystemMetrics struct {
	Timestamp time.Time        `json:"timestamp"`
	CPU       CPUMetrics       `json:"cpu"`
	Memory    MemoryMetrics    `json:"memory"`
	Disk      []DiskMetrics    `json:"disk"`
	Load      LoadMetrics      `json:"load"`
	Processes []ProcessMetrics `json:"processes"`
}

// CPUMetrics holds CPU-related metrics
type CPUMetrics struct {
	UsagePercent float64   `json:"usage_percent"`
	Cores        int       `json:"cores"`
	PerCore      []float64 `json:"per_core"`
}

// MemoryMetrics holds memory-related metrics
type MemoryMetrics struct {
	TotalGB      float64 `json:"total_gb"`
	UsedGB       float64 `json:"used_gb"`
	AvailableGB  float64 `json:"available_gb"`
	UsedPercent  float64 `json:"percent"`
	SwapTotalGB  float64 `json:"swap_total_gb"`
	SwapUsedGB   float64 `json:"swap_used_gb"`
	SwapPercent  float64 `json:"swap_percent"`
}

// DiskMetrics holds disk-related metrics for a single partition
type DiskMetrics struct {
	MountPoint   string  `json:"mount_point"`
	Device       string  `json:"device"`
	TotalGB      float64 `json:"total_gb"`
	UsedGB       float64 `json:"used_gb"`
	FreeGB       float64 `json:"free_gb"`
	UsedPercent  float64 `json:"percent"`
}

// LoadMetrics holds system load averages
type LoadMetrics struct {
	Load1  float64 `json:"load1"`
	Load5  float64 `json:"load5"`
	Load15 float64 `json:"load15"`
}

// ProcessMetrics holds metrics for a single process
type ProcessMetrics struct {
	PID          int32   `json:"pid"`
	Name         string  `json:"name"`
	CPUPercent   float64 `json:"cpu_percent"`
	MemoryMB     float64 `json:"memory_mb"`
	MemoryPercent float64 `json:"memory_percent"`
}

// Alert represents a system alert
type Alert struct {
	Level     string    `json:"level"` // "warning", "critical"
	Category  string    `json:"category"` // "cpu", "memory", "disk"
	Message   string    `json:"message"`
	Value     float64   `json:"value"`
	Threshold float64   `json:"threshold"`
	Timestamp time.Time `json:"timestamp"`
}

// Config holds monitoring configuration
type Config struct {
	CPUThreshold    float64 `json:"cpu_threshold"`
	MemoryThreshold float64 `json:"memory_threshold"`
	DiskThreshold   float64 `json:"disk_threshold"`
	TopProcessCount int     `json:"top_process_count"`
}

// DefaultConfig returns default monitoring configuration
func DefaultConfig() Config {
	return Config{
		CPUThreshold:    80.0,
		MemoryThreshold: 90.0,
		DiskThreshold:   90.0,
		TopProcessCount: 10,
	}
}
