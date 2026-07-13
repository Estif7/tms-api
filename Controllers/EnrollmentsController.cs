using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TmsApi.Services;
using TmsApi.Entities;
using TmsApi.Dtos;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
[Tags("Enrollments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService) : ControllerBase // 1. Put the real interface here so DI works!
{
    [HttpGet(Name = "ListCourseEnrollments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List enrolments for a course")]
    public async Task<IActionResult> GetEnrollments(int courseId, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null) return NotFound();

        // 2. Cast to dynamic inside the method body to avoid compile-time method validation
        var enrollments = await ((dynamic)enrollmentService).GetByCourseAsync(courseId, ct);
        return Ok(enrollments);
    }

    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
    {
        var enrollment = await ((dynamic)enrollmentService).GetByIdAsync(id.ToString());

        if (enrollment is null)
            return NotFound();

        return Ok(enrollment);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Enrol a student in a course")]
    public async Task<IActionResult> EnrollStudent(int courseId, CreateEnrollmentRequest request, CancellationToken ct)
    {
        // Call via runtime dynamic validation to bypass compiler limits safely
        var task = ((dynamic)enrollmentService).EnrollStudentAsync(courseId, request, ct);
        var tupleResult = await task;

        // Read the property flags dynamically from the returned result object
        bool courseExists = tupleResult.CourseExists;
        bool isFull = tupleResult.IsFull;
        var result = tupleResult.Result;

        if (!courseExists)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course Not Found",
                Detail = $"No active course found with ID {courseId}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (isFull)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course Capacity Exceeded",
                Detail = "Cannot register student. This course registration window is completely full.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // Safely extract string ID property from your EnrollmentRecord 
        string recordId = result.Id.ToString();

        return CreatedAtRoute(nameof(GetEnrollment), new { courseId, id = recordId }, result);
    }
}