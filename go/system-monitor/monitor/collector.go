package monitor

import (
	"fmt"
	"runtime"
	"sort"
	"sync"
	"time"

	"github.com/shirou/gopsutil/v3/cpu"
	"github.com/shirou/gopsutil/v3/disk"
	"github.com/shirou/gopsutil/v3/host"
	"github.com/shirou/gopsutil/v3/load"
	"github.com/shirou/gopsutil/v3/mem"
	"github.com/shirou/gopsutil/v3/process"
)

// Collector handles system metrics collection
type Collector struct {
	config Config
}

// NewCollector creates a new metrics collector
func NewCollector(config Config) *Collector {
	return &Collector{
		config: config,
	}
}

// CollectMetrics gathers all system metrics concurrently
func (c *Collector) CollectMetrics() (*SystemMetrics, error) {
	metrics := &SystemMetrics{
		Timestamp: time.Now(),
	}

	var wg sync.WaitGroup
	var mu sync.Mutex
	var errs []error

	// Collect CPU metrics
	wg.Add(1)
	go func() {
		defer wg.Done()
		if err := c.collectCPUMetrics(metrics, &mu); err != nil {
			mu.Lock()
			errs = append(errs, fmt.Errorf("cpu metrics: %w", err))
			mu.Unlock()
		}
	}()

	// Collect memory metrics
	wg.Add(1)
	go func() {
		defer wg.Done()
		if err := c.collectMemoryMetrics(metrics, &mu); err != nil {
			mu.Lock()
			errs = append(errs, fmt.Errorf("memory metrics: %w", err))
			mu.Unlock()
		}
	}()

	// Collect disk metrics
	wg.Add(1)
	go func() {
		defer wg.Done()
		if err := c.collectDiskMetrics(metrics, &mu); err != nil {
			mu.Lock()
			errs = append(errs, fmt.Errorf("disk metrics: %w", err))
			mu.Unlock()
		}
	}()

	// Collect load metrics
	wg.Add(1)
	go func() {
		defer wg.Done()
		if err := c.collectLoadMetrics(metrics, &mu); err != nil {
			mu.Lock()
			errs = append(errs, fmt.Errorf("load metrics: %w", err))
			mu.Unlock()
		}
	}()

	// Collect process metrics
	wg.Add(1)
	go func() {
		defer wg.Done()
		if err := c.collectProcessMetrics(metrics, &mu); err != nil {
			mu.Lock()
			errs = append(errs, fmt.Errorf("process metrics: %w", err))
			mu.Unlock()
		}
	}()

	wg.Wait()

	if len(errs) > 0 {
		return metrics, fmt.Errorf("collection errors: %v", errs)
	}

	return metrics, nil
}

func (c *Collector) collectCPUMetrics(metrics *SystemMetrics, mu *sync.Mutex) error {
	// Get overall CPU usage
	overallPercent, err := cpu.Percent(time.Second, false)
	if err != nil {
		return err
	}

	// Get per-core CPU usage
	perCorePercent, err := cpu.Percent(time.Second, true)
	if err != nil {
		return err
	}

	mu.Lock()
	metrics.CPU = CPUMetrics{
		UsagePercent: overallPercent[0],
		Cores:        runtime.NumCPU(),
		PerCore:      perCorePercent,
	}
	mu.Unlock()

	return nil
}

func (c *Collector) collectMemoryMetrics(metrics *SystemMetrics, mu *sync.Mutex) error {
	// Virtual memory
	vmStat, err := mem.VirtualMemory()
	if err != nil {
		return err
	}

	// Swap memory
	swapStat, err := mem.SwapMemory()
	if err != nil {
		return err
	}

	mu.Lock()
	metrics.Memory = MemoryMetrics{
		TotalGB:      float64(vmStat.Total) / (1024 * 1024 * 1024),
		UsedGB:       float64(vmStat.Used) / (1024 * 1024 * 1024),
		AvailableGB:  float64(vmStat.Available) / (1024 * 1024 * 1024),
		UsedPercent:  vmStat.UsedPercent,
		SwapTotalGB:  float64(swapStat.Total) / (1024 * 1024 * 1024),
		SwapUsedGB:   float64(swapStat.Used) / (1024 * 1024 * 1024),
		SwapPercent:  swapStat.UsedPercent,
	}
	mu.Unlock()

	return nil
}

func (c *Collector) collectDiskMetrics(metrics *SystemMetrics, mu *sync.Mutex) error {
	partitions, err := disk.Partitions(false)
	if err != nil {
		return err
	}

	var diskMetrics []DiskMetrics

	for _, partition := range partitions {
		usage, err := disk.Usage(partition.Mountpoint)
		if err != nil {
			continue // Skip inaccessible partitions
		}

		// Skip very small partitions (< 1GB)
		if usage.Total < 1024*1024*1024 {
			continue
		}

		diskMetrics = append(diskMetrics, DiskMetrics{
			MountPoint:  partition.Mountpoint,
			Device:      partition.Device,
			TotalGB:     float64(usage.Total) / (1024 * 1024 * 1024),
			UsedGB:      float64(usage.Used) / (1024 * 1024 * 1024),
			FreeGB:      float64(usage.Free) / (1024 * 1024 * 1024),
			UsedPercent: usage.UsedPercent,
		})
	}

	mu.Lock()
	metrics.Disk = diskMetrics
	mu.Unlock()

	return nil
}

func (c *Collector) collectLoadMetrics(metrics *SystemMetrics, mu *sync.Mutex) error {
	loadStat, err := load.Avg()
	if err != nil {
		return err
	}

	mu.Lock()
	metrics.Load = LoadMetrics{
		Load1:  loadStat.Load1,
		Load5:  loadStat.Load5,
		Load15: loadStat.Load15,
	}
	mu.Unlock()

	return nil
}

func (c *Collector) collectProcessMetrics(metrics *SystemMetrics, mu *sync.Mutex) error {
	processes, err := process.Processes()
	if err != nil {
		return err
	}

	var processMetrics []ProcessMetrics

	for _, p := range processes {
		name, _ := p.Name()
		if name == "" {
			continue
		}

		cpuPercent, err := p.CPUPercent()
		if err != nil {
			continue
		}

		memInfo, err := p.MemoryInfo()
		if err != nil {
			continue
		}

		memPercent, err := p.MemoryPercent()
		if err != nil {
			continue
		}

		processMetrics = append(processMetrics, ProcessMetrics{
			PID:           p.Pid,
			Name:          name,
			CPUPercent:    cpuPercent,
			MemoryMB:      float64(memInfo.RSS) / (1024 * 1024),
			MemoryPercent: float64(memPercent),
		})
	}

	// Sort by CPU usage and take top N
	sort.Slice(processMetrics, func(i, j int) bool {
		return processMetrics[i].CPUPercent > processMetrics[j].CPUPercent
	})

	if len(processMetrics) > c.config.TopProcessCount {
		processMetrics = processMetrics[:c.config.TopProcessCount]
	}

	mu.Lock()
	metrics.Processes = processMetrics
	mu.Unlock()

	return nil
}

// GetSystemInfo returns basic system information
func GetSystemInfo() (map[string]interface{}, error) {
	hostInfo, err := host.Info()
	if err != nil {
		return nil, err
	}

	return map[string]interface{}{
		"hostname":        hostInfo.Hostname,
		"platform":        hostInfo.Platform,
		"platform_family": hostInfo.PlatformFamily,
		"os":              hostInfo.OS,
		"kernel_version":  hostInfo.KernelVersion,
		"uptime_hours":    float64(hostInfo.Uptime) / 3600,
	}, nil
}
