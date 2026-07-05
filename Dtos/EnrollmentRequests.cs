using System.ComponentModel.DataAnnotations;

namespace TmsApi.Dtos;

public record CreateEnrollmentRequest(
    [Required] int StudentId); // Identifies the student registering

public record EnrollmentResponseDto(
    int Id,
    int CourseId,
    int StudentId,
    DateTime EnrolledAt);