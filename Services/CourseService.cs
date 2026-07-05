using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Dtos;

namespace TmsApi.Services;

public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Courses
            .AsNoTracking() // Saves memory since no modifications happen on read paths
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count)) // EF translates this safely into a SQL COUNT(*) subquery
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);

        // Re-fetch via GetByIdAsync to get the correct unified projection shape safely
        return (await GetByIdAsync(course.Id, ct))!;
    }
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        // 1. Initialize a no-tracking query expression baseline
        var query = context.Courses.AsNoTracking(); // [cite: 234, 235]

        // 2. Apply case-insensitive PostgreSQL ILike search filtering if provided
        if (!string.IsNullOrWhiteSpace(request.Search)) // [cite: 236]
        {
            var searchTerm = $"%{request.Search}%"; // [cite: 237, 240]
            query = query.Where(c => EF.Functions.ILike(c.Title, searchTerm) 
                                  || EF.Functions.ILike(c.Code, searchTerm)); // [cite: 237, 239, 242]
        }

        // 3. Compute the Total Record Count BEFORE applying skip/take limitations
        var totalCount = await query.CountAsync(ct); // [cite: 247, 250]

        // 4. Sort safely via an explicit column whitelist fallback filter
        query = request.OrderBy.Trim() switch // [cite: 254, 256, 320]
        {
            "Code" => request.Descending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
            "MaxCapacity" => request.Descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
            _ => request.Descending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title) // Safe fallback 
        };

        // 5. Apply Page Offsets, Record Take Constraints, and database-level Select Projections
        var items = await query
            .Skip((request.Page - 1) * request.PageSize) // [cite: 254, 270]
            .Take(request.PageSize) // [cite: 254, 271]
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count)) // [cite: 254, 272, 273]
            .ToListAsync(ct); // [cite: 264, 275]

        // 6. Return the packaged pagination metadata envelope
        return new PagedResponse<CourseResponseDto> // [cite: 276]
        {
            Items = items, // [cite: 276]
            TotalCount = totalCount, // [cite: 276]
            Page = request.Page, // [cite: 276]
            PageSize = request.PageSize // [cite: 276]
        };
    }
}