using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public class EnrollmentService(DbContext context) : IEnrollmentService 
{
    public async Task<IReadOnlyList<EnrollmentRecord>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        // Join with the Courses table to retrieve the CourseCode string matching EnrollmentRecord's signature
        var enrollments = await context.Set<Enrollment>()
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Join(context.Set<Course>(),
                e => e.CourseId,
                c => c.Id,
                (e, c) => new { Enrollment = e, CourseCode = c.Code })
            .ToListAsync(ct);

        return enrollments.Select(x => new EnrollmentRecord(
            x.Enrollment.Id.ToString(), 
            x.CourseCode, 
            x.Enrollment.StudentId.ToString(), 
            DateTime.UtcNow
        )).ToList();
    }

    public async Task<EnrollmentRecord?> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out var enrollmentId)) return null;

        var item = await context.Set<Enrollment>()
            .AsNoTracking()
            .Where(e => e.Id == enrollmentId)
            .Join(context.Set<Course>(),
                e => e.CourseId,
                c => c.Id,
                (e, c) => new { Enrollment = e, CourseCode = c.Code })
            .FirstOrDefaultAsync();

        if (item is null) return null;

        return new EnrollmentRecord(
            item.Enrollment.Id.ToString(), 
            item.CourseCode, 
            item.Enrollment.StudentId.ToString(), 
            DateTime.UtcNow
        );
    }

    public async Task<(bool CourseExists, bool IsFull, EnrollmentRecord? Result)> EnrollStudentAsync(
        int courseId, 
        CreateEnrollmentRequest request, 
        CancellationToken ct)
    {
        var course = await context.Set<Course>().FindAsync([courseId], cancellationToken: ct);
        if (course is null)
        {
            return (false, false, null);
        }

        var currentCount = await context.Set<Enrollment>().CountAsync(e => e.CourseId == courseId, ct);
        if (currentCount >= course.MaxCapacity) 
        {
            return (true, true, null);
        }

        if (!int.TryParse(request.StudentId, out var parsedStudentId))
        {
            throw new ArgumentException("StudentId must be an integer.");
        }

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = parsedStudentId
        };

        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync(ct);

        var resultRecord = new EnrollmentRecord(
            enrollment.Id.ToString(), 
            course.Code, 
            enrollment.StudentId.ToString(), 
            DateTime.UtcNow
        );

        return (true, false, resultRecord);
    }
}