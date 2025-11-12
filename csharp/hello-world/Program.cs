/// <summary>
/// Hello World EYWA Robot - C# Edition
/// 
/// Demonstrates basic EYWA robot patterns according to CSHARP_GUIDE.md:
/// - Dynamic Dictionary<string, object> approach  
/// - Direct GraphQL queries
/// - Task lifecycle management
/// - Proper resource disposal
/// - Protocol abstraction (not query abstraction)
/// </summary>

using EywaClient;
using EywaClient.Core;

// Follow guide pattern: using statement for proper disposal
using var eywa = new Eywa();

try
{
    // Initialize EYWA communication pipe
    eywa.OpenPipe();
    
    // Get current task - returns Dictionary<string, object>
    var task = await eywa.Tasks.GetTaskAsync();
    await eywa.Logger.InfoAsync("Hello World C# Robot started", new { taskId = task["euuid"] });
    
    // Update task status to processing
    await eywa.Tasks.UpdateTaskAsync(Status.Processing);

    // Extract input parameters from task data - dynamic approach
    string name = "World";
    string customMessage = "Hello";
    
    // Follow guide: Use dynamic access patterns with Dictionary<string, object>
    if (task.ContainsKey("data") && task["data"] is Dictionary<string, object> inputData)
    {
        if (inputData.TryGetValue("name", out var nameValue))
            name = nameValue?.ToString() ?? "World";
            
        if (inputData.TryGetValue("customMessage", out var messageValue))
            customMessage = messageValue?.ToString() ?? "Hello";
    }

    // Core business logic
    await SayHello(eywa, name, customMessage);

    // Complete the task successfully
    await eywa.Tasks.CloseTaskAsync(Status.Success);
    Console.WriteLine("✅ Hello World robot finished successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    await eywa.Logger.ErrorAsync("Robot failed", new { error = ex.Message });
    await eywa.Tasks.CloseTaskAsync(Status.Error);
    throw;
}
// using statement automatically calls eywa.Dispose()

static async Task SayHello(Eywa eywa, string name, string customMessage)
{
    try
    {
        // Log the greeting with structured data
        var greeting = $"{customMessage}, {name}!";
        await eywa.Logger.InfoAsync("🌟 Delivering greeting", new { greeting = greeting });
        
        // Demonstrate direct GraphQL - following the guide's approach
        await eywa.Logger.InfoAsync("📡 Checking EYWA connection...");
        
        // Use direct GraphQL query to fetch some basic data to demonstrate connectivity
        // Let's search for active users to show the system is responsive
        var result = await eywa.GraphQLAsync(@"
            query TestConnection {
                searchUser(_where: { active: { _boolean: TRUE} }, _limit: 5) {
                    euuid
                    name
                    active
                    type
                }
            }");
        
        // Access results dynamically - just like JavaScript! (guide line 90-94)
        if (result["data"] is Dictionary<string, object> data && 
            data["searchUser"] is List<object> users && users.Count > 0)
        {
            var userCount = users.Count;
            var firstUser = users[0] as Dictionary<string, object>;
            var firstUserName = firstUser?["name"]?.ToString() ?? "Unknown";
            
            await eywa.Logger.InfoAsync("👋 Connected to EYWA successfully", new { 
                activeUsersFound = userCount,
                firstActiveUser = firstUserName,
                graphqlWorking = true
            });
        }
        else
        {
            await eywa.Logger.InfoAsync("ℹ️ No users found, but GraphQL connection is working...");
        }

        // Generate comprehensive report using the exact pattern from guide
        await eywa.Tasks.ReportAsync("Hello World Results", new ReportOptions
        {
            Data = new ReportData
            {
                Card = $"""
                    # 🤖 Hello World C# Robot Results
                    
                    ## Greeting Delivered Successfully! 🎉
                    
                    **Message:** {greeting}
                    
                    **Details:**
                    - **Target:** {name}
                    - **Custom Message:** {customMessage}
                    - **Timestamp:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                    - **Robot Language:** C#
                    - **Runtime:** .NET {Environment.Version}
                    - **Dynamic Approach:** Dictionary<string, object> ✅
                    - **GraphQL Connection:** Working ✅
                    
                    ✅ **Status:** Completed successfully
                    """,
                Tables = new Dictionary<string, TableData>
                {
                    ["Execution Summary"] = new TableData
                    {
                        Headers = ["Property", "Value"],
                        Rows = new object[][]
                        {
                            ["Greeting Target", name],
                            ["Custom Message", customMessage],
                            ["Execution Time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")],
                            ["Robot Language", "C# (.NET)"],
                            ["Data Approach", "Dynamic Dictionary<string, object>"],
                            ["GraphQL Style", "Direct queries (no abstraction)"],
                            ["Status", "SUCCESS"]
                        }
                    }
                }
            }
        });
        
        // Simulate processing time
        await Task.Delay(1000);
        
        await eywa.Logger.InfoAsync("🎉 Greeting delivered successfully!");
    }
    catch (Exception ex)
    {
        await eywa.Logger.ErrorAsync("Failed to say hello", new { error = ex.Message });
        throw;
    }
}
