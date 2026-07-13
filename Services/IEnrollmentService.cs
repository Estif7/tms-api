using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Dtos;

namespace TmsApi.Services;

public interface IEnrollmentService
{
    // Fixes the 'GetByCourseAsync' compiler error
    Task<IReadOnlyList<EnrollmentRecord>> GetByCourseAsync(int courseId, CancellationToken ct);
    
    // Kept for single lookups
    Task<EnrollmentRecord?> GetByIdAsync(string id); 

    // Fixes the 'EnrollStudentAsync' and the 3 'Cannot infer the type...' deconstruction errors
    Task<(bool CourseExists, bool IsFull, EnrollmentRecord? Result)> EnrollStudentAsync(
        int courseId, 
        CreateEnrollmentRequest request, 
        CancellationToken ct);
}