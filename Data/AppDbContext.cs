using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<QuizOption> QuizOptions { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<LessonProgress> LessonProgresses { get; set; }
    public DbSet<LessonQuestion> LessonQuestions { get; set; }
    public DbSet<LessonQuestionOption> LessonQuestionOptions { get; set; }
    public DbSet<LessonQuestionAnswer> LessonQuestionAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasDefaultValue("Student");
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");
            entity.HasKey(course => course.Id);
            entity.Property(course => course.Title).IsRequired().HasMaxLength(200);
            entity.Property(course => course.Description).IsRequired().HasMaxLength(2000);
            entity.HasOne(course => course.Instructor)
                .WithMany()
                .HasForeignKey(course => course.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("Lessons");
            entity.HasKey(lesson => lesson.Id);
            entity.Property(lesson => lesson.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(lesson => new { lesson.CourseId, lesson.Order }).IsUnique();
            entity.HasOne(lesson => lesson.Course)
                .WithMany(course => course.Lessons)
                .HasForeignKey(lesson => lesson.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.ToTable("Quizzes");
            entity.HasKey(quiz => quiz.Id);
            entity.Property(quiz => quiz.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(quiz => quiz.Lesson)
                .WithMany(lesson => lesson.Quizzes)
                .HasForeignKey(quiz => quiz.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.ToTable("QuizQuestions");
            entity.HasKey(question => question.Id);
            entity.Property(question => question.Question).IsRequired().HasMaxLength(1000);
            entity.HasOne(question => question.Quiz)
                .WithMany(quiz => quiz.Questions)
                .HasForeignKey(question => question.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizOption>(entity =>
        {
            entity.ToTable("QuizOptions");
            entity.HasKey(option => option.Id);
            entity.Property(option => option.Text).IsRequired().HasMaxLength(500);
            entity.HasIndex(option => new { option.QuizQuestionId, option.Order }).IsUnique();
            entity.HasOne(option => option.QuizQuestion)
                .WithMany(question => question.Options)
                .HasForeignKey(option => option.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.ToTable("LessonProgresses");
            entity.HasKey(lp => lp.Id);
            entity.HasIndex(lp => new { lp.UserId, lp.LessonId }).IsUnique();
            entity.HasOne(lp => lp.User)
                .WithMany()
                .HasForeignKey(lp => lp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(lp => lp.Lesson)
                .WithMany()
                .HasForeignKey(lp => lp.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonQuestion>(entity =>
        {
            entity.ToTable("LessonQuestions");
            entity.HasKey(q => q.Id);
            entity.Property(q => q.QuestionText).IsRequired().HasMaxLength(2000);
            entity.Property(q => q.Type).HasConversion<int>();
            entity.HasOne(q => q.Lesson)
                .WithMany()
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(q => q.Creator)
                .WithMany()
                .HasForeignKey(q => q.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonQuestionOption>(entity =>
        {
            entity.ToTable("LessonQuestionOptions");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Text).IsRequired().HasMaxLength(500);
            entity.HasOne(o => o.LessonQuestion)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.LessonQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonQuestionAnswer>(entity =>
        {
            entity.ToTable("LessonQuestionAnswers");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.EssayText).HasMaxLength(10000);
            entity.Property(a => a.InstructorFeedback).HasMaxLength(2000);
            // Mỗi user chỉ nộp 1 lần cho mỗi câu hỏi
            entity.HasIndex(a => new { a.LessonQuestionId, a.UserId }).IsUnique();
            entity.HasOne(a => a.LessonQuestion)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.LessonQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}