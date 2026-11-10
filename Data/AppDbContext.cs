using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<QuizOption> QuizOptions { get; set; }
    public DbSet<StoredFile> StoredFiles { get; set; }
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
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).IsRequired().HasMaxLength(200);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(200);
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Role).HasDefaultValue("Student");
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).IsRequired().HasMaxLength(200);
            entity.Property(product => product.Description).HasMaxLength(1000);
            entity.Property(product => product.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).IsRequired().HasMaxLength(200);
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

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("StoredFiles");
            entity.HasKey(file => file.Id);
            entity.Property(file => file.FileName).IsRequired().HasMaxLength(255);
            entity.Property(file => file.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(file => file.Content).IsRequired();
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");
            entity.HasKey(enrollment => enrollment.Id);
            entity.HasIndex(enrollment => new { enrollment.UserId, enrollment.CourseId }).IsUnique();
            entity.HasOne(enrollment => enrollment.User)
                .WithMany()
                .HasForeignKey(enrollment => enrollment.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(enrollment => enrollment.Course)
                .WithMany()
                .HasForeignKey(enrollment => enrollment.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.ToTable("LessonProgresses");
            entity.HasKey(progress => progress.Id);
            entity.HasIndex(progress => new { progress.UserId, progress.LessonId }).IsUnique();
            entity.HasOne(progress => progress.User)
                .WithMany()
                .HasForeignKey(progress => progress.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(progress => progress.Lesson)
                .WithMany()
                .HasForeignKey(progress => progress.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonQuestion>(entity =>
        {
            entity.ToTable("LessonQuestions");
            entity.HasKey(question => question.Id);
            entity.Property(question => question.QuestionText).IsRequired().HasMaxLength(2000);
            entity.Property(question => question.Type).HasConversion<int>();
            entity.HasOne(question => question.Lesson)
                .WithMany()
                .HasForeignKey(question => question.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(question => question.Creator)
                .WithMany()
                .HasForeignKey(question => question.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LessonQuestionOption>(entity =>
        {
            entity.ToTable("LessonQuestionOptions");
            entity.HasKey(option => option.Id);
            entity.Property(option => option.Text).IsRequired().HasMaxLength(500);
            entity.HasOne(option => option.LessonQuestion)
                .WithMany(question => question.Options)
                .HasForeignKey(option => option.LessonQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonQuestionAnswer>(entity =>
        {
            entity.ToTable("LessonQuestionAnswers");
            entity.HasKey(answer => answer.Id);
            entity.Property(answer => answer.EssayText).HasMaxLength(10000);
            entity.Property(answer => answer.InstructorFeedback).HasMaxLength(2000);
            entity.HasIndex(answer => new { answer.LessonQuestionId, answer.UserId }).IsUnique();
            entity.HasOne(answer => answer.LessonQuestion)
                .WithMany(question => question.Answers)
                .HasForeignKey(answer => answer.LessonQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(answer => answer.User)
                .WithMany()
                .HasForeignKey(answer => answer.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
