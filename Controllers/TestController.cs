using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TmsApi.Data;
namespace TmsApi.Controllers;

using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (nodatabase contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);
        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);
        Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");
        var results = orderedQuery.ToList(); // Execution is triggered here
        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
        return Ok(results);
    }
    // Non-translatable helper method
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }
    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students = context.Students
            .Where(s => s.GPA >= 3.5m)
            .ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }


    // [HttpGet("n-plus-one-fixed")]
    // public async Task<IActionResult> TestFixedQuery(CancellationToken cancellationToken)
    // {
    //     Console.WriteLine("\n>>> [PART B] Starting High-Performance Shaped Projection...");

    //     // Single unified round-trip query compiling the count on the Postgres side
    //     var report = await context.Students
    //         .AsNoTracking()
    //         .Select(s => new
    //         {
    //             s.Name,
    //             EnrollmentCount = s.Enrollments.Count // Translates into a SQL SELECT COUNT(*) subquery
    //         })
    //         .ToListAsync(cancellationToken);

    //     foreach (var r in report)
    //     {
    //         Console.WriteLine($"Student: {r.Name} | Enrollments: {r.EnrollmentCount}");
    //     }

    //     return Ok(report);
    // }

    [HttpGet("n-plus-one-fixed")]
    public async Task<IActionResult> TestFixedQuery(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> [PART B] Starting High-Performance Shaped Projection...");

        // Single unified round-trip query compiling the count on the Postgres side
        var report = await context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count // Translates into a SQL SELECT COUNT(*) subquery
            })
            .ToListAsync(cancellationToken);

        foreach (var r in report)
        {
            Console.WriteLine($"Student: {r.Name} | Enrollments: {r.EnrollmentCount}");
        }

        return Ok(report);
    }

    [HttpPost("archive-old-enrollments")]
    public async Task<IActionResult> ArchiveEnrollments([FromQuery] DateTime cutoffDate, CancellationToken cancellationToken)
    {
        Console.WriteLine("\n>>> Executing high-performance set-based bulk update...");

        // Directly targets the database without tracking or loading rows into application RAM
        int rowsAffected = await context.Enrollments
            .Where(e => e.EnrolledAt < cutoffDate && !e.IsArchived)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsArchived, true), cancellationToken);

        return Ok(new { Message = "Bulk archiving completed successfully.", RowsArchived = rowsAffected });
    }
}

