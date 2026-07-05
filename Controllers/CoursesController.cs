using Microsoft.AspNetCore.Mvc;
using TmsApi.Services;
using TmsApi.Entities;
using TmsApi.Dtos;

namespace TmsApi.Controllers;

[ApiController] // Triggers model validation rules automatically
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    [HttpGet] // Bare [HttpGet] handles the root 'api/courses' path
    public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken ct)
    {
        // Passes the query parameters down to build the optimized SQL statement
        var result = await courseService.GetCoursesAsync(request, ct);
        return Ok(result);
    }
    
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);
        return course is not null ? Ok(course) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(CreateCourseRequest request, CancellationToken ct)
    {
        // Step 1: Pre-check if the course code is already taken
        if (await courseService.CodeExistsAsync(request.Code, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course code already exists",
                Detail = $"A course with code '{request.Code}' is already registered.",
                Status = StatusCodes.Status409Conflict
            }); // 
        }

        // Step 2: Happy path creation if unique
        var result = await courseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }
}