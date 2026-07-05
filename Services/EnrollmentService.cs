using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public async Task<(bool CourseExists, bool IsFull, TmsApi.Dtos.EnrollmentResponseDto? Result)> EnrollStudentAsync(
        int courseId, TmsApi.Dtos.CreateEnrollmentRequest request, CancellationToken ct)
    {
        var courseData = await context.Courses
            .AsNoTracking()
            .Select(c => new { c.Id, c.MaxCapacity, CurrentCount = c.Enrollments.Count })
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (courseData is null)
        {
            return (CourseExists: false, IsFull: false, Result: null);
        }

        if (courseData.CurrentCount >= courseData.MaxCapacity)
        {
            return (CourseExists: true, IsFull: true, Result: null);
        }

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Student {StudentId} enrolled in course {CourseId}", request.StudentId, courseId);

        // Matching the exact constructor order of EnrollmentResponseDto
        var dto = new EnrollmentResponseDto(
            enrollment.Id, 
            enrollment.CourseId, 
            enrollment.StudentId, 
            enrollment.EnrolledAt);

        return (CourseExists: true, IsFull: false, Result: dto);
    }
}