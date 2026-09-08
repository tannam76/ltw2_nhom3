using CourseManagement.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});
builder.Services.AddSingleton<IUserService, UserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    name = "Course Management API",
    status = "running",
    endpoints = new[] { "/api/auth/register", "/api/auth/login", "/api/auth/me", "/openapi/v1.json" }
}));

app.Run();
