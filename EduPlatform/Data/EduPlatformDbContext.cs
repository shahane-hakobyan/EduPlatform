using EduPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Data
{
    public class EduPlatformDbContext : DbContext
    {
        public EduPlatformDbContext(DbContextOptions<EduPlatformDbContext> options) : base(options) { }

        public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<ScheduleEntry> ScheduleEntries => Set<ScheduleEntry>(); 
        public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
        public DbSet<TeacherClass> TeacherClasses => Set<TeacherClass>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ScheduleEntry>()
                .HasOne(s => s.SchoolClass)
                .WithMany(c => c.ScheduleEntries)
                .HasForeignKey(s => s.SchoolClassId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ScheduleEntry>()
                .HasOne(s => s.Subject)
                .WithMany()
                .HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Subject)
            .WithMany()
            .HasForeignKey(ts => ts.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherClass>()
                .HasOne(tc => tc.SchoolClass)
                .WithMany()
                .HasForeignKey(tc => tc.SchoolClassId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}