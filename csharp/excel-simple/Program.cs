/// <summary>
/// Simple Excel Report Generator - EYWA Robot
/// 
/// Creates a basic Excel report with sample data and uploads to EYWA filesystem.
/// No COM, no complexity, just pure .NET that WORKS.
/// </summary>

using EywaClient;
using EywaClient.Core;
using OfficeOpenXml;

// EPPlus requires license context
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

using var eywa = new Eywa();

try
{
    eywa.OpenPipe();
    
    var task = await eywa.Tasks.GetTaskAsync();
    var taskId = task["euuid"]?.ToString() ?? Guid.NewGuid().ToString();
    
    await eywa.Logger.InfoAsync("📊 Excel Simple Report Generator started", new { taskId });
    
    await eywa.Tasks.UpdateTaskAsync(Status.Processing);

    // Get report title from task data
    string reportTitle = "Sales Report";
    if (task.ContainsKey("data") && task["data"] is Dictionary<string, object> inputData)
    {
        if (inputData.TryGetValue("title", out var titleValue))
            reportTitle = titleValue?.ToString() ?? "Sales Report";
    }

    // Ensure folder structure exists in EYWA
    await eywa.Logger.InfoAsync("📁 Creating folder structure in EYWA...");
    var folderUuid = await EnsureReportFolderExists(eywa);
    await eywa.Logger.InfoAsync($"✅ Folder ready: {folderUuid}");

    // Generate Excel file and upload to EYWA
    await eywa.Logger.InfoAsync("📝 Creating Excel file...");
    var (fileUuid, fileName, eywaPath) = await CreateExcelReport(eywa, reportTitle, taskId, folderUuid);
    
    await eywa.Logger.InfoAsync($"✅ Excel file uploaded to EYWA: {eywaPath}");

    // Report success
    await eywa.Tasks.ReportAsync("Excel Report Generated", new ReportOptions
    {
        Data = new ReportData
        {
            Card = $"""
                # 📊 Excel Report Generated Successfully

                **Report Title:** {reportTitle}
                **File:** {fileName}
                **EYWA Location:** {eywaPath}
                **File UUID:** `{fileUuid}`
                **Rows:** 5 data rows
                **Created:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

                ✅ Report stored in EYWA filesystem
                """,
            Tables = new Dictionary<string, TableData>
            {
                ["Sample Data"] = new TableData
                {
                    Headers = new[] { "Product", "Q1", "Q2", "Q3", "Q4", "Total" },
                    Rows = new object[][]
                    {
                        new object[] { "Widget A", 1000, 1200, 1100, 1300, 4600 },
                        new object[] { "Widget B", 800, 900, 950, 1000, 3650 },
                        new object[] { "Widget C", 1500, 1400, 1600, 1700, 6200 },
                        new object[] { "Widget D", 600, 700, 650, 800, 2750 },
                        new object[] { "Widget E", 1100, 1000, 1200, 1300, 4600 }
                    }
                }
            }
        }
    });

    await eywa.Tasks.CloseTaskAsync(Status.Success);
    Console.WriteLine("✅ Excel report generated successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    await eywa.Logger.ErrorAsync("Robot failed", new { error = ex.Message });
    await eywa.Tasks.CloseTaskAsync(Status.Error);
    throw;
}

/// <summary>
/// Find the deepest existing folder matching the target path.
/// Returns (deepestFolderUuid, matchedDepth) where matchedDepth is how many path components exist.
/// </summary>
static async Task<(string folderUuid, int matchedDepth)> FindDeepestMatchingFolder(
    Eywa eywa, string[] pathComponents)
{
    // Start checking from the full path down to root
    for (int depth = pathComponents.Length; depth > 0; depth--)
    {
        var checkPath = "/" + string.Join("/", pathComponents.Take(depth));
        
        // Use getFolder since path is unique
        var query = @"
            query GetFolder($path: String!) {
                getFolder(path: $path) {
                    euuid name path
                }
            }";
        
        try
        {
            var result = await eywa.GraphQLAsync(query, new { path = checkPath });
            var data = (Dictionary<string, object>)result["data"];
            
            // Check if folder exists
            if (data["getFolder"] != null && data["getFolder"] is not System.Text.Json.JsonElement jsonNull)
            {
                var folder = (Dictionary<string, object>)data["getFolder"];
                var folderUuid = folder["euuid"]?.ToString() ?? Guid.NewGuid().ToString();
                return (folderUuid, depth);
            }
        }
        catch
        {
            // Folder doesn't exist, try next depth
            continue;
        }
    }
    
    // Nothing found, start from root
    return (eywa.Files.RootUuid, 0);
}

/// <summary>
/// Ensure the full folder path /demo/csharp/excel-simple/YYYY/MM/ exists.
/// Finds deepest existing path, then creates only what's missing.
/// Returns the final month folder UUID.
/// </summary>
static async Task<string> EnsureReportFolderExists(Eywa eywa)
{
    var year = DateTime.Now.Year.ToString();
    var month = DateTime.Now.Month.ToString("D2");
    
    // Target path components
    var pathComponents = new[] { "demo", "csharp", "excel-simple", year, month };
    var targetPath = $"/demo/csharp/excel-simple/{year}/{month}";
    
    await eywa.Logger.InfoAsync($"📁 Ensuring folder exists: {targetPath}");
    
    // Find deepest existing folder
    var (parentUuid, matchedDepth) = await FindDeepestMatchingFolder(eywa, pathComponents);
    
    if (matchedDepth == pathComponents.Length)
    {
        // Full path exists!
        await eywa.Logger.InfoAsync($"✅ Folder structure already exists: {targetPath}");
        return parentUuid;
    }
    
    // Log what we found
    var existingPath = matchedDepth == 0 ? "/" : $"/{string.Join("/", pathComponents.Take(matchedDepth))}";
    var missingPath = string.Join("/", pathComponents.Skip(matchedDepth));
    await eywa.Logger.InfoAsync($"📂 Found: {existingPath}, Creating: {missingPath}");
    
    // Create missing folders from matchedDepth onwards
    var currentParent = parentUuid;
    for (int i = matchedDepth; i < pathComponents.Length; i++)
    {
        var folderName = pathComponents[i];
        var folderUuid = Guid.NewGuid().ToString();
        
        await eywa.Files.CreateFolderAsync(new Dictionary<string, object>
        {
            ["euuid"] = folderUuid,
            ["name"] = folderName,
            ["parent"] = new Dictionary<string, object> { ["euuid"] = currentParent }
        });
        
        await eywa.Logger.InfoAsync($"✅ Created: {folderName}", new { uuid = folderUuid });
        currentParent = folderUuid;
    }
    
    return currentParent;
}

/// <summary>
/// Create Excel report and upload to EYWA.
/// Returns (fileUuid, fileName, eywaPath).
/// </summary>
static async Task<(string fileUuid, string fileName, string eywaPath)> CreateExcelReport(
    Eywa eywa, string title, string taskId, string folderUuid)
{
    var fileName = $"report_{taskId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
    var year = DateTime.Now.Year.ToString();
    var month = DateTime.Now.Month.ToString("D2");
    var eywaPath = $"/demo/csharp/excel-simple/{year}/{month}/{fileName}";

    using var package = new ExcelPackage();
    var worksheet = package.Workbook.Worksheets.Add(title);

    // Header
    worksheet.Cells[1, 1].Value = title;
    worksheet.Cells[1, 1].Style.Font.Size = 16;
    worksheet.Cells[1, 1].Style.Font.Bold = true;

    // Column headers
    worksheet.Cells[3, 1].Value = "Product";
    worksheet.Cells[3, 2].Value = "Q1 Sales";
    worksheet.Cells[3, 3].Value = "Q2 Sales";
    worksheet.Cells[3, 4].Value = "Q3 Sales";
    worksheet.Cells[3, 5].Value = "Q4 Sales";
    worksheet.Cells[3, 6].Value = "Total";

    // Make headers bold
    using (var range = worksheet.Cells[3, 1, 3, 6])
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
    }

    // Sample data
    var data = new[]
    {
        new { Product = "Widget A", Q1 = 1000, Q2 = 1200, Q3 = 1100, Q4 = 1300 },
        new { Product = "Widget B", Q1 = 800, Q2 = 900, Q3 = 950, Q4 = 1000 },
        new { Product = "Widget C", Q1 = 1500, Q2 = 1400, Q3 = 1600, Q4 = 1700 },
        new { Product = "Widget D", Q1 = 600, Q2 = 700, Q3 = 650, Q4 = 800 },
        new { Product = "Widget E", Q1 = 1100, Q2 = 1000, Q3 = 1200, Q4 = 1300 }
    };

    // Fill data and calculate totals
    int row = 4;
    foreach (var item in data)
    {
        worksheet.Cells[row, 1].Value = item.Product;
        worksheet.Cells[row, 2].Value = item.Q1;
        worksheet.Cells[row, 3].Value = item.Q2;
        worksheet.Cells[row, 4].Value = item.Q3;
        worksheet.Cells[row, 5].Value = item.Q4;
        worksheet.Cells[row, 6].Formula = $"SUM(B{row}:E{row})";
        row++;
    }

    // Format numbers
    worksheet.Cells[4, 2, 8, 6].Style.Numberformat.Format = "#,##0";

    // Auto-fit columns
    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

    // Save to memory stream and upload to EYWA
    using var memoryStream = new MemoryStream();
    await package.SaveAsAsync(memoryStream);
    memoryStream.Position = 0; // Reset stream position for upload
    
    // Upload to EYWA
    var fileUuid = Guid.NewGuid().ToString();
    await eywa.Files.UploadStreamAsync(memoryStream, new Dictionary<string, object>
    {
        ["euuid"] = fileUuid,
        ["name"] = fileName,
        ["size"] = memoryStream.Length,
        ["folder"] = new Dictionary<string, object> { ["euuid"] = folderUuid },
        ["content_type"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    });
    
    return (fileUuid, fileName, eywaPath);
}