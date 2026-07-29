namespace EduPlatform.Models
{
    /// <summary>
    /// Որ դասարանների հետ է աշխատում ուսուցիչը՝ ըստ ուսումնական տարվա
    /// </summary>
    /// <remarks>
    /// Ուսուցչի դասարանները փոխվում են տարեցտարի, ուստի տարին մաս է կազմում
    /// առաջնային բանալու՝ <c>(TeacherUserId, GradeId, AcademicYear)</c>:
    /// Ներկայումս սա նաև միակ ճանապարհն է ուսուցչից դեպի հաստատություն՝
    /// <c>TeacherGrade → Grade → Institution</c>:
    /// </remarks>
    public class TeacherGrade
    {
        /// <summary>Ուսուցչի Id-ն AuthServer-ում («sub»)</summary>
        public string TeacherUserId { get; set; } = "";

        /// <summary>Ուսուցչի Id-ն ԴՇՀ համակարգում</summary>
        /// <remarks>Դատարկ է մինչև AuthServer-ի հետ ինտեգրումը</remarks>
        public string? DshhTeacherId { get; set; }

        /// <summary>Դասարանը, որի հետ աշխատում է ուսուցիչ</summary>
        public int GradeId { get; set; }

        /// <summary>Նավիգացիա դեպի <see cref="Grade"/></summary>
        public Grade Grade { get; set; } = null!;

        /// <summary>Ուսումնական տարին՝ «2025-2026»</summary>
        public string AcademicYear { get; set; } = "";
    }
}