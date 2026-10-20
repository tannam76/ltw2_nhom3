using System.Text;
using CourseManagement.Data;
using CourseManagement.Repositories;
using CourseManagement.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── JWT Authentication Configuration ──────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? "CourseManagement_SuperSecretKey_ForJwtToken_2026!@#$MustBeAtLeast32Bytes";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CourseManagementAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CourseManagementClient";

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
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Course Management API", Version = "v1" });

    // Hỗ trợ nhập JWT Bearer token trực tiếp trong Swagger UI
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT Token của bạn (Swagger sẽ tự thêm tiền tố Bearer)"
    };
    options.AddSecurityDefinition("Bearer", scheme);

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Đăng ký Repository & Service
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILessonQuestionGeneratorService, LessonQuestionGeneratorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Tự động tạo database khi khởi động (development only)
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try { ctx.Database.EnsureCreated(); } catch { /* bỏ qua nếu lỗi kết nối */ }
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
