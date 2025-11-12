# 🤖 Hello World C# Robot

**A simple EYWA robot demonstration in C#** - Following the CSHARP_GUIDE.md principles! 🚀

This robot demonstrates the **fundamental EYWA C# client patterns** according to the official guide:
- ✅ **Dynamic-first approach** using `Dictionary<string, object>`
- ✅ **Direct GraphQL** queries (no abstraction layers)  
- ✅ **Protocol abstraction only** (S3, JSON-RPC handled by client)
- ✅ **Proper resource management** with `using` statements
- ✅ **Task lifecycle management**
- ✅ **Structured logging** with contextual data
- ✅ **Report generation** with rich content

## 🎯 What This Robot Does

1. **Gets a task** from EYWA with optional parameters
2. **Says hello** to a specified name with a custom message  
3. **Queries EYWA** using direct GraphQL (demonstrating guide patterns)
4. **Generates a comprehensive report** with greeting details
5. **Logs progress** with structured contextual data throughout execution

## 🏗 Key Architecture Points (Per CSHARP_GUIDE.md)

### ✅ **Dynamic Data Structures**
```csharp
// Uses Dictionary<string, object> - no DTOs, no type mapping layers
var task = await eywa.Tasks.GetTaskAsync();  // Returns Dictionary<string, object>
```

### ✅ **Direct GraphQL**  
```csharp
// Write GraphQL directly - full power, no query builders
var result = await eywa.GraphQLAsync(@"
    query WhoAmI {
        whoami { name email }
    }");
```

### ✅ **Protocol Abstraction Only**
- Client handles JSON-RPC over stdin/stdout
- No query abstraction - you write the GraphQL
- Full access to GraphQL features (filtering, aggregations, relationships)

### ✅ **Proper Resource Management**
```csharp
// Guide pattern: using statement for automatic disposal
using var eywa = new Eywa();
```

## 🚀 How to Run

### Prerequisites
- **.NET 9.0 SDK** installed  
- **EYWA CLI** connected to an EYWA server
- **EywaClient 0.2.1** package

### Quick Start

```bash
# Navigate to this robot
cd /Users/robi/dev/eywa-robots/csharp/hello-world

# Restore packages
dotnet restore

# Build the project  
dotnet build

# Run with EYWA integration
eywa run -c "cd csharp/hello-world" -c "dotnet run"
```

### Test with Custom Input

```bash
# Test with task input (dynamic Dictionary approach)
eywa run --task-json '{"data": {"name": "Robert", "customMessage": "Greetings"}}' \
         -c "cd csharp/hello-world" -c "dotnet run"

# Test with task file
cat > task.json << EOF
{
  "euuid": "$(uuidgen)",
  "data": {
    "name": "EYWA Developer",
    "customMessage": "Welcome to C# robotics"
  }
}
EOF

eywa run --task-file task.json -c "cd csharp/hello-world" -c "dotnet run"
```

## 📊 Input Parameters

The robot accepts these optional parameters in the `data` object:

- **`name`** (string): Who to greet (default: "World")
- **`customMessage`** (string): Custom greeting message (default: "Hello")

## 📋 Example Output

```
🤖 Hello World C# Robot started
✅ Task received and processing
🌟 Delivering greeting
📡 Checking EYWA connection...
👋 Connected successfully as: Robert
🎉 Greeting delivered successfully!
✅ Hello World robot finished successfully!
```

## 🎯 Guide Compliance Checklist

This implementation follows the **CSHARP_GUIDE.md** exactly:

### ✅ **Core Philosophy Adherence**
- [x] **Dynamic-first**: Uses `Dictionary<string, object>` for all data
- [x] **GraphQL-native**: Single map arguments, direct queries
- [x] **No translation layers**: What you write in GraphQL is what you pass
- [x] **Client-controlled**: No hidden abstractions

### ✅ **Architecture Patterns**  
- [x] **using statement**: Proper disposal pattern (`using var eywa = new Eywa()`)
- [x] **Dynamic access**: `task["data"]` and safe casting
- [x] **Direct GraphQL**: `eywa.GraphQLAsync()` not wrapped methods
- [x] **Structured logging**: Rich context objects in log calls

### ✅ **Error Handling**
- [x] **Try-catch with EYWA cleanup**: Proper exception handling
- [x] **Task status management**: Processing → Success/Error
- [x] **Resource cleanup**: Automatic disposal via using statement

### ✅ **Data Patterns**
- [x] **No DTOs**: Direct dictionary manipulation 
- [x] **Dynamic casting**: Safe type conversion patterns
- [x] **JSON-like access**: Natural property access patterns

## 🔧 Code Structure

### Program.cs
- **Main execution**: Entry point with proper `using` disposal pattern
- **SayHello()**: Core business logic demonstrating GraphQL patterns
- **Dynamic data access**: Safe dictionary operations throughout
- **Error handling**: Comprehensive try-catch with EYWA integration

### Key EYWA Client Patterns (From Guide)

```csharp
// Initialize with proper disposal
using var eywa = new Eywa();
eywa.OpenPipe();

// Get task input (Dictionary<string, object>)
var task = await eywa.Tasks.GetTaskAsync();

// Dynamic data access 
var inputData = task["data"] as Dictionary<string, object>;

// Direct GraphQL (no abstraction)
var result = await eywa.GraphQLAsync("query { whoami { name } }");

// Access results dynamically
var userData = result["data"]["whoami"] as Dictionary<string, object>;

// Structured logging
await eywa.Logger.InfoAsync("Message", new { context = "data" });

// Task lifecycle
await eywa.Tasks.UpdateTaskAsync(Status.Processing);
await eywa.Tasks.CloseTaskAsync(Status.Success);
```

## 🛠 Dependencies

```xml
<PackageReference Include="EywaClient" Version="0.2.1" />
```

## 🐛 Troubleshooting

### "EywaClient not found"
```bash
dotnet restore
dotnet build
```

### "No task data received"  
This happens when running without EYWA. The robot handles this gracefully using defaults.

### "GraphQL query failed"
Ensure you're connected to EYWA:
```bash
eywa status
eywa whoami
```

## 🚀 Next Steps

Once this Hello World robot works:

1. **Study the dynamic patterns** - Notice how everything uses `Dictionary<string, object>`
2. **Try custom GraphQL** - Add your own queries to explore EYWA data
3. **Build complex robots** - Use this as a template for more advanced automation
4. **Read the full guide** - `/mnt/user-data/uploads/CSHARP_GUIDE.md` has comprehensive patterns

## 🎓 Learning Objectives

This robot teaches the **core CSHARP_GUIDE.md principles**:

- ✅ **Dynamic-first development**: Embracing `Dictionary<string, object>`
- ✅ **GraphQL mastery**: Writing direct queries without abstraction  
- ✅ **Resource management**: Proper disposal and cleanup patterns
- ✅ **EYWA integration**: Task lifecycle, logging, and reporting
- ✅ **Error resilience**: Comprehensive exception handling
- ✅ **Protocol vs Query abstraction**: Understanding what the client provides

Perfect foundation for building production C# robots following EYWA best practices! 🎉

---

*Built with 💪 following the official CSHARP_GUIDE.md for the EYWA robotics platform*
