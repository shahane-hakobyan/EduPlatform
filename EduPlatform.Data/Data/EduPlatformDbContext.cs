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
        public DbSet<UserLibraryMaterial> UserLibraryMaterials => Set<UserLibraryMaterial>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<AssignmentSubject> AssignmentSubjects => Set<AssignmentSubject>();
        public DbSet<AssignmentGradeLevel> AssignmentGradeLevels => Set<AssignmentGradeLevel>();
        public DbSet<GivenAssignment> GivenAssignments => Set<GivenAssignment>();
        public DbSet<AssignmentFile> AssignmentFiles => Set<AssignmentFile>();
        public DbSet<AssignmentRecipient> AssignmentRecipients => Set<AssignmentRecipient>();
        public DbSet<AssignmentSubmissionFile> AssignmentSubmissionFiles => Set<AssignmentSubmissionFile>();
        public DbSet<UserGroup> UserGroups => Set<UserGroup>();
        public DbSet<UserGroupMember> UserGroupMembers => Set<UserGroupMember>();
        public DbSet<AiConversation> AiConversations => Set<AiConversation>();
        public DbSet<AiMessage> AiMessages => Set<AiMessage>();
        public DbSet<AiMessageAttachment> AiMessageAttachments => Set<AiMessageAttachment>();

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

            // ── Օգտատիրոջ պահված Library նյութեր ─────────────────────────
            modelBuilder.Entity<UserLibraryMaterial>(e =>
            {
                e.Property(x => x.UserId).HasMaxLength(450);
                e.Property(x => x.MaterialType).HasMaxLength(50);

                // Նույն նյութը օգտատերը կարող է պահել միայն մեկ անգամ:
                e.HasIndex(x => new { x.UserId, x.MaterialType, x.LibraryMaterialId })
                 .IsUnique();
            });

            // ── Առաջադրանք (ձևանմուշ կամ ուղարկված) ──────────────────────
            modelBuilder.Entity<Assignment>(e =>
            {
                e.Property(x => x.TeacherUserId).HasMaxLength(450);
                e.Property(x => x.Title).HasMaxLength(300);
                e.Property(x => x.Description).HasMaxLength(4000);
                e.Property(x => x.TaskText).HasMaxLength(4000);
                e.Property(x => x.OtherTypeText).HasMaxLength(500);
                e.Property(x => x.MaterialUrl).HasMaxLength(1000);

                // Ուսուցչի շտեմարանի ցուցակը
                e.HasIndex(x => new { x.TeacherUserId, x.IsInBank });
            });

            // ── Տրված (ուղարկված) առաջադրանք՝ կապված շտեմարանի առաջադրանքին ──
            modelBuilder.Entity<GivenAssignment>(e =>
            {
                e.Property(x => x.TeacherUserId).HasMaxLength(450);

                // Շտեմարանի առաջադրանքը չի ջնջվում, քանի դեռ տրված առաջադրանքներ կան (դրա փոխարեն թաքցվում է)
                e.HasOne(x => x.Assignment)
                 .WithMany(a => a.GivenAssignments)
                 .HasForeignKey(x => x.AssignmentId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Ուսուցչի ցուցակները՝ ընթացիկ / արխիվացված
                e.HasIndex(x => new { x.TeacherUserId, x.IsArchived });
            });

            // ── Առաջադրանք ↔ Առարկա ──────────────────────────────────────
            modelBuilder.Entity<AssignmentSubject>(e =>
            {
                e.HasKey(x => new { x.AssignmentId, x.SubjectId });

                e.HasOne(x => x.Assignment)
                 .WithMany(a => a.Subjects)
                 .HasForeignKey(x => x.AssignmentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Subject)
                 .WithMany()
                 .HasForeignKey(x => x.SubjectId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Առաջադրանք ↔ Դասարանի մակարդակ (1–12) ────────────────────
            modelBuilder.Entity<AssignmentGradeLevel>(e =>
            {
                e.HasKey(x => new { x.AssignmentId, x.Level });

                e.ToTable(t => t.HasCheckConstraint("CK_AssignmentGradeLevels_Level", "[Level] BETWEEN 1 AND 12"));

                e.HasOne(x => x.Assignment)
                 .WithMany(a => a.GradeLevels)
                 .HasForeignKey(x => x.AssignmentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Առաջադրանքին կցված ֆայլեր ────────────────────────────────
            modelBuilder.Entity<AssignmentFile>(e =>
            {
                e.Property(x => x.FileName).HasMaxLength(260);
                e.Property(x => x.StoredFileName).HasMaxLength(100);
                e.Property(x => x.ContentType).HasMaxLength(150);

                // Մեկ ֆայլ՝ մեկ գրառում, կապվում է մի քանի առաջադրանքի հետ (շտեմարանի օրինակ + ուղարկված)
                e.HasMany(x => x.Assignments)
                 .WithMany(a => a.Files)
                 .UsingEntity<Dictionary<string, object>>(
                     "AssignmentFileLinks",
                     j => j.HasOne<Assignment>().WithMany().HasForeignKey("AssignmentId").OnDelete(DeleteBehavior.Cascade),
                     j => j.HasOne<AssignmentFile>().WithMany().HasForeignKey("FileId").OnDelete(DeleteBehavior.Cascade),
                     j =>
                     {
                         j.HasKey("FileId", "AssignmentId");
                         j.HasIndex("AssignmentId");
                         j.ToTable("AssignmentFileLinks");
                     });
            });

            // ── Ստացող աշակերտ (կարգավիճակ, պատասխան, գնահատական) ─────────
            modelBuilder.Entity<AssignmentRecipient>(e =>
            {
                e.Property(x => x.PupilUserId).HasMaxLength(450);
                e.Property(x => x.Score).HasPrecision(6, 2);
                e.Property(x => x.MaxScore).HasPrecision(6, 2);
                e.Property(x => x.Feedback).HasMaxLength(4000);

                // Աշակերտը մեկ առաջադրանքին ունի միայն մեկ տող
                e.HasIndex(x => new { x.GivenAssignmentId, x.PupilUserId }).IsUnique();

                // Աշակերտի «իմ առաջադրանքները» ցուցակի համար
                e.HasIndex(x => new { x.PupilUserId, x.Status });

                e.HasOne(x => x.GivenAssignment)
                 .WithMany(a => a.Recipients)
                 .HasForeignKey(x => x.GivenAssignmentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Grade)
                 .WithMany()
                 .HasForeignKey(x => x.GradeId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Աշակերտի կցած ֆայլեր ─────────────────────────────────────
            modelBuilder.Entity<AssignmentSubmissionFile>(e =>
            {
                e.Property(x => x.FileName).HasMaxLength(260);
                e.Property(x => x.StoredFileName).HasMaxLength(100);
                e.Property(x => x.ContentType).HasMaxLength(150);

                e.HasOne(x => x.Recipient)
                 .WithMany(r => r.Files)
                 .HasForeignKey(x => x.RecipientId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Խումբ (սովորողների / ծնողների / այլ) ──────────────────────
            modelBuilder.Entity<UserGroup>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200);
                e.Property(x => x.CreatedByUserId).HasMaxLength(450);
                e.Property(x => x.ImageStoredFileName).HasMaxLength(100);
                e.Property(x => x.ImageContentType).HasMaxLength(150);

                // «Իմ ստեղծած խմբերը»
                e.HasIndex(x => x.CreatedByUserId);
            });

            // ── Խմբի անդամ ───────────────────────────────────────────────
            modelBuilder.Entity<UserGroupMember>(e =>
            {
                e.HasKey(x => new { x.GroupId, x.UserId });
                e.Property(x => x.UserId).HasMaxLength(450);

                // «Այն խմբերը, որոնցում ինձ ավելացրել են»
                e.HasIndex(x => x.UserId);

                e.HasOne(x => x.Group)
                 .WithMany(g => g.Members)
                 .HasForeignKey(x => x.GroupId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── ԱԲ զրուցարան ─────────────────────────────────────────────
            modelBuilder.Entity<AiConversation>(e =>
            {
                e.Property(x => x.UserId).HasMaxLength(450);
                e.Property(x => x.Title).HasMaxLength(200);
                e.Property(x => x.AgentKey).HasMaxLength(50);

                // «Իմ զրույցները»՝ ամրացվածները առաջինը, ապա վերջին փոփոխվածները
                e.HasIndex(x => new { x.UserId, x.IsPinned, x.UpdatedAt });
            });

            modelBuilder.Entity<AiMessage>(e =>
            {
                e.HasOne(x => x.Conversation)
                 .WithMany(c => c.Messages)
                 .HasForeignKey(x => x.ConversationId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.ConversationId);
            });

            modelBuilder.Entity<AiMessageAttachment>(e =>
            {
                e.Property(x => x.FileName).HasMaxLength(260);
                e.Property(x => x.StoredFileName).HasMaxLength(100);   // IAssignmentFileService-ի բանալիների սահմանը
                e.Property(x => x.ContentType).HasMaxLength(150);

                e.HasOne(x => x.Message)
                 .WithMany(m => m.Attachments)
                 .HasForeignKey(x => x.MessageId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => x.MessageId);
            });
        }
    }
}
