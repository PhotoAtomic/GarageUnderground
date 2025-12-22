using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using System.Text;

namespace GarageUnderground.Api;

/// <summary>
/// Diagnostic endpoints for troubleshooting deployment issues.
/// </summary>
public static class DiagnosticEndpoints
{
    public static void MapDiagnosticEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/diagnostics");

        group.MapGet("/storage", GetStorageDiagnostics)
            .WithName("GetStorageDiagnostics");

        group.MapGet("/file-operations", TestFileOperations)
            .WithName("TestFileOperations");
    }

    private static IResult TestFileOperations(IConfiguration configuration, ILogger<Program> logger)
    {
        var connectionString = configuration.GetValue<string>("LiteDb:ConnectionString")
            ?? "Filename=/app/data/garageunderground.db;Connection=shared";
        
        var filename = ExtractFilename(connectionString);
        var directory = Path.GetDirectoryName(filename) ?? "/app/data";
        
        var results = new List<object>();
        
        // Test 1: Create a new file
        var testFile1 = Path.Combine(directory, $"test-create-{Guid.NewGuid()}.txt");
        try
        {
            File.WriteAllText(testFile1, "test content");
            results.Add(new
            {
                operation = "CREATE",
                file = testFile1,
                success = true,
                fileExists = File.Exists(testFile1),
                message = "File created successfully"
            });
        }
        catch (Exception ex)
        {
            results.Add(new
            {
                operation = "CREATE",
                file = testFile1,
                success = false,
                error = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
        
        // Test 2: Update existing file
        var testFile2 = Path.Combine(directory, $"test-update-{Guid.NewGuid()}.txt");
        try
        {
            File.WriteAllText(testFile2, "initial content");
            System.Threading.Thread.Sleep(100); // Small delay
            File.WriteAllText(testFile2, "updated content");
            var content = File.ReadAllText(testFile2);
            
            results.Add(new
            {
                operation = "UPDATE",
                file = testFile2,
                success = true,
                contentMatches = content == "updated content",
                message = "File updated successfully"
            });
        }
        catch (Exception ex)
        {
            results.Add(new
            {
                operation = "UPDATE",
                file = testFile2,
                success = false,
                error = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
        
        // Test 3: Append to existing file
        var testFile3 = Path.Combine(directory, $"test-append-{Guid.NewGuid()}.txt");
        try
        {
            File.WriteAllText(testFile3, "line1\n");
            File.AppendAllText(testFile3, "line2\n");
            var content = File.ReadAllText(testFile3);
            
            results.Add(new
            {
                operation = "APPEND",
                file = testFile3,
                success = true,
                lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                message = "File appended successfully"
            });
        }
        catch (Exception ex)
        {
            results.Add(new
            {
                operation = "APPEND",
                file = testFile3,
                success = false,
                error = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
        
        // Test 4: Delete file
        var testFile4 = Path.Combine(directory, $"test-delete-{Guid.NewGuid()}.txt");
        try
        {
            File.WriteAllText(testFile4, "to be deleted");
            var existsBefore = File.Exists(testFile4);
            File.Delete(testFile4);
            var existsAfter = File.Exists(testFile4);
            
            results.Add(new
            {
                operation = "DELETE",
                file = testFile4,
                success = true,
                existedBefore = existsBefore,
                existsAfter = existsAfter,
                message = "File deleted successfully"
            });
        }
        catch (Exception ex)
        {
            results.Add(new
            {
                operation = "DELETE",
                file = testFile4,
                success = false,
                error = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
        
        // Test 5: FileStream with write
        var testFile5 = Path.Combine(directory, $"test-stream-{Guid.NewGuid()}.txt");
        try
        {
            using (var fs = new FileStream(testFile5, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes("stream write test");
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
            }
            
            var content = File.ReadAllText(testFile5);
            results.Add(new
            {
                operation = "FILESTREAM_WRITE",
                file = testFile5,
                success = true,
                contentMatches = content == "stream write test",
                message = "FileStream write successful"
            });
        }
        catch (Exception ex)
        {
            results.Add(new
            {
                operation = "FILESTREAM_WRITE",
                file = testFile5,
                success = false,
                error = ex.Message,
                exceptionType = ex.GetType().Name
            });
        }
        
        // Test 6: FileStream with ReadWrite (like LiteDB does)
        var testFile6 = Path.Combine(directory, $"test-readwrite-{Guid.NewGuid()}.txt");
        try
        {
            using (var fs = new FileStream(testFile6, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var bytes = Encoding.UTF8.GetBytes("initial");
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
                
                // Try to modify
                fs.Seek(0, SeekOrigin.Begin);
                var bytes2 = Encoding.UTF8.GetBytes("updated");
                fs.Write(bytes2, 0, bytes2.Length);
                fs.Flush();
            }
            
            var content = File.ReadAllText(testFile6);
            results.Add(new
            {
                operation = "FILESTREAM_READWRITE",
                file = testFile6,
                success = true,
                contentMatches = content.StartsWith("updated"),
                message = "FileStream ReadWrite successful"
            });
        }
        catch (Exception ex)
        {
            results.Add(new
            {
                operation = "FILESTREAM_READWRITE",
                file = testFile6,
                success = false,
                error = ex.Message,
                exceptionType = ex.GetType().Name,
                stackTrace = ex.StackTrace
            });
        }
        
        // Test 7: Test on actual database file if it exists
        if (File.Exists(filename))
        {
            try
            {
                var fileInfo = new FileInfo(filename);
                var canRead = (fileInfo.Attributes & FileAttributes.ReadOnly) == 0;
                
                // Try to open with ReadWrite
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    // Try to write a byte at the end
                    fs.Seek(0, SeekOrigin.End);
                    fs.WriteByte(0x00);
                    fs.SetLength(fs.Length - 1); // Remove the byte we added
                    fs.Flush();
                }
                
                results.Add(new
                {
                    operation = "ACTUAL_DB_FILE_WRITE",
                    file = filename,
                    success = true,
                    message = "Can write to actual database file"
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    operation = "ACTUAL_DB_FILE_WRITE",
                    file = filename,
                    success = false,
                    error = ex.Message,
                    exceptionType = ex.GetType().Name,
                    stackTrace = ex.StackTrace
                });
            }
        }
        
        // Cleanup test files
        var cleanupResults = new List<string>();
        foreach (var result in results)
        {
            try
            {
                var fileProperty = result.GetType().GetProperty("file");
                if (fileProperty != null)
                {
                    var filePath = fileProperty.GetValue(result)?.ToString();
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) && filePath != filename)
                    {
                        File.Delete(filePath);
                        cleanupResults.Add($"Cleaned up: {Path.GetFileName(filePath)}");
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        var summary = new
        {
            timestamp = DateTime.UtcNow,
            directory = directory,
            databaseFile = filename,
            databaseFileExists = File.Exists(filename),
            totalTests = results.Count,
            successfulTests = results.Count(r => {
                var prop = r.GetType().GetProperty("success");
                return prop != null && (bool)prop.GetValue(r)!;
            }),
            results = results,
            cleanup = cleanupResults
        };
        
        return Results.Ok(summary);
    }

    private static IResult GetStorageDiagnostics(
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        var diagnostics = new
        {
            timestamp = DateTime.UtcNow,
            environment = new
            {
                os = RuntimeInformation.OSDescription,
                framework = RuntimeInformation.FrameworkDescription,
                processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                currentDirectory = Directory.GetCurrentDirectory(),
                environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            },
            database = GetDatabaseDiagnostics(configuration, logger),
            volumes = GetVolumeDiagnostics(logger)
        };

        return Results.Ok(diagnostics);
    }

    private static object GetDatabaseDiagnostics(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetValue<string>("LiteDb:ConnectionString")
            ?? "Filename=data/garageunderground.db;Connection=shared";

        var filename = ExtractFilename(connectionString);
        var fullPath = Path.GetFullPath(filename);
        var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;

        var result = new
        {
            connectionString,
            filename,
            fullPath,
            directory,
            directoryExists = Directory.Exists(directory),
            fileExists = File.Exists(fullPath),
            fileSize = File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0,
            canWriteToDirectory = TestDirectoryWrite(directory, logger)
        };

        return result;
    }

    private static object GetVolumeDiagnostics(ILogger logger)
    {
        var volumePaths = new[] { "/app/data", "/data", "/mnt/data", "/tmp" };
        var volumes = new List<object>();

        foreach (var path in volumePaths)
        {
            try
            {
                var exists = Directory.Exists(path);
                var canWrite = exists && TestDirectoryWrite(path, logger);

                volumes.Add(new
                {
                    path,
                    exists,
                    canWrite,
                    files = exists ? Directory.GetFiles(path).Length : 0,
                    directories = exists ? Directory.GetDirectories(path).Length : 0
                });
            }
            catch (Exception ex)
            {
                volumes.Add(new
                {
                    path,
                    exists = false,
                    canWrite = false,
                    error = ex.Message
                });
            }
        }

        return volumes;
    }

    private static bool TestDirectoryWrite(string directory, ILogger logger)
    {
        if (!Directory.Exists(directory))
            return false;

        var testFile = Path.Combine(directory, $".write-test-{Guid.NewGuid()}");
        try
        {
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cannot write to directory: {Directory}", directory);
            return false;
        }
    }

    private static string ExtractFilename(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed["Filename=".Length..];
            }
        }
        return "garageunderground.db";
    }
}
