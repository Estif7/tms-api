using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(TmsDbContext context) : ControllerBase
{
    // Task 1: Paginated list of students (Page size: 20)
    [HttpGet("students")]
    public async Task<IActionResult> GetPaginatedStudents([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        const int pageSize = 20;

        // CRITICAL: Always OrderBy before Skip/Take for a stable PostgreSQL sort!
        var students = await context.Students
            .OrderBy(s => s.Name) 
            .Skip((page - 1) * pageSize) 
            .Take(pageSize)
            .Select(s => new { s.Id, s.Name, s.RegistrationNumber, s.GPA })
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    // Task 2: Top 5 courses by enrollment count
    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses(CancellationToken cancellationToken = default)
    {
        var topCourses = await context.Courses
            .Select(c => new
            {
                CourseId = c.Id,
                Title = c.Title,
                EnrollmentCount = c.Enrollments.Count // EF Core translates this to an aggregate subquery or join count
            })
            .OrderByDescending(c => c.EnrollmentCount)
            .Take(5) // Translates to LIMIT 5 in SQL
            .ToListAsync(cancellationToken);

        return Ok(topCourses);
    }
}