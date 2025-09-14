using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AzotaLite.Web.Models;

namespace AzotaLite.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Classroom> Classrooms => Set<Classroom>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<Choice> Choices => Set<Choice>();
        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<SubmissionAnswer> SubmissionAnswers => Set<SubmissionAnswer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Classroom: mỗi Teacher (ApplicationUser) có nhiều Classrooms
            modelBuilder.Entity<Classroom>()
                .HasOne(c => c.Teacher)
                .WithMany() // bạn có thể thêm ICollection<Classroom> vào ApplicationUser nếu muốn
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Student: 1 Classroom - n Students
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Classroom)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Gợi ý chỉ mục để tìm kiếm nhanh
            modelBuilder.Entity<Classroom>()
                .HasIndex(c => new { c.TeacherId, c.Name });

            modelBuilder.Entity<Student>()
                .HasIndex(s => new { s.ClassroomId, s.FullName });

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.OwnerTeacher)
                .WithMany()
                .HasForeignKey(e => e.OwnerTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            // Question: Exam 1-n Question
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany() // nếu muốn: thêm ICollection<Question> vào Exam
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Choice: Question 1-n Choice
            modelBuilder.Entity<Choice>()
                .HasOne(c => c.Question)
                .WithMany(q => q.Choices)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Submission: Unique ExamId + StudentId (1 HS, 1 bài thi → 1 submission)
            modelBuilder.Entity<Submission>()
                .HasIndex(s => new { s.ExamId, s.StudentId })
                .IsUnique();

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Exam)
                .WithMany()
                .HasForeignKey(s => s.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // SubmissionAnswer: Submission 1-n Answer
            modelBuilder.Entity<SubmissionAnswer>()
                .HasOne(a => a.Submission)
                .WithMany(s => s.Answers)
                .HasForeignKey(a => a.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Answer → Question (bắt buộc), → Choice (tuỳ)
            modelBuilder.Entity<SubmissionAnswer>()
                .HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubmissionAnswer>()
                .HasOne(a => a.Choice)
                .WithMany()
                .HasForeignKey(a => a.ChoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exam>()
                .HasIndex(e => new { e.OwnerTeacherId, e.Title }); // tìm kiếm nhanh theo Teacher
            // Tối ưu chỉ mục hay tìm kiếm
            modelBuilder.Entity<Question>().HasIndex(q => new { q.ExamId, q.Order });
            modelBuilder.Entity<Choice>().HasIndex(c => c.QuestionId);
            modelBuilder.Entity<SubmissionAnswer>().HasIndex(a => new { a.SubmissionId, a.QuestionId });

        }
    }
}
