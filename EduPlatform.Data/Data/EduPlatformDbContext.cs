using EduPlatform.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Data
{
    /// <summary>
    /// EduPlatform-ի տվյալների բազայի համատեքստը:
    /// </summary>
    public class EduPlatformDbContext : DbContext
    {
        public EduPlatformDbContext(DbContextOptions<EduPlatformDbContext> options)
            : base(options)
        {
        }

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
            base.OnModelCreating(modelBuilder);

            // ── Հաստատություն ────────────────────────────────────────────
            modelBuilder.Entity<Institution>(e =>
            {
                e.HasIndex(x => x.KtakInstitutionId).IsUnique();
                e.Property(x => x.KtakInstitutionId).HasMaxLength(50);
                e.Property(x => x.DshhInstitutionId).HasMaxLength(100);
                e.Property(x => x.Name).HasMaxLength(200);
            });

            // ── Դասարան ──────────────────────────────────────────────────
            modelBuilder.Entity<Grade>(e =>
            {
                e.Property(x => x.GradeName).HasMaxLength(20);

                // Դասարանի անվանումը եզակի է մեկ հաստատության շրջանակում:
                e.HasIndex(x => new { x.InstitutionId, x.GradeName }).IsUnique();

                e.HasOne(x => x.Institution)
                 .WithMany(i => i.Grades)
                 .HasForeignKey(x => x.InstitutionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Առարկա ───────────────────────────────────────────────────
            modelBuilder.Entity<Subject>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(100);
                e.HasIndex(x => x.Name).IsUnique();
            });

            // ── Դասացուցակի դաս ──────────────────────────────────────────
            modelBuilder.Entity<ScheduleLesson>(e =>
            {
                e.Property(x => x.TeacherUserId).HasMaxLength(450);
                e.Property(x => x.DshhTeacherId).HasMaxLength(100);
                e.Property(x => x.AcademicYear).HasMaxLength(9);
                e.Property(x => x.RoomNumber).HasMaxLength(20);
                e.Property(x => x.Notes).HasMaxLength(500);

                // Ուսուցիչը չի կարող ունենալ երկու դաս նույն տարվա,
                // նույն օրվա, նույն ժամի համար:
                e.HasIndex(x => new
                {
                    x.TeacherUserId,
                    x.AcademicYear,
                    x.Day,
                    x.PeriodNumber
                }).IsUnique();
            });

            // ── Դաս ↔ Առարկա ─────────────────────────────────────────────
            modelBuilder.Entity<LessonSubject>(e =>
            {
                e.HasKey(x => new { x.ScheduleLessonId, x.SubjectId });

                e.HasOne(x => x.ScheduleLesson)
                 .WithMany(l => l.LessonSubjects)
                 .HasForeignKey(x => x.ScheduleLessonId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Subject)
                 .WithMany()
                 .HasForeignKey(x => x.SubjectId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Դաս ↔ Դասարան ────────────────────────────────────────────
            modelBuilder.Entity<LessonGrade>(e =>
            {
                e.HasKey(x => new { x.ScheduleLessonId, x.GradeId });

                e.HasOne(x => x.ScheduleLesson)
                 .WithMany(l => l.LessonGrades)
                 .HasForeignKey(x => x.ScheduleLessonId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Grade)
                 .WithMany()
                 .HasForeignKey(x => x.GradeId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Ուսուցիչ ↔ Առարկա ────────────────────────────────────────
            modelBuilder.Entity<TeacherSubject>(e =>
            {
                e.HasKey(x => new { x.TeacherUserId, x.SubjectId });

                e.Property(x => x.TeacherUserId).HasMaxLength(450);
                e.Property(x => x.DshhTeacherId).HasMaxLength(100);

                e.HasOne(x => x.Subject)
                 .WithMany()
                 .HasForeignKey(x => x.SubjectId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Ուսուցիչ ↔ Դասարան (ըստ ուսումնական տարվա) ───────────────
            modelBuilder.Entity<TeacherGrade>(e =>
            {
                e.HasKey(x => new { x.TeacherUserId, x.GradeId, x.AcademicYear });

                e.Property(x => x.TeacherUserId).HasMaxLength(450);
                e.Property(x => x.DshhTeacherId).HasMaxLength(100);
                e.Property(x => x.AcademicYear).HasMaxLength(9);

                e.HasOne(x => x.Grade)
                 .WithMany()
                 .HasForeignKey(x => x.GradeId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Աշակերտ ↔ Դասարան (ըստ ուսումնական տարվա) ────────────────
            modelBuilder.Entity<PupilGrade>(e =>
            {
                e.HasKey(x => new { x.PupilUserId, x.GradeId, x.AcademicYear });

                e.Property(x => x.PupilUserId).HasMaxLength(450);
                e.Property(x => x.DshhPupilId).HasMaxLength(100);
                e.Property(x => x.AcademicYear).HasMaxLength(9);

                e.HasOne(x => x.Grade)
                 .WithMany()
                 .HasForeignKey(x => x.GradeId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}