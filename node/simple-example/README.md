# Simple EYWA Robot Examples for Node.js

This directory contains straightforward examples of EYWA robots in Node.js, demonstrating core concepts without unnecessary complexity.

## Examples

### 1. Basic Robot (`index.js`)
The simplest possible EYWA robot that shows the complete lifecycle:
- Opening communication pipe
- Getting task input
- Processing data
- Reporting results
- Closing the task

**Key concepts:**
- `eywa.open_pipe()` - Start communication
- `eywa.get_task()` - Receive task data
- `eywa.info()` - Log information
- `eywa.report()` - Send results
- `eywa.close_task()` - Complete the task

### 2. User Greeter (`user-greeter.js`)
Demonstrates basic GraphQL operations:
- Querying data
- Updating records
- Error handling

**Key concepts:**
- `eywa.graphql()` - Execute GraphQL operations
- Query syntax for reading data
- Mutation syntax for updating data
- Proper error handling

### 3. Batch Processor (`batch-processor.js`)
Shows advanced patterns:
- Processing data in batches
- Progress reporting
- Retry logic for resilience

**Key concepts:**
- Batch processing pattern
- `eywa.update_task()` with progress
- Retry mechanism with exponential backoff
- Intermediate progress reporting

## Setup

1. Install dependencies:
```bash
npm install
```

2. Test locally with EYWA:
```bash
# Basic robot
eywa run -c "node index.js" --task-json '{"input": {"name": "John", "message": "Hello EYWA!"}}'

# User greeter (requires User entity in your EYWA model)
eywa run -c "node user-greeter.js" --task-json '{"input": {"userId": "user-uuid-here", "greeting": "Welcome!"}}'

# Batch processor
eywa run -c "node batch-processor.js" --task-json '{"input": {"items": [{"id": 1}, {"id": 2}, {"id": 3}], "batchSize": 2}}'
```

## Robot Declaration

Add these to your `robotics.graphql`:

```graphql
type Mutation {
  # Basic example
  sayHello(
    name: String
    @label(value: "Your Name")
    
    message: String
    @label(value: "Custom Message")
  ): STDResult
  @robot(
    euuid: "hello-robot-001"
    name: "Hello Robot"
    task_message: "Saying hello to {{name}}"
  )
  @form_input
  @execute(commands: [
    "cd node/simple-example"
    "npm install"
    "node index.js"
  ])
  
  # User greeter
  greetUser(
    userId: ID!
    @label(value: "User ID")
    
    greeting: String
    @label(value: "Custom Greeting")
  ): STDResult
  @robot(
    euuid: "user-greeter-001"
    name: "User Greeter"
    task_message: "Greeting user {{userId}}"
  )
  @form_input
  @execute(commands: [
    "cd node/simple-example"
    "npm install"
    "node user-greeter.js"
  ])
  
  # Batch processor
  processBatch(
    batchSize: Int = 10
    @label(value: "Batch Size")
  ): STDResult
  @robot(
    euuid: "batch-processor-001"
    name: "Batch Processor"
    task_message: "Processing in batches of {{batchSize}}"
  )
  @table_input(entity: "ItemToProcess")
  @execute(commands: [
    "cd node/simple-example"
    "npm install"
    "node batch-processor.js"
  ])
}
```

## Common Patterns

### Error Handling
```javascript
try {
    // Your robot logic
} catch (error) {
    eywa.error('Description', { error: error.message })
    eywa.close_task(eywa.ERROR)
}
```

### Logging
```javascript
eywa.info('What happened', { key: 'value' })  // Information
eywa.warn('Warning message', { data })       // Warnings
eywa.error('Error message', { error })       // Errors
```

### Task Status Updates
```javascript
eywa.update_task(eywa.PROCESSING)           // Simple status
eywa.update_task(eywa.PROCESSING, { progress: 50 })  // With progress
```

## Tips

1. **Always close tasks** - Use `eywa.close_task()` in both success and error cases
2. **Log appropriately** - Use info for progress, warn for recoverable issues, error for failures
3. **Test locally first** - Use `eywa run` command before deploying
4. **Handle errors gracefully** - Always wrap main logic in try/catch
5. **Keep it simple** - Start with basic functionality, add complexity as needed
