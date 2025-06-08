# Go System Monitor - EYWA Robot Example

## Overview

This EYWA robot demonstrates real-time system monitoring using Go, showcasing how to build a high-performance RPA (Robotic Process Automation) solution that collects and analyzes system metrics without requiring any external setup or API keys.

## What This Robot Does

The system monitor robot performs the following actions:

1. **Collects Real-Time System Metrics**
   - CPU usage percentage (overall and per-core)
   - Memory usage (used, available, percentage)
   - Disk usage for all mounted partitions
   - System load averages (1, 5, 15 minutes)
   - Process count and top processes by CPU/memory

2. **Analyzes Trends**
   - Detects anomalies (CPU spikes, memory leaks)
   - Identifies resource-hungry processes
   - Tracks usage patterns over time

3. **Stores Data in EYWA**
   - Creates structured logs with `TaskLog` entities
   - Generates alerts as `Task` entities for critical conditions
   - Maintains historical data for trend analysis

4. **Provides Actionable Insights**
   - Automatic alerts when thresholds exceeded
   - Process recommendations for optimization
   - Resource usage reports with visualizations

## Why Go for System Monitoring?

Go is the ideal language for this use case because:

1. **Concurrency Model** - Goroutines allow simultaneous monitoring of multiple system aspects without blocking
2. **Low Resource Overhead** - Minimal memory footprint, crucial for a monitoring tool
3. **Native System Integration** - Excellent OS-level integration through packages like `gopsutil`
4. **Fast Execution** - Compiled language ensures rapid data collection
5. **Cross-Platform** - Single codebase works on Linux, macOS, and Windows

## Technologies Used

### Core Technologies
- **Go 1.21+** - Modern Go with enhanced performance
- **EYWA Go Client** - For robot communication and GraphQL operations
- **gopsutil/v3** - Cross-platform system monitoring library

### Go Packages
```go
github.com/shirou/gopsutil/v3/cpu     // CPU metrics
github.com/shirou/gopsutil/v3/mem     // Memory information
github.com/shirou/gopsutil/v3/disk    // Disk usage
github.com/shirou/gopsutil/v3/load    // System load
github.com/shirou/gopsutil/v3/process // Process information
github.com/shirou/gopsutil/v3/host    // Host/OS information
```

### EYWA Integration
- **JSON-RPC 2.0** - Communication protocol via stdin/stdout
- **GraphQL** - Data persistence and querying
- **Task Management** - Alert creation and tracking

## Project Structure

```
go-system-monitor/
├── README.md                 # This file
├── go.mod                    # Go module definition
├── go.sum                    # Dependency checksums
├── main.go                   # Main robot implementation
├── monitor/
│   ├── collector.go          # Metrics collection logic
│   ├── analyzer.go           # Anomaly detection
│   └── types.go              # Data structures
├── robotics.graphql          # Robot declaration for EYWA
└── test-task.json            # Sample task for local testing
```

## How It Works

### 1. Data Collection Phase
The robot uses goroutines to collect metrics concurrently:
```go
var wg sync.WaitGroup
wg.Add(4)
go collectCPUMetrics(&metrics, &wg)
go collectMemoryMetrics(&metrics, &wg)
go collectDiskMetrics(&metrics, &wg)
go collectProcessMetrics(&metrics, &wg)
wg.Wait()
```

### 2. Analysis Phase
- Compares current metrics against historical baselines
- Detects anomalies using standard deviation
- Identifies top resource consumers

### 3. EYWA Storage Phase
- Stores detailed logs for historical analysis
- Creates user-actionable tasks for critical alerts
- Updates dashboards with latest metrics

## Key Features

### Real-Time Monitoring
- Collects metrics every 30 seconds (configurable)
- Near-zero latency between collection and storage
- Handles system metric spikes gracefully

### Intelligent Alerting
- CPU usage > 80% for 5+ minutes
- Memory usage > 90%
- Disk space < 10% free
- Unusual process behavior

### Cross-Platform Support
- **Linux**: Full support including cgroups
- **macOS**: All metrics including M1/M2 chips
- **Windows**: Complete Windows-specific metrics

## Usage Examples

### Basic Monitoring
```bash
# Run once to collect current metrics
eywa run -c 'go run main.go'
```

### Continuous Monitoring
```bash
# Run with custom interval (in seconds)
eywa run --task-json '{"input": {"interval": 60}}' -c 'go run main.go'
```

### Threshold-Based Monitoring
```bash
# Set custom alert thresholds
eywa run --task-json '{"input": {"cpu_threshold": 70, "memory_threshold": 85}}' -c 'go run main.go'
```

## Sample Output

The robot generates structured data in EYWA:

```json
{
  "message": "System metrics collected",
  "event": "INFO",
  "data": {
    "cpu": {
      "usage_percent": 45.2,
      "cores": 8,
      "per_core": [52.1, 41.3, 48.7, 39.2, 44.8, 51.3, 42.9, 41.3]
    },
    "memory": {
      "total_gb": 16.0,
      "used_gb": 9.8,
      "available_gb": 6.2,
      "percent": 61.3
    },
    "disk": {
      "/": {"total_gb": 500, "used_gb": 234, "percent": 46.8},
      "/data": {"total_gb": 1000, "used_gb": 678, "percent": 67.8}
    },
    "top_processes": [
      {"name": "chrome", "cpu_percent": 12.3, "memory_mb": 523},
      {"name": "docker", "cpu_percent": 8.7, "memory_mb": 412}
    ]
  }
}
```

## Benefits for RPA Showcase

1. **Immediate Value** - Works on any system without configuration
2. **Visual Impact** - Real metrics that change in real-time
3. **Business Relevance** - Infrastructure monitoring is universally needed
4. **Extensibility** - Easy to add custom metrics or integrations
5. **Performance Demo** - Shows Go's concurrency advantages

## Next Steps

This example can be extended to:
- Send notifications to Slack/Email when alerts trigger
- Create visual dashboards using EYWA's UI capabilities
- Integrate with cloud monitoring services
- Add predictive analytics for capacity planning
- Monitor specific applications or services

## Testing the Robot

1. Install dependencies:
   ```bash
   cd go-system-monitor
   go mod download
   ```

2. Test locally:
   ```bash
   eywa run -c 'go run main.go'
   ```

3. Deploy to EYWA:
   ```bash
   git add .
   git commit -m "Add system monitor robot"
   git push
   ```

## Why This Showcases RPA Excellence

- **Zero Configuration** - Works immediately on any system
- **Real Data** - Not mock data, actual system metrics
- **Business Value** - Prevents downtime, optimizes resources
- **Technical Excellence** - Demonstrates Go's strengths
- **Scalable** - Can monitor one machine or thousands

This robot proves that RPA doesn't need complex setups or external dependencies to provide immediate, tangible value.