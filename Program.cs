using System.Security.Claims;
using System.Text;
using CourseManagement.Data;
using CourseManagement.Middleware;
using CourseManagement.Repositories;
using CourseManagement.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var adminEmail = builder.Configuration["AdminSeed:Email"];
    var adminPassword = builder.Configuration["AdminSeed:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) != string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Both AdminSeed:Email and AdminSeed:Password must be configured together.");
    }

    // 1. Đọc thông tin Jwt từ appsettings.json (hỗ trợ cả JwtSettings lẫn Jwt)
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var jwtKey = jwtSettings["SecretKey"]
        ?? builder.Configuration["Jwt:Key"]
        ?? "CourseManagement_SuperSecretKey_ForJwtToken_2026!@#$MustBeAtLeast32Bytes";
    var jwtIssuer = jwtSettings["Issuer"]
        ?? builder.Configuration["Jwt:Issuer"]
        ?? "CourseManagementAPI";
    var jwtAudience = jwtSettings["Audience"]
        ?? builder.Configuration["Jwt:Audience"]
        ?? "CourseManagementClient";

    // 2. Cấu hình dịch vụ Authentication với JwtBearer
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };
    });

    builder.Services.AddAuthorization();

    // 3. Cấu hình Swagger kèm khung nhập Token JWT
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "Course Management API", Version = "v1" });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập AccessToken vào ô bên dưới (Swagger sẽ tự thêm tiền tố 'Bearer '):"
        });

        options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
        });
    });

    // 4. Đăng ký DbContext và CORS
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
    });

    // 5. Đăng ký Dependency Injection cho Services & Repositories
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Dữ liệu không đúng định dạng." : x.ErrorMessage))
                    .ToList();

                var response = new CourseManagement.DTOs.Common.ErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Dữ liệu gửi lên không hợp lệ.",
                    Detail = string.Join("; ", errors),
                    TraceId = context.HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return new BadRequestObjectResult(response);
            };
        });
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IFileRepository, FileRepository>();
    builder.Services.AddScoped<IFileService, FileService>();
    builder.Services.AddScoped<ILessonQuestionGeneratorService, LessonQuestionGeneratorService>();

    var app = builder.Build();

    // 6. Pipeline: Exception Handling & Request Logging đặt đầu tiên
    app.UseExceptionHandling();
    app.UseRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            ctx.Database.EnsureCreated();

            ctx.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StoredFiles]') AND type = 'U')
                BEGIN
                    CREATE TABLE [dbo].[StoredFiles] (
                        [Id]          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [FileName]    NVARCHAR(255)     NOT NULL,
                        [ContentType] NVARCHAR(100)     NOT NULL,
                        [Content]     VARBINARY(MAX)    NOT NULL,
                        [Length]      BIGINT            NOT NULL DEFAULT 0,
                        [UploadedAt]  DATETIME2         NOT NULL DEFAULT GETUTCDATE()
                    );
                END");

            if (!string.IsNullOrWhiteSpace(adminEmail)
                && !string.IsNullOrWhiteSpace(adminPassword)
                && !ctx.Users.Any(user => user.Role == "Admin"))
            {
                ctx.Users.Add(new CourseManagement.Models.User
                {
                    FullName = "Administrator",
                    Email = adminEmail,
                    PasswordHash = UserService.HashPassword(adminPassword),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                });
                ctx.SaveChanges();
            }

            await DataSeeder.SeedUsersAsync(ctx, 100);
        }
        catch
        {
            // Keep the existing development-only startup behavior when the database is unavailable.
        }
    }

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapGet("/home", () => Results.Redirect("/home.html"));
    app.MapGet("/study", () => Results.Redirect("/study.html"));
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ứng dụng dừng bất thường khi khởi động.");
}
finally
{
    Log.CloseAndFlush();
}
