using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Services;
using TmsApi.Filters;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. SERVICE REGISTRATION (Dependency Injection)
// =========================================================================

// Register the strict security services required by the runtime pipeline
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();

// Bind, validate, and enforce instant validation for our payment options section
builder.Services.AddOptions<PaymentOptions>()
    .Bind(builder.Configuration.GetSection(PaymentOptions.SectionName))
    .ValidateDataAnnotations() // Runs our validation checks
    .ValidateOnStart(); // Forces validation to execute during server startup

// Register clashing service lifetimes to trigger container analysis
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// Turn on explicit framework validation constraints
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true; // Blocks captive service injections
    options.ValidateOnBuild = true; // Scans dependencies completely during build execution
});

// Register TmsDbContext scoped for incoming HTTP requests
builder.Services.AddDbContext<TmsDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
.LogTo(Console.WriteLine, LogLevel.Information) // Log SQL to output window
.EnableSensitiveDataLogging()); // Show parameters in querylogs (dev only)

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();


builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>(); // Using the generic method allows DI to resolve the logger automatically
});

var app = builder.Build();


// =========================================================================
// 2. MIDDLEWARE PIPELINE CONFIGURATION (Ordering Matters Explicitly!)
// =========================================================================
app.UseMiddleware<RequestLoggingMiddleware>();

// Standard framework behaviors follow
// app.UseExceptionHandler("/error"); 

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();
app.UseRouting();

// Security MUST intercept requests right after routing matches them, 
// and ALWAYS before endpoints run!
app.UseAuthentication();
app.UseAuthorization();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Accessible at /scalar/v1
}
else
{
    // Production only: ensure exceptions are caught and formatted without leaks
    app.UseExceptionHandler();
}

// =========================================================================
// 3. ENDPOINT DEFINITIONS
// =========================================================================
app.MapControllers();

// Secured placeholder endpoint for Session 1 evaluation
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization(); // Instantly turns away anonymous calls with a 401 response

app.MapGet("/api/test-enroll", async (IEnrollmentService enrollmentService) =>
{
    string testStudent = "STU-777";
    string testCourse = "CRS-EXT10";

    // First attempt: Creates the new record smoothly
    var firstAttempt = await enrollmentService.EnrollAsync(testStudent, testCourse);

    // Second attempt: Triggers our defensive duplicate check and logs the warning
    var secondAttempt = await enrollmentService.EnrollAsync(testStudent, testCourse);

    return Results.Ok(new
    {
        Message = "Check your application console logs to see the structured output properties!",
        FirstRecordId = firstAttempt.Id,
        SecondRecordId = secondAttempt.Id,
        IsSameInstance = object.ReferenceEquals(firstAttempt, secondAttempt)
    });
});

app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});


// Seed test data at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    context.Database.Migrate(); // Applies any pending migrations; keeps migration history intact
    if (!context.Students.Any())
    {
        var students = new List<Student>
{
new() { RegistrationNumber = "TMS-2026-0001", Name = "AliceSmith", GPA = 3.8m, IsActive = true },
new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
new() { RegistrationNumber = "TMS-2026-0004", Name = "DianaPrince", GPA = 3.9m, IsActive = true },
new() { RegistrationNumber = "TMS-2026-0005", Name = "EvanWright", GPA = 2.5m, IsActive = true }
};
        context.Students.AddRange(students);
        var courses = new List<Course>
{
new() { Code = "CS-101", Title = "Introduction to ComputerScience", MaxCapacity = 30 },
new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
};
        context.Courses.AddRange(courses);
        context.SaveChanges();
        var enrollments = new List<Enrollment>
{
new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
};
        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
    }
}


if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    
    // Call our static seed method using the correct persistence namespace
    await TmsApi.Persistence.DataSeeder.SeedAsync(context);
}

app.Run();