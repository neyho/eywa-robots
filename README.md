# EYWA Robots Showcase

This project demonstrates the EYWA robotics approach for building serverless automation functions.

## Overview

EYWA robots are serverless functions that:
- Communicate via JSON-RPC 2.0 over stdin/stdout
- Execute tasks with full GraphQL API access
- Support multiple programming languages (Python, Node.js, Ruby, Go, Babashka/Clojure)
- Can be scheduled, triggered, or executed on-demand

## Project Structure

```
eywa-robots/
├── README.md
├── robotics.graphql      # Robot declarations
├── python/               # Python robot examples
├── node/                 # Node.js robot examples
├── babashka/            # Babashka/Clojure robot examples
├── ruby/                # Ruby robot examples
└── go/                  # Go robot examples
```

## Key Features Demonstrated

1. **Multi-language Support**: Examples in all supported languages
2. **GraphQL Integration**: Direct data model access
3. **Task Lifecycle**: Processing states and error handling
4. **Batch Processing**: Efficient handling of large datasets
5. **Local Testing**: Using `eywa run` for development

## Getting Started

1. Connect to your EYWA instance:
   ```bash
   eywa connect <YOUR_EYWA_URL>
   ```

2. Test a robot locally:
   ```bash
   eywa run -c 'python python/hello_robot.py'
   ```

3. Deploy by pushing to your Git repository

## Documentation

For detailed documentation, see the EYWA Development Guide.
