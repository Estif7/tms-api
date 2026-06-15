using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

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

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var app = builder.Build();


// =========================================================================
// 2. MIDDLEWARE PIPELINE CONFIGURATION (Ordering Matters Explicitly!)
// =========================================================================
app.UseMiddleware<RequestLoggingMiddleware>();

// Standard framework behaviors follow
app.UseExceptionHandler("/error"); // For Session 3 ProblemDetails integration
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

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

app.Run();