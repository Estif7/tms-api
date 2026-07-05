using Microsoft.AspNetCore.Mvc;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/enrollments")]
public class EnrollmentsController(TmsApi.Services.IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> EnrollStudent(int courseId, TmsApi.Dtos.CreateEnrollmentRequest request, CancellationToken ct)
    {
        var (courseExists, isFull, result) = await enrollmentService.EnrollStudentAsync(courseId, request, ct);

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
                Status = StatusCodes.Status404NotFound // Using 409 Conflict via standard
            });
        }

        return Created($"/api/courses/{courseId}/enrollments/{result!.Id}", result);
    }
}