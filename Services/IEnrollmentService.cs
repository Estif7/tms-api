using TmsApi.Dtos;

namespace TmsApi.Services;

public interface IEnrollmentService
{
    Task<(bool CourseExists, bool IsFull, TmsApi.Dtos.EnrollmentResponseDto? Result)> EnrollStudentAsync(
        int courseId, TmsApi.Dtos.CreateEnrollmentRequest request, CancellationToken ct);
}