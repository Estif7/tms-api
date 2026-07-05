using System;
using Microsoft.Extensions.DependencyInjection;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Inject the factory instead of the scoped instance directly
    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        // Create a short-lived scope boundary using the factory
        using var scope = _scopeFactory.CreateScope();

        // Resolve the scoped service safely from the localized provider instance
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();

        // Execute processing safely against a fresh connection instance
        var records = enrollmentService.GetAllAsync().GetAwaiter().GetResult();
        Console.WriteLine($"[Worker] Processed batch containing {records.Count} enrollment records.");
    }
}