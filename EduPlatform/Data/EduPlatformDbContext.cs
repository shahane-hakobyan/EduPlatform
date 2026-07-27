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
        public DbSet<LessonSubject> LessonSubjects => Set<LessonSubject>();
        public DbSet<LessonGrade> LessonGrades => Set<LessonGrade>();
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

            // ScheduleLesson ↔ Subject (many-to-many via LessonSubject)
            modelBuilder.Entity<LessonSubject>()
                .HasOne(ls => ls.ScheduleLesson)
                .WithMany(l => l.LessonSubjects)
                .HasForeignKey(ls => ls.ScheduleLessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonSubject>()
                .HasOne(ls => ls.Subject)
                .WithMany()
                .HasForeignKey(ls => ls.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ScheduleLesson ↔ Grade (many-to-many via LessonGrade)
            modelBuilder.Entity<LessonGrade>()
                .HasOne(lg => lg.ScheduleLesson)
                .WithMany(l => l.LessonGrades)
                .HasForeignKey(lg => lg.ScheduleLessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonGrade>()
                .HasOne(lg => lg.Grade)
                .WithMany()
                .HasForeignKey(lg => lg.GradeId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Consistency: a teacher is in one place per day+hour
            modelBuilder.Entity<ScheduleLesson>()
                .HasIndex(l => new { l.TeacherUserId, l.Day, l.PeriodNumber })
                .IsUnique();
        }
    }
}