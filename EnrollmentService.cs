using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// --- The Contract ---
public interface IEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}

// --- The In-Memory Implementation ---
public class EnrollmentService : IEnrollmentService
{
    private readonly Dictionary<string, EnrollmentRecord> _store = new();
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        _logger = logger;
    }

    public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
    {
        // Check if a record with the exact same student and course already exists
        var existingRecord = _store.Values.FirstOrDefault(r => 
            r.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase) && 
            r.CourseCode.Equals(courseCode, StringComparison.OrdinalIgnoreCase));

        if (existingRecord != null)
        {
            // ✅ CORRECT: Structured logging using template parameters
            // The properties 'StudentId' and 'CourseCode' will be indexed separately in log aggregators
            _logger.LogWarning(
                "Duplicate enrollment attempt with {StudentId} and {CourseCode}", 
                studentId, courseCode);

            return Task.FromResult(existingRecord);
        }

        // Process a fresh enrollment if no duplicate is found
        var id = Guid.NewGuid().ToString("N")[..8];
        var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
        _store[id] = record;

        _logger.LogInformation(
            "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}", 
            studentId, courseCode, id);

        return Task.FromResult(record);
    }

    public Task<EnrollmentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        IReadOnlyList<EnrollmentRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        return Task.FromResult(removed);
    }
}

// --- The Data Shape ---
public record EnrollmentRecord(
    string Id,
    string StudentId,
    string CourseCode,
    DateTime EnrolledAt);

// Exception Class
public class TmsDatabaseException(string message) : Exception(message);