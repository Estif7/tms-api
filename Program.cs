using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. SERVICE REGISTRATION (Dependency Injection)
// =========================================================================
builder.Services.AddControllers();

// Register the strict security services required by the runtime pipeline
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();

var app = builder.Build();

// =========================================================================
// 2. MIDDLEWARE PIPELINE CONFIGURATION (Ordering Matters Explicitly!)
// =========================================================================
app.UseMiddleware<RequestLoggingMiddleware>();

// Standard framework behaviors follow
app.UseExceptionHandler("/error"); // For Session 3 ProblemDetails integration
app.UseHttpsRedirection();

app.UseRouting();

// Security MUST intercept requests right after routing matches them, 
// and ALWAYS before endpoints run!
app.UseAuthentication();
app.UseAuthorization();

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

app.Run();