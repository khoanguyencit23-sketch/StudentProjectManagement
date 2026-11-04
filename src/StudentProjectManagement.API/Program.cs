using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StudentProjectManagement.API.Middleware;
using StudentProjectManagement.Application.Common;
using StudentProjectManagement.Infrastructure;
using StudentProjectManagement.Infrastructure.Options;
using StudentProjectManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Jwt configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey must contain at least 32 UTF-8 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
}

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Chuẩn hóa cả lỗi validation do [ApiController] tự động trả về.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? "Invalid value."
                        : e.ErrorMessage).ToArray());

            var response = ApiResponse<object?>.FailureResponse(
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                errors);

            return new BadRequestObjectResult(response);
        };
    });

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["https://localhost:3000", "http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Student Project Management API",
        Version = "v1",
        Description = "Base API for the CSW306 Student Project Management System."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the access token only. Swagger adds the Bearer prefix automatically."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Migration và seed chạy một lần trong lúc khởi động ứng dụng.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var applyMigrations = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");

    if (applyMigrations)
    {
        await dbContext.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(dbContext, app.Environment.IsDevelopment());
}

app.Run();

// Cần thiết cho WebApplicationFactory trong integration tests.
public partial class Program { }
