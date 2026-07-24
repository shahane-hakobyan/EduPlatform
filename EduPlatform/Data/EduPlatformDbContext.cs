using EduPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Data
{
    public class EduPlatformDbContext : DbContext
    {
        public EduPlatformDbContext(DbContextOptions<EduPlatformDbContext> options) : base(options) { }

        public DbSet<Institution> Institutions => Set<Institution>();
        public DbSet<Grade> Grades => Set<Grade>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<ScheduleLesson> ScheduleLessons => Set<ScheduleLesson>();
        public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
        public DbSet<TeacherGrade> TeacherGrades => Set<TeacherGrade>();
        public DbSet<PupilGrade> PupilGrades => Set<PupilGrade>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Institution → Grade
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Institution)
                .WithMany(i => i.Grades)
                .HasForeignKey(g => g.InstitutionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Grade → ScheduleLesson (nullable: null = teacher free hour)
            modelBuilder.Entity<ScheduleLesson>()
                .HasOne(l => l.Grade)
                .WithMany(g => g.ScheduleLessons)
                .HasForeignKey(l => l.GradeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Subject → ScheduleLesson (nullable)
            modelBuilder.Entity<ScheduleLesson>()
                .HasOne(l => l.Subject)
                .WithMany()
                .HasForeignKey(l => l.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // Teacher scoping tables
            modelBuilder.Entity<TeacherSubject>()
                .HasOne(ts => ts.Subject)
                .WithMany()
                .HasForeignKey(ts => ts.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherGrade>()
                .HasOne(tg => tg.Grade)
                .WithMany()
                .HasForeignKey(tg => tg.GradeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Pupil → Grade
            modelBuilder.Entity<PupilGrade>()
                .HasOne(pg => pg.Grade)
                .WithMany()
                .HasForeignKey(pg => pg.GradeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Consistency: a grade has one lesson per day+hour
            modelBuilder.Entity<ScheduleLesson>()
                .HasIndex(l => new { l.GradeId, l.Day, l.PeriodNumber })
                .IsUnique()
                .HasFilter("[GradeId] IS NOT NULL");

            // Consistency: a teacher is in one place per day+hour
            modelBuilder.Entity<ScheduleLesson>()
                .HasIndex(l => new { l.TeacherUserId, l.Day, l.PeriodNumber })
                .IsUnique();
        }
    }
}