using CourseManagement.Data;
using CourseManagement.Repositories;
using CourseManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var adminEmail = builder.Configuration["AdminSeed:Email"];
var adminPassword = builder.Configuration["AdminSeed:Password"];

if (string.IsNullOrWhiteSpace(adminEmail) != string.IsNullOrWhiteSpace(adminPassword))
{
    throw new InvalidOperationException(
        "Both AdminSeed:Email and AdminSeed:Password must be configured together.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
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
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });

});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFileService, FileService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Tự động tạo database khi khởi động (development only)
    using var scope = app.Services.CreateScope();
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try {
        ctx.Database.EnsureCreated();

        // Tạo bảng StoredFiles nếu chưa tồn tại (EnsureCreated không tự thêm bảng mới vào DB cũ)
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

        // Seed an admin only when credentials are supplied through configuration.
        if (!string.IsNullOrWhiteSpace(adminEmail)
            && !string.IsNullOrWhiteSpace(adminPassword)
            && !ctx.Users.Any(u => u.Role == "Admin"))
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
    } catch { /* bỏ qua nếu lỗi kết nối */ }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/home", () => Results.Redirect("/home.html"));
app.MapControllers();

app.Run();
